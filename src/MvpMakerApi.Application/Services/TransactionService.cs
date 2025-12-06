using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMvpRepository _mvpRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGitHubService _gitHubService;
    private readonly IGoogleDriveService _googleDriveService;
    private readonly IPaymentService _paymentService;
    private readonly IEncryptionService _encryptionService;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IMvpRepository mvpRepository,
        IUserRepository userRepository,
        IGitHubService gitHubService,
        IGoogleDriveService googleDriveService,
        IPaymentService paymentService,
        IEncryptionService encryptionService)
    {
        _transactionRepository = transactionRepository;
        _mvpRepository = mvpRepository;
        _userRepository = userRepository;
        _gitHubService = gitHubService;
        _googleDriveService = googleDriveService;
        _paymentService = paymentService;
        _encryptionService = encryptionService;
    }

    public async Task<PurchaseResponse> InitiatePurchaseAsync(Guid mvpId, Guid buyerId)
    {
        // 1. Verificar se MVP existe
        var mvp = await _mvpRepository.GetByIdAsync(mvpId);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        // 2. Verificar se comprador não é o vendedor
        if (mvp.OwnerId == buyerId)
        {
            throw new InvalidOperationException("You cannot purchase your own MVP");
        }

        // 3. Verificar se já existe transação pendente para este MVP
        var pendingTransaction = await _transactionRepository.GetPendingByMvpIdAsync(mvpId);
        if (pendingTransaction != null)
        {
            throw new InvalidOperationException("This MVP already has a pending transaction");
        }

        // 4. Buscar comprador para obter GitHubUsername se disponível
        var buyer = await _userRepository.GetByIdAsync(buyerId);
        if (buyer == null)
        {
            throw new Exception("Buyer not found");
        }

        // 5. Determinar status inicial baseado no tipo de produto
        // GitHubRepo e Drive requerem transferência/compartilhamento pelo vendedor
        var requiresTransfer = mvp.ProductType == MvpProductType.GitHubRepo ||
                               mvp.ProductType == MvpProductType.Drive;

        // VALIDAÇÃO DE CREDENCIAIS DO VENDEDOR (NOVO)
        if (requiresTransfer)
        {
            var seller = await _userRepository.GetByIdAsync(mvp.OwnerId);
            if (seller == null) throw new Exception("Seller not found");

            if (mvp.ProductType == MvpProductType.GitHubRepo)
            {
                if (string.IsNullOrEmpty(seller.GitHubToken))
                {
                    throw new InvalidOperationException("Seller has not configured GitHub credentials. Purchase cannot proceed.");
                }

                var token = _encryptionService.DecryptData(cipherText: seller.GitHubToken!);
                var (isValid, _) = await _gitHubService.ValidateTokenAsync(token);
                if (!isValid)
                {
                    throw new InvalidOperationException("Seller's GitHub credentials are invalid or expired. Purchase cannot proceed.");
                }
            }
            else if (mvp.ProductType == MvpProductType.Drive)
            {
                if (string.IsNullOrEmpty(seller.GoogleDriveToken))
                {
                    throw new InvalidOperationException("Seller has not configured Google Drive credentials. Purchase cannot proceed.");
                }

                var token = _encryptionService.DecryptData(cipherText: seller.GoogleDriveToken!);
                var (isValid, _) = await _googleDriveService.ValidateTokenAsync(token);
                if (!isValid)
                {
                    throw new InvalidOperationException("Seller's Google Drive credentials are invalid or expired. Purchase cannot proceed.");
                }
            }
        }

        var initialStatus = requiresTransfer
            ? TransactionStatus.PENDING_TRANSFER
            : TransactionStatus.PENDING;

        var message = mvp.ProductType switch
        {
            MvpProductType.GitHubRepo => "Purchase initiated. Waiting for seller to transfer repository.",
            MvpProductType.Drive => "Purchase initiated. Waiting for seller to share/transfer Drive file.",
            _ => "Purchase initiated. Complete payment to finalize transaction."
        };

        // 6. Criar transação
        var transaction = new Transaction
        {
            MvpId = mvpId,
            SellerId = mvp.OwnerId,
            BuyerId = buyerId,
            Amount = mvp.Price,
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow,
            ProductType = mvp.ProductType.ToString(),
            RepoUrl = mvp.ProductType == MvpProductType.GitHubRepo ? mvp.Link : null,
            BuyerGitHubUsername = null // Will be provided during transfer
        };

        await _transactionRepository.CreateAsync(transaction);

        // 7. Criar sessão de checkout do Stripe
        var checkoutResult = await _paymentService.CreateCheckoutSessionAsync(
            transaction.Id,
            mvp.Name,
            mvp.Price
        );

        // 8. Salvar IDs do Stripe na transação
        if (checkoutResult.Success)
        {
            transaction.StripeSessionId = checkoutResult.SessionId;
            await _transactionRepository.UpdateAsync(transaction);
        }

        return new PurchaseResponse
        {
            TransactionId = transaction.Id,
            Status = transaction.Status.ToString(),
            Amount = transaction.Amount,
            Message = message,
            CheckoutUrl = checkoutResult.SessionUrl,
            CheckoutSessionId = checkoutResult.SessionId
        };
    }

    public async Task<TransactionDto> CompleteTransactionAsync(Guid transactionId, Guid userId)
    {
        // 1. Buscar transação
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new Exception("Transaction not found");
        }

        // 2. Verificar se usuário é o comprador
        if (transaction.BuyerId != userId)
        {
            throw new UnauthorizedAccessException("Only the buyer can complete this transaction");
        }

        // 3. Rejeitar se for GitHubRepo (deve usar endpoint de transferência)
        if (transaction.ProductType == "GitHubRepo")
        {
            throw new InvalidOperationException("GitHubRepo transactions must be completed via transfer endpoint");
        }

        // 4. Verificar se transação está pendente
        if (transaction.Status != TransactionStatus.PENDING)
        {
            throw new InvalidOperationException($"Transaction is not pending. Current status: {transaction.Status}");
        }

        // 5. Buscar MVP
        var mvp = await _mvpRepository.GetByIdAsync(transaction.MvpId);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        // 6. Verificar se vendedor ainda é o dono
        if (mvp.OwnerId != transaction.SellerId)
        {
            transaction.Status = TransactionStatus.FAILED;
            await _transactionRepository.UpdateAsync(transaction);
            throw new InvalidOperationException("MVP ownership has changed. Transaction failed.");
        }

        // 7. Completar transação (repository handles atomic operation)
        await _transactionRepository.CompleteTransactionAsync(transactionId, transaction.BuyerId);

        // 8. Recarregar transação com includes
        transaction = await _transactionRepository.GetByIdAsync(transactionId);

        return MapToDto(transaction!);
    }

    public async Task<TransactionDto> GetTransactionAsync(Guid transactionId, Guid userId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new Exception("Transaction not found");
        }

        // Verificar se usuário é parte da transação
        if (transaction.BuyerId != userId && transaction.SellerId != userId)
        {
            throw new UnauthorizedAccessException("You don't have access to this transaction");
        }

        return MapToDto(transaction);
    }

    public async Task<PagedResult<TransactionDto>> GetUserTransactionsAsync(Guid userId, int pageNumber, int pageSize)
    {
        // Validar parâmetros
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _transactionRepository.GetByUserIdAsync(userId, pageNumber, pageSize);

        return new PagedResult<TransactionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<(bool Success, string Message)> TransferGitHubRepositoryAsync(Guid transactionId, string sellerToken, string buyerUsername)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            return (false, "Transaction not found");
        }

        if (transaction.Status != TransactionStatus.PENDING_TRANSFER)
        {
            return (false, $"Transaction is not pending transfer. Current status: {transaction.Status}");
        }

        if (transaction.ProductType != "GitHubRepo")
        {
            return (false, "Transaction is not for a GitHub repository");
        }

        if (string.IsNullOrEmpty(transaction.RepoUrl))
        {
            return (false, "Repository URL not found in transaction");
        }

        // Get MVP to check BusinessType
        var mvp = transaction.Mvp;
        if (mvp == null)
        {
            return (false, "MVP not found");
        }

        // Execute transfer or fork based on BusinessType
        (bool success, string message) result;

        if (mvp.GitHubBusinessType == Domain.Entities.GitHubBusinessType.Fork)
        {
            // Fork: Invite buyer as collaborator with read access
            // No buyer token needed, as the seller sends the invite
            result = await _gitHubService.InviteCollaboratorAsync(
                transaction.RepoUrl,
                sellerToken,
                buyerUsername
            );
        }
        else
        {
            // Transfer: Move ownership to buyer (default behavior)
            result = await _gitHubService.TransferRepositoryAsync(
                transaction.RepoUrl,
                sellerToken,
                buyerUsername
            );
        }

        if (result.success)
        {
            // Atualizar status para WAITING_ACCEPTANCE (aguardando aceite do comprador)
            transaction.Status = TransactionStatus.WAITING_ACCEPTANCE;
            // CompletedAt permanece null até o comprador verificar
            await _transactionRepository.UpdateAsync(transaction);
        }

        return result;
    }

    public async Task<(bool Success, string Message)> TransferDriveFileAsync(Guid transactionId, string sellerToken, string buyerEmail)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            return (false, "Transaction not found");
        }

        if (transaction.Status != TransactionStatus.PENDING_TRANSFER)
        {
            return (false, $"Transaction is not pending transfer. Current status: {transaction.Status}");
        }

        if (transaction.ProductType != "Drive")
        {
            return (false, "Transaction is not for a Google Drive file");
        }

        // Get MVP to check BusinessType and get file link
        var mvp = transaction.Mvp;
        if (mvp == null)
        {
            return (false, "MVP not found");
        }

        if (string.IsNullOrEmpty(mvp.Link))
        {
            return (false, "Drive file URL not found in MVP");
        }

        // Extract file ID from Drive URL
        var fileId = _googleDriveService.ExtractFileIdFromUrl(mvp.Link);
        if (string.IsNullOrEmpty(fileId))
        {
            return (false, "Could not extract file ID from Drive URL");
        }

        // Execute transfer or share based on BusinessType
        (bool success, string message) result;

        if (mvp.DriveBusinessType == Domain.Entities.DriveBusinessType.Share)
        {
            // Share: Give buyer writer access (can view and edit)
            result = await _googleDriveService.ShareFileAsync(
                fileId,
                sellerToken,
                buyerEmail,
                "writer"
            );
        }
        else
        {
            // Transfer: Move ownership to buyer (default behavior)
            result = await _googleDriveService.TransferOwnershipAsync(
                fileId,
                sellerToken,
                buyerEmail
            );
        }

        if (result.success)
        {
            // Atualizar status para WAITING_ACCEPTANCE (aguardando aceite do comprador)
            transaction.Status = TransactionStatus.WAITING_ACCEPTANCE;
            // CompletedAt permanece null até o comprador verificar
            await _transactionRepository.UpdateAsync(transaction);
        }

        return result;
    }

    public async Task<TransactionDto> VerifyTransferAsync(Guid transactionId, Guid userId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new Exception("Transaction not found");
        }

        // Verificar se usuário é o comprador
        if (transaction.BuyerId != userId)
        {
            throw new UnauthorizedAccessException("Only the buyer can verify this transfer");
        }

        // Verificar status
        if (transaction.Status != TransactionStatus.WAITING_ACCEPTANCE)
        {
            throw new InvalidOperationException($"Transaction is not waiting for acceptance. Current status: {transaction.Status}");
        }

        // Atualizar status para COMPLETED
        transaction.Status = TransactionStatus.COMPLETED;
        transaction.CompletedAt = DateTime.UtcNow;

        await _transactionRepository.UpdateAsync(transaction);

        // Only transfer MVP ownership if BusinessType is Transfer (not Fork/Share)
        // For Fork (GitHub) or Share (Drive), the seller retains ownership and can sell multiple times
        var mvp = transaction.Mvp;
        if (mvp != null)
        {
            bool shouldTransferOwnership = true;

            // Check GitHub business type
            if (mvp.ProductType == MvpProductType.GitHubRepo &&
                mvp.GitHubBusinessType == Domain.Entities.GitHubBusinessType.Fork)
            {
                shouldTransferOwnership = false;
            }

            // Check Drive business type
            if (mvp.ProductType == MvpProductType.Drive &&
                mvp.DriveBusinessType == Domain.Entities.DriveBusinessType.Share)
            {
                shouldTransferOwnership = false;
            }

            if (shouldTransferOwnership)
            {
                await _transactionRepository.CompleteTransactionAsync(transactionId, userId);
            }
        }

        // Recarregar para retornar DTO atualizado
        transaction = await _transactionRepository.GetByIdAsync(transactionId);
        return MapToDto(transaction!);
    }

    private TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            SellerId = transaction.SellerId,
            BuyerId = transaction.BuyerId,
            MvpId = transaction.MvpId,
            Mvp = new MvpDto
            {
                Id = transaction.Mvp!.Id,
                Name = transaction.Mvp.Name,
                Description = transaction.Mvp.Description,
                ImageUrl = transaction.Mvp.ImageUrl,
                Price = transaction.Mvp.Price,
                Technologies = transaction.Mvp.Technologies,
                Categories = transaction.Mvp.Categories,
                CreatedAt = transaction.Mvp.CreatedAt,
                UpdatedAt = transaction.Mvp.UpdatedAt,
                Owner = new OwnerDto
                {
                    Id = transaction.Mvp.Owner!.Id,
                    Name = transaction.Mvp.Owner.Name,
                    Email = transaction.Mvp.Owner.Email
                },
                ProductType = transaction.Mvp.ProductType.ToString(),
                Link = transaction.Mvp.Link,
                PreviewLink = transaction.Mvp.PreviewLink
            },
            Seller = new OwnerDto
            {
                Id = transaction.Seller!.Id,
                Name = transaction.Seller.Name,
                Email = transaction.Seller.Email
            },
            Buyer = new OwnerDto
            {
                Id = transaction.Buyer!.Id,
                Name = transaction.Buyer.Name,
                Email = transaction.Buyer.Email
            },
            Amount = transaction.Amount,
            Status = transaction.Status.ToString(),
            CreatedAt = transaction.CreatedAt,
            CompletedAt = transaction.CompletedAt,
            ProductType = transaction.ProductType,
            RepoUrl = transaction.RepoUrl,
            BuyerGitHubUsername = transaction.BuyerGitHubUsername
        };
    }
}

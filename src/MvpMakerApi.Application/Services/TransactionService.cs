using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMvpRepository _mvpRepository;
    private readonly IGitHubService _gitHubService;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IMvpRepository mvpRepository,
        IGitHubService gitHubService)
    {
        _transactionRepository = transactionRepository;
        _mvpRepository = mvpRepository;
        _gitHubService = gitHubService;
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

        // 4. Criar transação
        var transaction = new Transaction
        {
            MvpId = mvpId,
            SellerId = mvp.OwnerId,
            BuyerId = buyerId,
            Amount = mvp.Price,
            Status = TransactionStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.CreateAsync(transaction);

        return new PurchaseResponse
        {
            TransactionId = transaction.Id,
            Status = transaction.Status.ToString(),
            Amount = transaction.Amount,
            Message = "Purchase initiated. Complete payment to finalize transaction."
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

        // 3. Verificar se transação está pendente
        if (transaction.Status != TransactionStatus.PENDING)
        {
            throw new InvalidOperationException($"Transaction is not pending. Current status: {transaction.Status}");
        }

        // 4. Buscar MVP
        var mvp = await _mvpRepository.GetByIdAsync(transaction.MvpId);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        // 5. Verificar se vendedor ainda é o dono
        if (mvp.OwnerId != transaction.SellerId)
        {
            transaction.Status = TransactionStatus.FAILED;
            await _transactionRepository.UpdateAsync(transaction);
            throw new InvalidOperationException("MVP ownership has changed. Transaction failed.");
        }

        // 6. Completar transação (repository handles atomic operation)
        await _transactionRepository.CompleteTransactionAsync(transactionId, transaction.BuyerId);

        // Nota: Transferência GitHub agora é manual via endpoint /api/transaction/{id}/transfer-github

        // 7. Recarregar transação com includes
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

        if (transaction.Status != TransactionStatus.COMPLETED)
        {
            return (false, "Transaction must be completed before transfer");
        }

        var mvp = transaction.Mvp;
        if (mvp == null || mvp.ProductType != MvpProductType.GitHubRepo)
        {
            return (false, "MVP is not a GitHub repository");
        }

        return await _gitHubService.TransferRepositoryAsync(mvp.Link, sellerToken, buyerUsername);
    }

    private TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
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
            CompletedAt = transaction.CompletedAt
        };
    }
}

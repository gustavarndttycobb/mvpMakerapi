using MvpMakerApi.Application.DTOs;

namespace MvpMakerApi.Application.Interfaces;

public interface ITransactionService
{
    Task<PurchaseResponse> InitiatePurchaseAsync(Guid mvpId, Guid buyerId);
    Task<TransactionDto> CompleteTransactionAsync(Guid transactionId, Guid userId);
    Task<TransactionDto> GetTransactionAsync(Guid transactionId, Guid userId);
    Task<PagedResult<TransactionDto>> GetUserTransactionsAsync(Guid userId, int pageNumber, int pageSize);
    Task<(bool Success, string Message)> TransferGitHubRepositoryAsync(Guid transactionId, string sellerToken, string buyerUsername);
}

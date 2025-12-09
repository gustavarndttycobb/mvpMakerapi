using MvpMakerApi.Domain.Entities;

namespace MvpMakerApi.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<Transaction?> GetPendingByMvpIdAsync(Guid mvpId);
    Task<(List<Transaction> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize);
    Task UpdateAsync(Transaction transaction);
    Task CompleteTransactionAsync(Guid transactionId, Guid newOwnerId);
    Task<List<Transaction>> GetAllAsync();
}

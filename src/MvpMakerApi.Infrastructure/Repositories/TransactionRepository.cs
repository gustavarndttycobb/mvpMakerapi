using Microsoft.EntityFrameworkCore;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Infrastructure.Data;

namespace MvpMakerApi.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.Mvp)
            .Include(t => t.Seller)
            .Include(t => t.Buyer)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction?> GetPendingByMvpIdAsync(Guid mvpId)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.MvpId == mvpId && t.Status == TransactionStatus.PENDING);
    }

    public async Task<(List<Transaction> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize)
    {
        var query = _context.Transactions
            .Include(t => t.Mvp)
            .Include(t => t.Seller)
            .Include(t => t.Buyer)
            .Where(t => t.SellerId == userId || t.BuyerId == userId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Transaction>> GetAllAsync()
    {
        return await _context.Transactions
            .Include(t => t.Mvp)
            .Include(t => t.Seller)
            .Include(t => t.Buyer)
            .ToListAsync();
    }

    public async Task CompleteTransactionAsync(Guid transactionId, Guid newOwnerId)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Buscar transação
            var transaction = await _context.Transactions
                .Include(t => t.Mvp)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new Exception("Transaction not found");
            }

            // 2. Transferir propriedade do MVP
            transaction.Mvp!.OwnerId = newOwnerId;
            transaction.Mvp.UpdatedAt = DateTime.UtcNow;

            // 3. Atualizar status da transação
            transaction.Status = TransactionStatus.COMPLETED;
            transaction.CompletedAt = DateTime.UtcNow;

            // 4. Salvar mudanças
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}

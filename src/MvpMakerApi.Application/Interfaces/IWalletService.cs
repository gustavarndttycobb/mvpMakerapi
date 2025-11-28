using MvpMakerApi.Application.DTOs;

namespace MvpMakerApi.Application.Interfaces;

public interface IWalletService
{
    Task<WalletBalanceDto> AddFundsAsync(Guid userId, decimal amount);
    Task<WalletBalanceDto> GetBalanceAsync(Guid userId);
}

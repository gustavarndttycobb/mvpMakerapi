using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUserRepository _userRepository;

    public WalletService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<WalletBalanceDto> AddFundsAsync(Guid userId, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        user.Balance += amount;
        await _userRepository.UpdateAsync(user);

        return new WalletBalanceDto
        {
            UserId = user.Id,
            Balance = user.Balance
        };
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        return new WalletBalanceDto
        {
            UserId = user.Id,
            Balance = user.Balance
        };
    }
}

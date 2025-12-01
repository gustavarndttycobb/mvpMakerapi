namespace MvpMakerApi.Application.DTOs;

public class AddFundsRequest
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
}

public class WalletBalanceDto
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}

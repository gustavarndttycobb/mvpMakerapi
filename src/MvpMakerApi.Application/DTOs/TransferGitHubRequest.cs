namespace MvpMakerApi.Application.DTOs;

public class TransferGitHubRequest
{
    public string BuyerUsername { get; set; } = string.Empty;
    // SellerToken removed - will use stored encrypted credentials from MVP
}

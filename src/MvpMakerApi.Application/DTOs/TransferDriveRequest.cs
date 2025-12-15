namespace MvpMakerApi.Application.DTOs;

public class TransferDriveRequest
{
    public string BuyerEmail { get; set; } = string.Empty;
    // SellerToken removed - will use stored encrypted credentials from MVP
}

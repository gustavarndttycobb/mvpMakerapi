namespace MvpMakerApi.Application.DTOs;

public class TransferDriveRequest
{
    public string SellerToken { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
}

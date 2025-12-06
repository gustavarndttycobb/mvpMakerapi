namespace MvpMakerApi.Application.DTOs;

public class CreateCheckoutResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionUrl { get; set; } = string.Empty;
    public Guid TransactionId { get; set; }
}

public class PaymentStatusResponse
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
}

namespace MvpMakerApi.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MvpId { get; set; }
    public Mvp? Mvp { get; set; }
    
    public Guid SellerId { get; set; }
    public User? Seller { get; set; }
    
    public Guid BuyerId { get; set; }
    public User? Buyer { get; set; }
    
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.PENDING;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public enum TransactionStatus
{
    PENDING = 0,    // Aguardando pagamento
    COMPLETED = 1,  // Concluída (pago e transferido)
    FAILED = 2,     // Falhou
    CANCELLED = 3   // Cancelada
}

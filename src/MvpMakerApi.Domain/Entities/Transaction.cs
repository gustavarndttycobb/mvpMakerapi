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

    // GitHub Transfer Fields
    public string? ProductType { get; set; } // "GitHubRepo" or "Drive"
    public string? RepoUrl { get; set; }
    public string? BuyerGitHubUsername { get; set; }

    // Stripe Payment Fields
    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
}

public enum TransactionStatus
{
    PENDING = 0,          // Aguardando pagamento
    PENDING_TRANSFER = 1, // Aguardando transferência GitHub
    WAITING_ACCEPTANCE = 2, // Aguardando aceite do comprador
    COMPLETED = 3,        // Concluída (pago e transferido)
    FAILED = 4,           // Falhou
    CANCELLED = 5         // Cancelada
}

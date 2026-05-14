using StockFlow.Domain.Enums;

namespace StockFlow.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerShare { get; set; }
    public decimal Commission { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // CF_12 — fingerprint
    public string Fingerprint { get; set; } = string.Empty;
    public string ExecutedBy { get; set; } = string.Empty;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Company Company { get; set; } = null!;
}

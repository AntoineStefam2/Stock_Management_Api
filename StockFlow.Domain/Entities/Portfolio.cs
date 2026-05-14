namespace StockFlow.Domain.Entities;

public class Portfolio
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public int Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    // CF_12 — fingerprint
    public string LastModifiedBy { get; set; } = "system";

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Company Company { get; set; } = null!;
}

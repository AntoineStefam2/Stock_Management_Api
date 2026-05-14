namespace StockFlow.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconEmoji { get; set; } = "🏢";

    // Stock parameters
    public decimal InitialPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public long TotalShares { get; set; }
    public long AvailableShares { get; set; }
    public int MaxSharesPerUser { get; set; }

    // Audit fingerprint (CF_12)
    public string LastModifiedBy { get; set; } = "system";
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";

    // Navigation
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

namespace StockFlow.Domain.Entities;

public class PriceHistory
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public decimal Price { get; set; }
    public long Volume { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string Trigger { get; set; } = "market"; // "market", "buy", "sell"

    // Navigation
    public Company Company { get; set; } = null!;
}

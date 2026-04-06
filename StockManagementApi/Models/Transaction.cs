public enum TransactionType
{
    Buy,
    Sell,
    Deposit
}

public class Transaction
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int? StockId { get; set; }
    public Stock Stock { get; set; }

    public TransactionType Type { get; set; }

    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }
}
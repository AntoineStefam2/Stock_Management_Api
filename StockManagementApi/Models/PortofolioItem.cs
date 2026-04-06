public class PortfolioItem
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int StockId { get; set; }
    public Stock Stock { get; set; }

    public int Quantity { get; set; }
    public decimal AveragePrice { get; set; }
}
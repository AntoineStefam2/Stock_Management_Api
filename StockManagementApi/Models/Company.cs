public class Company
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Description { get; set; }

    public ICollection<Stock> Stocks { get; set; }
}
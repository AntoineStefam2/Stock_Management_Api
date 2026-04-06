public class Stock
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; }

    public decimal CurrentPrice { get; set; }
    public int AvailableQuantity { get; set; }

    // Admin-controlled parameters
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
}
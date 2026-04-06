public enum UserRole
{
    User,
    Administrator,
    Auditor
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }

    public decimal Balance { get; set; }

    public ICollection<Transaction> Transactions { get; set; }
    public ICollection<PortfolioItem> Portfolio { get; set; }
}
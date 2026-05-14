using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Data;

public class StockFlowDbContext : DbContext
{
    public StockFlowDbContext(DbContextOptions<StockFlowDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Ticker).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.Ticker).IsUnique();
            e.Property(x => x.CurrentPrice).HasColumnType("decimal(18,4)");
            e.Property(x => x.InitialPrice).HasColumnType("decimal(18,4)");
            e.Property(x => x.LastModifiedBy).HasMaxLength(100);
            e.Property(x => x.CreatedBy).HasMaxLength(100);
        });

        // ApplicationUser
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Balance).HasColumnType("decimal(18,4)");
            e.Property(x => x.LastModifiedBy).HasMaxLength(100);
        });

        // Transaction
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PricePerShare).HasColumnType("decimal(18,4)");
            e.Property(x => x.Commission).HasColumnType("decimal(18,4)");
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
            e.Property(x => x.Fingerprint).HasMaxLength(50);
            e.Property(x => x.ExecutedBy).HasMaxLength(100);
            e.HasOne(x => x.User).WithMany(u => u.Transactions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Company).WithMany(c => c.Transactions).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        // Portfolio
        modelBuilder.Entity<Portfolio>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.CompanyId }).IsUnique();
            e.Property(x => x.AverageCost).HasColumnType("decimal(18,4)");
            e.Property(x => x.LastModifiedBy).HasMaxLength(100);
            e.HasOne(x => x.User).WithMany(u => u.Portfolios).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        // PriceHistory
        modelBuilder.Entity<PriceHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasColumnType("decimal(18,4)");
            e.Property(x => x.Trigger).HasMaxLength(20);
            e.HasOne(x => x.Company).WithMany(c => c.PriceHistories).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Company>().HasData(
            new Company { Id = 1, Name = "NovaTech Systems", Ticker = "NVT", Sector = "Technology", Description = "AI-driven enterprise software solutions.", IconEmoji = "💻", InitialPrice = 142.50m, CurrentPrice = 142.50m, TotalShares = 5000000, AvailableShares = 5000000, MaxSharesPerUser = 500, CreatedBy = "system", LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now },
            new Company { Id = 2, Name = "Apex Financial", Ticker = "APX", Sector = "Finance", Description = "Global investment banking & asset management.", IconEmoji = "🏦", InitialPrice = 88.20m, CurrentPrice = 88.20m, TotalShares = 12000000, AvailableShares = 12000000, MaxSharesPerUser = 1000, CreatedBy = "system", LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now },
            new Company { Id = 3, Name = "SolarGrid Co.", Ticker = "SGC", Sector = "Energy", Description = "Renewable energy infrastructure operator.", IconEmoji = "⚡", InitialPrice = 55.70m, CurrentPrice = 55.70m, TotalShares = 8000000, AvailableShares = 8000000, MaxSharesPerUser = 2000, CreatedBy = "system", LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now },
            new Company { Id = 4, Name = "MedVault Inc.", Ticker = "MVT", Sector = "Healthcare", Description = "Medical records & diagnostics platform.", IconEmoji = "🧬", InitialPrice = 203.00m, CurrentPrice = 203.00m, TotalShares = 2000000, AvailableShares = 2000000, MaxSharesPerUser = 200, CreatedBy = "system", LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now },
            new Company { Id = 5, Name = "BrightMart", Ticker = "BRM", Sector = "Consumer", Description = "E-commerce retail marketplace.", IconEmoji = "🛒", InitialPrice = 34.80m, CurrentPrice = 34.80m, TotalShares = 20000000, AvailableShares = 20000000, MaxSharesPerUser = 5000, CreatedBy = "system", LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now },
            new Company { Id = 6, Name = "IronCore Industries", Ticker = "ICI", Sector = "Industrial", Description = "Heavy machinery & logistics.", IconEmoji = "⚙️", InitialPrice = 78.90m, CurrentPrice = 78.90m, TotalShares = 6000000, AvailableShares = 6000000, MaxSharesPerUser = 800, CreatedBy = "system", LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now }
        );

        // Admin user (password: admin — BCrypt hash stored in application startup)
        modelBuilder.Entity<ApplicationUser>().HasData(
            new ApplicationUser { Id = "admin-001", UserName = "admin", FullName = "System Administrator", Email = "admin@stockflow.com", PasswordHash = "AQAAAAIAAYagAAAAEPlaceholderHashAdmin", Role = "Admin", Balance = 100000m, LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now },
            new ApplicationUser { Id = "demo-001", UserName = "demo", FullName = "Demo User", Email = "demo@stockflow.com", PasswordHash = "AQAAAAIAAYagAAAAEPlaceholderHashDemo", Role = "User", Balance = 10000m, LastModifiedBy = "system", CreatedAt = now, LastModifiedAt = now }
        );
    }
}

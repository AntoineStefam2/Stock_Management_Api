using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<PortfolioItem> PortfolioItems { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // USER
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.Balance)
                  .HasColumnType("decimal(18,2)")
                  .HasDefaultValue(0);
        });

        // =========================
        // COMPANY
        // =========================
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(c => c.Symbol)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.HasIndex(c => c.Symbol)
                  .IsUnique();
        });

        // =========================
        // STOCK
        // =========================
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.CurrentPrice)
                  .HasColumnType("decimal(18,2)");

            entity.Property(s => s.MinPrice)
                  .HasColumnType("decimal(18,2)");

            entity.Property(s => s.MaxPrice)
                  .HasColumnType("decimal(18,2)");

            entity.HasOne(s => s.Company)
                  .WithMany(c => c.Stocks)
                  .HasForeignKey(s => s.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // PORTFOLIO
        // =========================
        modelBuilder.Entity<PortfolioItem>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.AveragePrice)
                  .HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.User)
                  .WithMany(u => u.Portfolio)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Stock)
                  .WithMany()
                  .HasForeignKey(p => p.StockId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate stock per user
            entity.HasIndex(p => new { p.UserId, p.StockId })
                  .IsUnique();
        });

        // =========================
        // TRANSACTIONS
        // =========================
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Price)
                  .HasColumnType("decimal(18,2)");

            entity.Property(t => t.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(t => t.User)
                  .WithMany(u => u.Transactions)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Stock)
                  .WithMany()
                  .HasForeignKey(t => t.StockId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // REPORTS
        // =========================
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(r => r.GeneratedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(r => r.GeneratedBy)
                  .WithMany()
                  .HasForeignKey(r => r.GeneratedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // AUDIT LOGS
        // =========================
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Action)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(a => a.Timestamp)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(a => a.PerformedBy)
                  .WithMany()
                  .HasForeignKey(a => a.PerformedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
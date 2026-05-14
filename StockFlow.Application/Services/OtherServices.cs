using Microsoft.EntityFrameworkCore;
using StockFlow.Application.DTOs;
using StockFlow.Application.Interfaces;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace StockFlow.Application.Services;

// ─── UserService ─────────────────────────────────────────
public class UserService : IUserService
{
    private readonly StockFlowDbContext _db;
    public UserService(StockFlowDbContext db) => _db = db;

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var u = await _db.Users.FindAsync(id);
        return u is null ? null : await MapAsync(u);
    }

    public async Task<UserDto?> GetByUserNameAsync(string userName)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName);
        return u is null ? null : await MapAsync(u);
    }

    public async Task<UserDto> RegisterAsync(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == dto.UserName))
            throw new InvalidOperationException("Username already taken.");

        var user = new ApplicationUser
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            Role = "User",
            Balance = dto.InitialDeposit,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            LastModifiedBy = "self"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return await MapAsync(user);
    }

    public async Task<(UserDto? User, bool Success)> ValidateLoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
        if (user is null || !VerifyPassword(dto.Password, user.PasswordHash))
            return (null, false);
        return (await MapAsync(user), true);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _db.Users.ToListAsync();
        var result = new List<UserDto>();
        foreach (var u in users) result.Add(await MapAsync(u));
        return result;
    }

    public async Task DepositAsync(string userId, decimal amount, string modifiedBy)
    {
        var user = await _db.Users.FindAsync(userId) ?? throw new InvalidOperationException("User not found.");
        user.Balance += amount;
        user.LastModifiedBy = modifiedBy;
        user.LastModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task<UserDto> MapAsync(ApplicationUser u)
    {
        var tradeCount = await _db.Transactions.CountAsync(t => t.UserId == u.Id);
        return new UserDto(u.Id, u.UserName, u.FullName, u.Email, u.Role, u.Balance, tradeCount, u.CreatedAt, u.LastModifiedBy);
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "StockFlowSalt2025"));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        // Allow demo seeds: plain text "admin" / "demo"
        if (hash.StartsWith("AQAAAAIAAYag") && (password == "admin" || password == "demo")) return true;
        return HashPassword(password) == hash;
    }
}

// ─── MarketService ───────────────────────────────────────
public class MarketService : IMarketService
{
    private readonly StockFlowDbContext _db;
    private readonly ITradeService _tradeService;
    public MarketService(StockFlowDbContext db, ITradeService tradeService) { _db = db; _tradeService = tradeService; }

    public async Task<IEnumerable<MarketSummaryDto>> GetMarketSummaryAsync()
    {
        var companies = await _db.Companies.ToListAsync();
        var result = new List<MarketSummaryDto>();
        foreach (var c in companies)
        {
            var prev = await _db.PriceHistories
                .Where(p => p.CompanyId == c.Id)
                .OrderByDescending(p => p.RecordedAt)
                .Skip(1).FirstOrDefaultAsync();
            var prevPrice = prev?.Price ?? c.InitialPrice;
            var vol = await _db.PriceHistories
                .Where(p => p.CompanyId == c.Id && p.RecordedAt >= DateTime.UtcNow.AddHours(-24))
                .SumAsync(p => p.Volume);
            result.Add(new MarketSummaryDto(
                c.Id, c.Ticker, c.Name, c.Sector, c.IconEmoji,
                c.CurrentPrice, prevPrice,
                Math.Round(c.CurrentPrice - prevPrice, 4),
                prevPrice > 0 ? Math.Round((c.CurrentPrice - prevPrice) / prevPrice * 100, 2) : 0,
                vol, c.AvailableShares));
        }
        return result;
    }

    public async Task<DashboardDto> GetDashboardAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId) ?? throw new InvalidOperationException("User not found.");
        var portValue = await _tradeService.GetPortfolioValueAsync(userId);
        var trades = await _tradeService.GetUserTransactionsAsync(userId);
        var holdings = await _db.Portfolios.CountAsync(p => p.UserId == userId && p.Quantity > 0);
        var market = await GetMarketSummaryAsync();
        var topMovers = market.OrderByDescending(m => Math.Abs(m.ChangePct)).Take(5);
        return new DashboardDto(
            user.Balance, portValue, user.Balance + portValue,
            trades.Count(), holdings, topMovers, trades.Take(10));
    }
}

// ─── ReportService ───────────────────────────────────────
public class ReportService : IReportService
{
    private readonly StockFlowDbContext _db;
    public ReportService(StockFlowDbContext db) => _db = db;

    public async Task<ReportDto> GenerateReportAsync() => new(
        await GetVolumeReportAsync(),
        await GetAllTransactionsAsync(),
        await GetUserActivityAsync());

    public async Task<IEnumerable<VolumeReportItem>> GetVolumeReportAsync()
    {
        return await _db.Transactions
            .Include(t => t.Company)
            .GroupBy(t => new { t.CompanyId, t.Company.Ticker, t.Company.Name })
            .Select(g => new VolumeReportItem(
                g.Key.Ticker, g.Key.Name,
                g.Where(t => t.Type == Domain.Enums.TransactionType.Buy).Sum(t => (long)t.Quantity),
                g.Where(t => t.Type == Domain.Enums.TransactionType.Sell).Sum(t => (long)t.Quantity),
                g.Sum(t => t.TotalAmount)))
            .ToListAsync();
    }

    public async Task<IEnumerable<UserActivityDto>> GetUserActivityAsync()
    {
        return await _db.Users
            .Select(u => new UserActivityDto(
                u.UserName, u.FullName,
                u.Transactions.Count(),
                u.Transactions.Sum(t => t.TotalAmount),
                u.Transactions.OrderByDescending(t => t.ExecutedAt).Select(t => t.ExecutedAt).FirstOrDefault()))
            .ToListAsync();
    }

    private async Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync()
    {
        return await _db.Transactions
            .Include(t => t.Company)
            .OrderByDescending(t => t.ExecutedAt)
            .Select(t => new TransactionDto(
                t.Id, t.Company.Ticker, t.Company.Name,
                t.Type == Domain.Enums.TransactionType.Buy ? "Buy" : "Sell",
                t.Quantity, t.PricePerShare, t.Commission,
                t.TotalAmount, t.ExecutedAt, t.Fingerprint))
            .ToListAsync();
    }
}

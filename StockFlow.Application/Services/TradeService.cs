using Microsoft.EntityFrameworkCore;
using StockFlow.Application.DTOs;
using StockFlow.Application.Interfaces;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;
using StockFlow.Infrastructure.Data;

namespace StockFlow.Application.Services;

public class TradeService : ITradeService
{
    private readonly StockFlowDbContext _db;
    private const decimal CommissionRate = 0.001m; // 0.1%

    public TradeService(StockFlowDbContext db) => _db = db;

    public async Task<TransactionDto> PlaceOrderAsync(string userId, PlaceOrderDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var company = await _db.Companies.FindAsync(dto.CompanyId)
            ?? throw new InvalidOperationException("Company not found.");

        var isBuy = dto.Type.Equals("Buy", StringComparison.OrdinalIgnoreCase);
        var commission = company.CurrentPrice * dto.Quantity * CommissionRate;

        if (isBuy)
        {
            var total = company.CurrentPrice * dto.Quantity + commission;
            if (user.Balance < total)
                throw new InvalidOperationException("Insufficient funds.");
            if (company.AvailableShares < dto.Quantity)
                throw new InvalidOperationException("Insufficient shares available.");

            user.Balance -= total;
            company.AvailableShares -= dto.Quantity;

            // CF_8: buying pressure raises price
            company.CurrentPrice = Math.Round(company.CurrentPrice * (1 + (decimal)dto.Quantity / company.TotalShares * 10), 4);
            company.LastModifiedBy = user.UserName;
            company.LastModifiedAt = DateTime.UtcNow;

            await UpsertPortfolioAsync(userId, dto.CompanyId, dto.Quantity, company.CurrentPrice, user.UserName, add: true);

            var tx = await RecordTransactionAsync(userId, dto.CompanyId, TransactionType.Buy, dto.Quantity, company.CurrentPrice, commission, user.UserName);
            await RecordPriceHistoryAsync(company.Id, company.CurrentPrice, dto.Quantity, "buy");
            await _db.SaveChangesAsync();
            return TxToDto(tx, company);
        }
        else
        {
            var portfolio = await _db.Portfolios.FirstOrDefaultAsync(p => p.UserId == userId && p.CompanyId == dto.CompanyId);
            if (portfolio is null || portfolio.Quantity < dto.Quantity)
                throw new InvalidOperationException("Insufficient shares owned.");

            var proceeds = company.CurrentPrice * dto.Quantity - commission;
            user.Balance += proceeds;
            company.AvailableShares += dto.Quantity;

            // CF_8: selling pressure lowers price
            company.CurrentPrice = Math.Max(0.01m, Math.Round(company.CurrentPrice * (1 - (decimal)dto.Quantity / company.TotalShares * 8), 4));
            company.LastModifiedBy = user.UserName;
            company.LastModifiedAt = DateTime.UtcNow;

            await UpsertPortfolioAsync(userId, dto.CompanyId, dto.Quantity, company.CurrentPrice, user.UserName, add: false);

            var tx = await RecordTransactionAsync(userId, dto.CompanyId, TransactionType.Sell, dto.Quantity, company.CurrentPrice, commission, user.UserName);
            await RecordPriceHistoryAsync(company.Id, company.CurrentPrice, dto.Quantity, "sell");
            await _db.SaveChangesAsync();
            return TxToDto(tx, company);
        }
    }

    public async Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(string userId)
    {
        return await _db.Transactions
            .Include(t => t.Company)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.ExecutedAt)
            .Select(t => new TransactionDto(
                t.Id, t.Company.Ticker, t.Company.Name,
                t.Type == TransactionType.Buy ? "Buy" : "Sell",
                t.Quantity, t.PricePerShare, t.Commission,
                t.TotalAmount, t.ExecutedAt, t.Fingerprint))
            .ToListAsync();
    }

    public async Task<IEnumerable<PortfolioItemDto>> GetPortfolioAsync(string userId)
    {
        return await _db.Portfolios
            .Include(p => p.Company)
            .Where(p => p.UserId == userId && p.Quantity > 0)
            .Select(p => new PortfolioItemDto(
                p.CompanyId, p.Company.Ticker, p.Company.Name, p.Company.Sector,
                p.Quantity, p.AverageCost, p.Company.CurrentPrice,
                p.Company.CurrentPrice * p.Quantity,
                (p.Company.CurrentPrice - p.AverageCost) * p.Quantity,
                p.AverageCost > 0 ? Math.Round((p.Company.CurrentPrice - p.AverageCost) / p.AverageCost * 100, 2) : 0))
            .ToListAsync();
    }

    public async Task<decimal> GetPortfolioValueAsync(string userId)
    {
        return await _db.Portfolios
            .Include(p => p.Company)
            .Where(p => p.UserId == userId && p.Quantity > 0)
            .SumAsync(p => p.Company.CurrentPrice * p.Quantity);
    }

    // Helpers
    private async Task UpsertPortfolioAsync(string userId, int companyId, int qty, decimal price, string modifiedBy, bool add)
    {
        var p = await _db.Portfolios.FirstOrDefaultAsync(x => x.UserId == userId && x.CompanyId == companyId);
        if (add)
        {
            if (p is null)
            {
                _db.Portfolios.Add(new Portfolio { UserId = userId, CompanyId = companyId, Quantity = qty, AverageCost = price, LastModifiedBy = modifiedBy, LastUpdatedAt = DateTime.UtcNow });
            }
            else
            {
                p.AverageCost = (p.AverageCost * p.Quantity + price * qty) / (p.Quantity + qty);
                p.Quantity += qty;
                p.LastModifiedBy = modifiedBy;
                p.LastUpdatedAt = DateTime.UtcNow;
            }
        }
        else if (p is not null)
        {
            p.Quantity -= qty;
            p.LastModifiedBy = modifiedBy;
            p.LastUpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<Transaction> RecordTransactionAsync(string userId, int companyId, TransactionType type, int qty, decimal price, decimal commission, string userName)
    {
        var isBuy = type == TransactionType.Buy;
        var total = isBuy ? price * qty + commission : price * qty - commission;
        var tx = new Transaction
        {
            UserId = userId, CompanyId = companyId, Type = type,
            Quantity = qty, PricePerShare = price, Commission = commission,
            TotalAmount = Math.Round(total, 4), ExecutedAt = DateTime.UtcNow,
            Fingerprint = GenerateFingerprint(userName),
            ExecutedBy = userName
        };
        _db.Transactions.Add(tx);
        return tx;
    }

    private async Task RecordPriceHistoryAsync(int companyId, decimal price, long volume, string trigger)
    {
        _db.PriceHistories.Add(new PriceHistory
        {
            CompanyId = companyId, Price = price,
            Volume = volume, Trigger = trigger, RecordedAt = DateTime.UtcNow
        });
    }

    private static string GenerateFingerprint(string userName)
    {
        var prefix = userName.Length >= 3 ? userName[..3].ToUpper() : userName.ToUpper().PadRight(3, 'X');
        var random = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"{prefix}-{random}";
    }

    private static TransactionDto TxToDto(Transaction tx, Company company) => new(
        tx.Id, company.Ticker, company.Name,
        tx.Type == TransactionType.Buy ? "Buy" : "Sell",
        tx.Quantity, tx.PricePerShare, tx.Commission,
        tx.TotalAmount, tx.ExecutedAt, tx.Fingerprint);
}

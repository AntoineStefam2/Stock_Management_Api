namespace StockFlow.Application.DTOs;

public record CompanyDto(
    int Id, string Name, string Ticker, string Sector,
    string Description, string IconEmoji, decimal CurrentPrice,
    decimal InitialPrice, long TotalShares, long AvailableShares,
    int MaxSharesPerUser, string LastModifiedBy, DateTime LastModifiedAt);

public record CompanyCreateDto(
    string Name, string Ticker, string Sector, string Description,
    decimal InitialPrice, long TotalShares, int MaxSharesPerUser);

public record CompanyUpdateDto(
    string Name, string Sector, string Description,
    decimal CurrentPrice, long AvailableShares, int MaxSharesPerUser);

public record TransactionDto(
    int Id, string Ticker, string CompanyName, string Type,
    int Quantity, decimal PricePerShare, decimal Commission,
    decimal TotalAmount, DateTime ExecutedAt, string Fingerprint);

public record PlaceOrderDto(int CompanyId, int Quantity, string Type); // Type: "Buy" | "Sell"

public record PortfolioItemDto(
    int CompanyId, string Ticker, string CompanyName, string Sector,
    int Quantity, decimal AverageCost, decimal CurrentPrice,
    decimal CurrentValue, decimal ProfitLoss, decimal ProfitLossPct);

public record UserDto(
    string Id, string UserName, string FullName, string Email,
    string Role, decimal Balance, int TotalTrades,
    DateTime CreatedAt, string LastModifiedBy);

public record RegisterDto(string UserName, string FullName, string Email, string Password, decimal InitialDeposit);
public record LoginDto(string UserName, string Password);

public record PriceHistoryDto(decimal Price, long Volume, DateTime RecordedAt, string Trigger);

public record MarketSummaryDto(
    int CompanyId, string Ticker, string Name, string Sector, string IconEmoji,
    decimal CurrentPrice, decimal PreviousPrice, decimal ChangeAmount,
    decimal ChangePct, long Volume, long AvailableShares);

public record DashboardDto(
    decimal CashBalance, decimal PortfolioValue, decimal TotalWealth,
    int TotalTrades, int HoldingsCount,
    IEnumerable<MarketSummaryDto> TopMovers,
    IEnumerable<TransactionDto> RecentTransactions);

public record ReportDto(
    IEnumerable<VolumeReportItem> VolumeByStock,
    IEnumerable<TransactionDto> AllTransactions,
    IEnumerable<UserActivityDto> UserActivity);

public record VolumeReportItem(string Ticker, string CompanyName, long TotalBuyVolume, long TotalSellVolume, decimal TotalValue);
public record UserActivityDto(string UserName, string FullName, int TradeCount, decimal TotalTraded, DateTime LastActive);

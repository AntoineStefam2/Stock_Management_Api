using StockFlow.Application.DTOs;

namespace StockFlow.Application.Interfaces;

public interface ICompanyService
{
    Task<IEnumerable<CompanyDto>> GetAllAsync();
    Task<CompanyDto?> GetByIdAsync(int id);
    Task<CompanyDto?> GetByTickerAsync(string ticker);
    Task<IEnumerable<CompanyDto>> SearchAsync(string? query, string? sector);
    Task<CompanyDto> CreateAsync(CompanyCreateDto dto, string createdBy);
    Task<CompanyDto?> UpdateAsync(int id, CompanyUpdateDto dto, string modifiedBy);
    Task<bool> DeleteAsync(int id, string deletedBy);
    Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(int companyId, int days = 30);
}

public interface ITradeService
{
    Task<TransactionDto> PlaceOrderAsync(string userId, PlaceOrderDto dto);
    Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(string userId);
    Task<IEnumerable<PortfolioItemDto>> GetPortfolioAsync(string userId);
    Task<decimal> GetPortfolioValueAsync(string userId);
}

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(string id);
    Task<UserDto?> GetByUserNameAsync(string userName);
    Task<UserDto> RegisterAsync(RegisterDto dto);
    Task<(UserDto? User, bool Success)> ValidateLoginAsync(LoginDto dto);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task DepositAsync(string userId, decimal amount, string modifiedBy);
}

public interface IMarketService
{
    Task<IEnumerable<MarketSummaryDto>> GetMarketSummaryAsync();
    Task<DashboardDto> GetDashboardAsync(string userId);
}

public interface IReportService
{
    Task<ReportDto> GenerateReportAsync();
    Task<IEnumerable<VolumeReportItem>> GetVolumeReportAsync();
    Task<IEnumerable<UserActivityDto>> GetUserActivityAsync();
}

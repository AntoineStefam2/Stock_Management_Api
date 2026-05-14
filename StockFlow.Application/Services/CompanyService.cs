using Microsoft.EntityFrameworkCore;
using StockFlow.Application.DTOs;
using StockFlow.Application.Interfaces;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;

namespace StockFlow.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly StockFlowDbContext _db;

    public CompanyService(StockFlowDbContext db) => _db = db;

    public async Task<IEnumerable<CompanyDto>> GetAllAsync()
        => await _db.Companies.Select(c => ToDto(c)).ToListAsync();

    public async Task<CompanyDto?> GetByIdAsync(int id)
    {
        var c = await _db.Companies.FindAsync(id);
        return c is null ? null : ToDto(c);
    }

    public async Task<CompanyDto?> GetByTickerAsync(string ticker)
    {
        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Ticker == ticker.ToUpper());
        return c is null ? null : ToDto(c);
    }

    public async Task<IEnumerable<CompanyDto>> SearchAsync(string? query, string? sector)
    {
        var q = _db.Companies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(c => c.Name.Contains(query) || c.Ticker.Contains(query) || c.Sector.Contains(query));
        if (!string.IsNullOrWhiteSpace(sector) && sector != "All")
            q = q.Where(c => c.Sector == sector);
        return await q.Select(c => ToDto(c)).ToListAsync();
    }

    public async Task<CompanyDto> CreateAsync(CompanyCreateDto dto, string createdBy)
    {
        var company = new Company
        {
            Name = dto.Name,
            Ticker = dto.Ticker.ToUpper(),
            Sector = dto.Sector,
            Description = dto.Description,
            InitialPrice = dto.InitialPrice,
            CurrentPrice = dto.InitialPrice,
            TotalShares = dto.TotalShares,
            AvailableShares = dto.TotalShares,
            MaxSharesPerUser = dto.MaxSharesPerUser,
            IconEmoji = SectorIcon(dto.Sector),
            CreatedBy = createdBy,
            LastModifiedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };
        _db.Companies.Add(company);

        // Record initial price
        _db.PriceHistories.Add(new PriceHistory
        {
            Company = company, Price = dto.InitialPrice,
            Volume = 0, Trigger = "init", RecordedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ToDto(company);
    }

    public async Task<CompanyDto?> UpdateAsync(int id, CompanyUpdateDto dto, string modifiedBy)
    {
        var company = await _db.Companies.FindAsync(id);
        if (company is null) return null;

        company.Name = dto.Name;
        company.Sector = dto.Sector;
        company.Description = dto.Description;
        company.CurrentPrice = dto.CurrentPrice;
        company.AvailableShares = dto.AvailableShares;
        company.MaxSharesPerUser = dto.MaxSharesPerUser;
        company.IconEmoji = SectorIcon(dto.Sector);
        company.LastModifiedBy = modifiedBy;
        company.LastModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(company);
    }

    public async Task<bool> DeleteAsync(int id, string deletedBy)
    {
        var company = await _db.Companies.FindAsync(id);
        if (company is null) return false;
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(int companyId, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await _db.PriceHistories
            .Where(p => p.CompanyId == companyId && p.RecordedAt >= since)
            .OrderBy(p => p.RecordedAt)
            .Select(p => new PriceHistoryDto(p.Price, p.Volume, p.RecordedAt, p.Trigger))
            .ToListAsync();
    }

    private static CompanyDto ToDto(Company c) => new(
        c.Id, c.Name, c.Ticker, c.Sector, c.Description, c.IconEmoji,
        c.CurrentPrice, c.InitialPrice, c.TotalShares, c.AvailableShares,
        c.MaxSharesPerUser, c.LastModifiedBy, c.LastModifiedAt);

    private static string SectorIcon(string sector) => sector switch
    {
        "Technology" => "💻", "Finance" => "🏦", "Energy" => "⚡",
        "Healthcare" => "🧬", "Consumer" => "🛒", _ => "⚙️"
    };
}

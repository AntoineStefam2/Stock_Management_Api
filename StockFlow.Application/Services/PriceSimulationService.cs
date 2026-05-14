using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;

namespace StockFlow.Application.Services;

/// <summary>
/// CF_8 — Simulates ambient market-driven price fluctuations every 30 seconds.
/// </summary>
public class PriceSimulationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceSimulationService> _logger;
    private static readonly Random _rng = new();

    public PriceSimulationService(IServiceScopeFactory scopeFactory, ILogger<PriceSimulationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            await SimulatePricesAsync(stoppingToken);
        }
    }

    private async Task SimulatePricesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();

            var companies = await db.Companies.ToListAsync(ct);
            var histories = new List<PriceHistory>();

            foreach (var c in companies)
            {
                var pctChange = (_rng.NextDouble() - 0.48) * 0.012; // slight upward bias
                c.CurrentPrice = Math.Max(0.01m, Math.Round(c.CurrentPrice * (1 + (decimal)pctChange), 4));
                c.LastModifiedBy = "market-engine";
                c.LastModifiedAt = DateTime.UtcNow;

                histories.Add(new PriceHistory
                {
                    CompanyId = c.Id,
                    Price = c.CurrentPrice,
                    Volume = _rng.NextInt64(1000, 50000),
                    Trigger = "market",
                    RecordedAt = DateTime.UtcNow
                });
            }

            db.PriceHistories.AddRange(histories);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Price simulation error");
        }
    }
}

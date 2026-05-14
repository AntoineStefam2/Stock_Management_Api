using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.DTOs;
using StockFlow.Application.Interfaces;
using System.Security.Claims;

namespace StockFlow.Web.Controllers;

// ── Market (CF_10) ─────────────────────────────────────────────────────────
[Authorize]
public class MarketController : Controller
{
    private readonly ICompanyService _companies;
    private readonly IMarketService _market;

    public MarketController(ICompanyService companies, IMarketService market)
    { _companies = companies; _market = market; }

    public async Task<IActionResult> Index(string? q, string? sector)
    {
        var items = await _companies.SearchAsync(q, sector);
        var summary = await _market.GetMarketSummaryAsync();
        ViewBag.Query = q;
        ViewBag.Sector = sector;
        ViewBag.Summary = summary.ToDictionary(s => s.Ticker);
        return View(items);
    }

    public async Task<IActionResult> Detail(string ticker)
    {
        var company = await _companies.GetByTickerAsync(ticker);
        if (company is null) return NotFound();
        var history = await _companies.GetPriceHistoryAsync(company.Id, 30);
        ViewBag.History = history;
        return View(company);
    }

    [HttpGet]
    public async Task<IActionResult> PriceHistory(int id, int days = 30)
    {
        var history = await _companies.GetPriceHistoryAsync(id, days);
        return Json(history);
    }
}

// ── Trade (CF_7) ────────────────────────────────────────────────────────────
[Authorize]
public class TradeController : Controller
{
    private readonly ICompanyService _companies;
    private readonly ITradeService _tradeService;
    private readonly IUserService _userService;

    public TradeController(ICompanyService companies, ITradeService tradeService, IUserService userService)
    { _companies = companies; _tradeService = tradeService; _userService = userService; }

    public async Task<IActionResult> Index(string? ticker)
    {
        var companies = await _companies.GetAllAsync();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userService.GetByIdAsync(userId);
        ViewBag.SelectedTicker = ticker;
        ViewBag.UserBalance = user?.Balance ?? 0m;
        return View(companies);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var result = await _tradeService.PlaceOrderAsync(userId, dto);
            TempData["Success"] = $"{dto.Type} order executed: {result.Quantity} × {result.Ticker} @ ${result.PricePerShare:F2} — Fingerprint: {result.Fingerprint}";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> LivePrice(string ticker)
    {
        var company = await _companies.GetByTickerAsync(ticker);
        if (company is null) return NotFound();
        return Json(new { price = company.CurrentPrice, ticker = company.Ticker });
    }
}

// ── Portfolio (CF_9) ────────────────────────────────────────────────────────
[Authorize]
public class PortfolioController : Controller
{
    private readonly ITradeService _tradeService;
    private readonly IUserService _userService;

    public PortfolioController(ITradeService tradeService, IUserService userService)
    { _tradeService = tradeService; _userService = userService; }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var holdings = await _tradeService.GetPortfolioAsync(userId);
        var user = await _userService.GetByIdAsync(userId);
        var portValue = await _tradeService.GetPortfolioValueAsync(userId);
        ViewBag.CashBalance = user?.Balance ?? 0m;
        ViewBag.PortfolioValue = portValue;
        ViewBag.TotalWealth = (user?.Balance ?? 0m) + portValue;
        return View(holdings);
    }

    public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var transactions = await _tradeService.GetUserTransactionsAsync(userId);
        return View(transactions);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var transactions = await _tradeService.GetUserTransactionsAsync(userId);
        var csv = "Time,Type,Ticker,Company,Qty,Price,Commission,Total,Fingerprint\n"
            + string.Join("\n", transactions.Select(t =>
                $"{t.ExecutedAt:s},{t.Type},{t.Ticker},{t.CompanyName},{t.Quantity},{t.PricePerShare:F4},{t.Commission:F4},{t.TotalAmount:F4},{t.Fingerprint}"));
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
            $"transactions_{User.Identity!.Name}_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

// ── Reports (CF_11) ─────────────────────────────────────────────────────────
[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService) => _reportService = reportService;

    public async Task<IActionResult> Index()
    {
        var report = await _reportService.GenerateReportAsync();
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string type)
    {
        var report = await _reportService.GenerateReportAsync();
        string csv = type switch
        {
            "volume" => "Ticker,Company,BuyVol,SellVol,TotalValue\n"
                + string.Join("\n", report.VolumeByStock.Select(v =>
                    $"{v.Ticker},{v.CompanyName},{v.TotalBuyVolume},{v.TotalSellVolume},{v.TotalValue:F2}")),
            "activity" => "User,Name,Trades,TotalTraded,LastActive\n"
                + string.Join("\n", report.UserActivity.Select(u =>
                    $"{u.UserName},{u.FullName},{u.TradeCount},{u.TotalTraded:F2},{u.LastActive:s}")),
            _ => "Time,Type,Ticker,Qty,Price,Total,Fingerprint\n"
                + string.Join("\n", report.AllTransactions.Select(t =>
                    $"{t.ExecutedAt:s},{t.Type},{t.Ticker},{t.Quantity},{t.PricePerShare:F4},{t.TotalAmount:F4},{t.Fingerprint}"))
        };
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
            $"report_{type}_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

// ── Admin — Companies (CF_1–CF_5) ───────────────────────────────────────────
[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly ICompanyService _companyService;
    private readonly IUserService _userService;

    public AdminController(ICompanyService companyService, IUserService userService)
    { _companyService = companyService; _userService = userService; }

    public async Task<IActionResult> Companies()
        => View(await _companyService.GetAllAsync());

    [HttpGet]
    public IActionResult AddCompany()
        => View(new CompanyCreateDto("", "", "Technology", "", 100m, 1_000_000, 10_000));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCompany(CompanyCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _companyService.CreateAsync(dto, User.Identity!.Name!);
            TempData["Success"] = $"Company {dto.Ticker.ToUpper()} added successfully.";
            return RedirectToAction(nameof(Companies));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditCompany(int id)
    {
        var c = await _companyService.GetByIdAsync(id);
        if (c is null) return NotFound();
        ViewBag.CompanyId = id;
        ViewBag.CompanyName = c.Name;
        return View(new CompanyUpdateDto(c.Name, c.Sector, c.Description, c.CurrentPrice, c.AvailableShares, c.MaxSharesPerUser));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCompany(int id, CompanyUpdateDto dto)
    {
        if (!ModelState.IsValid) { ViewBag.CompanyId = id; return View(dto); }
        var result = await _companyService.UpdateAsync(id, dto, User.Identity!.Name!);
        if (result is null) return NotFound();
        TempData["Success"] = "Company updated successfully.";
        return RedirectToAction(nameof(Companies));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        await _companyService.DeleteAsync(id, User.Identity!.Name!);
        TempData["Success"] = "Company deleted.";
        return RedirectToAction(nameof(Companies));
    }

    public async Task<IActionResult> Users()
        => View(await _userService.GetAllAsync());
}

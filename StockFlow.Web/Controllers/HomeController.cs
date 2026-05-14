using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Interfaces;
using System.Security.Claims;

namespace StockFlow.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IMarketService _marketService;

    public HomeController(IMarketService marketService) => _marketService = marketService;

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dashboard = await _marketService.GetDashboardAsync(userId);
        return View(dashboard);
    }
}

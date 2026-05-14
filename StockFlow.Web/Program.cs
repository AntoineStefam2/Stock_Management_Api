using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Interfaces;
using StockFlow.Application.Services;
using StockFlow.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Database (CF_14) ───────────────────────────────────────────────────
builder.Services.AddDbContext<StockFlowDbContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("StockFlow.Infrastructure")));

// ── Application Services ───────────────────────────────────────────────
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMarketService, MarketService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ── CF_8 Background price simulation ──────────────────────────────────
builder.Services.AddHostedService<PriceSimulationService>();

// ── MVC ───────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Cookie Authentication ──────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ── Migrate & seed ────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ── Middleware pipeline ───────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using Microsoft.EntityFrameworkCore;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // =========================
        // Add services
        // =========================

        // Add controllers
        builder.Services.AddControllers();

        // Add Swagger (API testing UI)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add DbContext (SQL Server)
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")
            )
        );

        // =========================
        // Build app
        // =========================

        var app = builder.Build();

        // =========================
        // Configure middleware
        // =========================

        // Enable Swagger only in development
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Optional: redirect HTTP → HTTPS
        app.UseHttpsRedirection();

        // Authorization middleware (we'll use later)
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();

        // =========================
        // Run app
        // =========================

        app.Run();
    }
}
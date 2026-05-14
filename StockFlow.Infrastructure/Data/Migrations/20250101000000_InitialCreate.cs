using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Companies",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                Ticker = table.Column<string>(maxLength: 10, nullable: false),
                Sector = table.Column<string>(nullable: false),
                Description = table.Column<string>(nullable: false),
                IconEmoji = table.Column<string>(nullable: false, defaultValue: "🏢"),
                InitialPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                CurrentPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                TotalShares = table.Column<long>(nullable: false),
                AvailableShares = table.Column<long>(nullable: false),
                MaxSharesPerUser = table.Column<int>(nullable: false),
                LastModifiedBy = table.Column<string>(maxLength: 100, nullable: false),
                LastModifiedAt = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                CreatedBy = table.Column<string>(maxLength: 100, nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Companies", x => x.Id); });

        migrationBuilder.CreateIndex(name: "IX_Companies_Ticker", table: "Companies", column: "Ticker", unique: true);

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                UserName = table.Column<string>(maxLength: 100, nullable: false),
                FullName = table.Column<string>(nullable: false),
                Email = table.Column<string>(maxLength: 200, nullable: false),
                PasswordHash = table.Column<string>(nullable: false),
                Role = table.Column<string>(nullable: false, defaultValue: "User"),
                Balance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                LastModifiedBy = table.Column<string>(maxLength: 100, nullable: false),
                LastModifiedAt = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

        migrationBuilder.CreateIndex(name: "IX_Users_UserName", table: "Users", column: "UserName", unique: true);

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(nullable: false),
                CompanyId = table.Column<int>(nullable: false),
                Type = table.Column<int>(nullable: false),
                Quantity = table.Column<int>(nullable: false),
                PricePerShare = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                Commission = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                ExecutedAt = table.Column<DateTime>(nullable: false),
                Fingerprint = table.Column<string>(maxLength: 50, nullable: false),
                ExecutedBy = table.Column<string>(maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.Id);
                table.ForeignKey("FK_Transactions_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Transactions_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Portfolios",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(nullable: false),
                CompanyId = table.Column<int>(nullable: false),
                Quantity = table.Column<int>(nullable: false),
                AverageCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                LastUpdatedAt = table.Column<DateTime>(nullable: false),
                LastModifiedBy = table.Column<string>(maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Portfolios", x => x.Id);
                table.ForeignKey("FK_Portfolios_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Portfolios_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Portfolios_UserId_CompanyId", table: "Portfolios", columns: new[] { "UserId", "CompanyId" }, unique: true);

        migrationBuilder.CreateTable(
            name: "PriceHistories",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                Volume = table.Column<long>(nullable: false),
                RecordedAt = table.Column<DateTime>(nullable: false),
                Trigger = table.Column<string>(maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PriceHistories", x => x.Id);
                table.ForeignKey("FK_PriceHistories_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PriceHistories");
        migrationBuilder.DropTable(name: "Portfolios");
        migrationBuilder.DropTable(name: "Transactions");
        migrationBuilder.DropTable(name: "Users");
        migrationBuilder.DropTable(name: "Companies");
    }
}

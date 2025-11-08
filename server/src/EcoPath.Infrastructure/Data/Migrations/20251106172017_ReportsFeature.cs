using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoPath.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReportsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalcEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FuelType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalcEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalcEntries_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OffsetEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    AreaHectares = table.Column<double>(type: "float", nullable: false),
                    TreesPerHectare = table.Column<double>(type: "float", nullable: false),
                    Years = table.Column<double>(type: "float", nullable: false),
                    MarketRate = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffsetEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffsetEntries_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PathwaysEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    EvCount = table.Column<double>(type: "float", nullable: false),
                    ReMW = table.Column<double>(type: "float", nullable: false),
                    RePct = table.Column<double>(type: "float", nullable: false),
                    McCH4 = table.Column<double>(type: "float", nullable: false),
                    VamCH4 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwaysEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PathwaysEntries_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalcEntries_ReportId",
                table: "CalcEntries",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_OffsetEntries_ReportId",
                table: "OffsetEntries",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwaysEntries_ReportId",
                table: "PathwaysEntries",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalcEntries");

            migrationBuilder.DropTable(
                name: "OffsetEntries");

            migrationBuilder.DropTable(
                name: "PathwaysEntries");

            migrationBuilder.DropTable(
                name: "Reports");
        }
    }
}

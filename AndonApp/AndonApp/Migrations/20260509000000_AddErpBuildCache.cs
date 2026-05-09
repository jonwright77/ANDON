using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndonApp.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(AndonApp.Data.AndonDbContext))]
    [Migration("20260509000000_AddErpBuildCache")]
    public partial class AddErpBuildCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErpBuildCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pool = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpBuildCaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErpBuildCaches_Pool_Timestamp",
                table: "ErpBuildCaches",
                columns: new[] { "Pool", "Timestamp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ErpBuildCaches");
        }
    }
}

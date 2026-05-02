using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AndonApp.Migrations
{
    [DbContext(typeof(AndonApp.Data.AndonDbContext))]
    [Migration("20240105000000_AddProductionLinePool")]
    public partial class AddProductionLinePool : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pool",
                table: "ProductionLines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pool",
                table: "ProductionLines");
        }
    }
}

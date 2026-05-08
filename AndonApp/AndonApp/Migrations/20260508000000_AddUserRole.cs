using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AndonApp.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(AndonApp.Data.AndonDbContext))]
    [Migration("20260508000000_AddUserRole")]
    public partial class AddUserRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AdminUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Admin");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "AdminUsers");
        }
    }
}

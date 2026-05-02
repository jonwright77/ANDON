using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AndonApp.Migrations
{
    [DbContext(typeof(AndonApp.Data.AndonDbContext))]
    [Migration("20240106000000_AddLineTypes")]
    public partial class AddLineTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LineTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineTypes", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "LineTypeId",
                table: "ProductionLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_LineTypeId",
                table: "ProductionLines",
                column: "LineTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionLines_LineTypes_LineTypeId",
                table: "ProductionLines",
                column: "LineTypeId",
                principalTable: "LineTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionLines_LineTypes_LineTypeId",
                table: "ProductionLines");

            migrationBuilder.DropIndex(
                name: "IX_ProductionLines_LineTypeId",
                table: "ProductionLines");

            migrationBuilder.DropColumn(
                name: "LineTypeId",
                table: "ProductionLines");

            migrationBuilder.DropTable(name: "LineTypes");
        }
    }
}

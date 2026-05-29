using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixStockActual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StockAnual",
                table: "Productos",
                newName: "StockActual");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StockActual",
                table: "Productos",
                newName: "StockAnual");
        }
    }
}

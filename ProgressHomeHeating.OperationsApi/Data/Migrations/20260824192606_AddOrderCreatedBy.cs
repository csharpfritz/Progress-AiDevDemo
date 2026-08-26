using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProgressHomeHeating.OperationsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Orders");
        }
    }
}

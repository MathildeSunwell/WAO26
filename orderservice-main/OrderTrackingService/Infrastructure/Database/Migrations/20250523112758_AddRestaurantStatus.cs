using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingService.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RestaurantStatus",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestaurantStatus",
                table: "Orders");
        }
    }
}

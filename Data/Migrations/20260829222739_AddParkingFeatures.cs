using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkly_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "Features",
                table: "Parkings",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features",
                table: "Parkings");
        }
    }
}

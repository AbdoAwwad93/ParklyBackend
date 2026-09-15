using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkly_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedSpaceTypeAndLevelTOParkignSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "ParkingSpaces",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpaceType",
                table: "ParkingSpaces",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "SpaceType",
                table: "ParkingSpaces");
        }
    }
}

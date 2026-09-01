using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkly_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAverageRatingToParking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Parkings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "Parkings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Parkings");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "Parkings");
        }
    }
}

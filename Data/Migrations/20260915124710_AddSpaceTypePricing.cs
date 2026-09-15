using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkly_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceTypePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpaceTypePricings",
                columns: table => new
                {
                    SpaceTypePricingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParkingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceType = table.Column<int>(type: "integer", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    WeeklyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceTypePricings", x => x.SpaceTypePricingId);
                    table.ForeignKey(
                        name: "FK_SpaceTypePricings_Parkings_ParkingId",
                        column: x => x.ParkingId,
                        principalTable: "Parkings",
                        principalColumn: "ParkingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceTypePricings_ParkingId_SpaceType",
                table: "SpaceTypePricings",
                columns: new[] { "ParkingId", "SpaceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpaceTypePricings");
        }
    }
}

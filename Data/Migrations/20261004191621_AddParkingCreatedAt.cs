using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkly_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Parkings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Parkings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkly_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BusinessVerifiedAt",
                table: "ParkingOwners",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityStateZip",
                table: "ParkingOwners",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyCancellations",
                table: "ParkingOwners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMarketingUpdates",
                table: "ParkingOwners",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyNewBookings",
                table: "ParkingOwners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyRevenueMilestones",
                table: "ParkingOwners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifySpaceAlerts",
                table: "ParkingOwners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "ParkingOwners",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "ParkingOwners",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityState",
                table: "AspNetUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessVerifiedAt",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "CityStateZip",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "NotifyCancellations",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "NotifyMarketingUpdates",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "NotifyNewBookings",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "NotifyRevenueMilestones",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "NotifySpaceAlerts",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "ParkingOwners");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CityState",
                table: "AspNetUsers");
        }
    }
}

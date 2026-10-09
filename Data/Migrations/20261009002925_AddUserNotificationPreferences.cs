using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkly_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Reservations"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
                ALTER TABLE ""AspNetUsers"" ADD COLUMN IF NOT EXISTS ""EmailNotificationsEnabled"" boolean NOT NULL DEFAULT TRUE;
                ALTER TABLE ""AspNetUsers"" ADD COLUMN IF NOT EXISTS ""PushNotificationsEnabled"" boolean NOT NULL DEFAULT TRUE;

                CREATE TABLE IF NOT EXISTS ""ActivityLogs"" (
                    ""ActivityId"" uuid NOT NULL,
                    ""EventType"" character varying(50) NOT NULL,
                    ""Category"" character varying(50) NOT NULL,
                    ""Description"" character varying(255) NOT NULL,
                    ""ActorUserId"" uuid NULL,
                    ""ActorName"" character varying(255) NULL,
                    ""TargetEntityId"" uuid NULL,
                    ""TargetEntityType"" character varying(50) NULL,
                    ""ParkingId"" uuid NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_ActivityLogs"" PRIMARY KEY (""ActivityId"")
                );

                CREATE INDEX IF NOT EXISTS ""IX_Reservations_CreatedAt"" ON ""Reservations"" (""CreatedAt"");
                CREATE INDEX IF NOT EXISTS ""IX_ActivityLogs_CreatedAt"" ON ""ActivityLogs"" (""CreatedAt"");
                CREATE INDEX IF NOT EXISTS ""IX_ActivityLogs_ParkingId_CreatedAt"" ON ""ActivityLogs"" (""ParkingId"", ""CreatedAt"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CreatedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PushNotificationsEnabled",
                table: "AspNetUsers");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStravaDetailLapsSplitsAndLatLng : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "ActivityDataPoints",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "ActivityDataPoints",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Moving",
                table: "ActivityDataPoints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "ActivityDataPoints",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AverageSpeed",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndLat",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndLng",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapPolyline",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxSpeed",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartLat",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartLng",
                table: "Activities",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityLaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    StravaLapId = table.Column<long>(type: "INTEGER", nullable: false),
                    LapIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ElapsedTime = table.Column<int>(type: "INTEGER", nullable: false),
                    MovingTime = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Distance = table.Column<double>(type: "REAL", nullable: false),
                    AverageSpeed = table.Column<double>(type: "REAL", nullable: false),
                    MaxSpeed = table.Column<double>(type: "REAL", nullable: true),
                    AverageHeartRate = table.Column<double>(type: "REAL", nullable: true),
                    MaxHeartRate = table.Column<double>(type: "REAL", nullable: true),
                    ElevationGain = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivitySplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    SplitIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Distance = table.Column<double>(type: "REAL", nullable: false),
                    ElapsedTime = table.Column<int>(type: "INTEGER", nullable: false),
                    ElevationDifference = table.Column<double>(type: "REAL", nullable: true),
                    MovingTime = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageSpeed = table.Column<double>(type: "REAL", nullable: false),
                    AverageHeartRate = table.Column<double>(type: "REAL", nullable: true),
                    PaceZone = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySplits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLaps_ActivityId_LapIndex",
                table: "ActivityLaps",
                columns: new[] { "ActivityId", "LapIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySplits_ActivityId_SplitIndex",
                table: "ActivitySplits",
                columns: new[] { "ActivityId", "SplitIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLaps");

            migrationBuilder.DropTable(
                name: "ActivitySplits");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "ActivityDataPoints");

            migrationBuilder.DropColumn(
                name: "Lng",
                table: "ActivityDataPoints");

            migrationBuilder.DropColumn(
                name: "Moving",
                table: "ActivityDataPoints");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "ActivityDataPoints");

            migrationBuilder.DropColumn(
                name: "AverageSpeed",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "EndLat",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "EndLng",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "MapPolyline",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "MaxSpeed",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "StartLat",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "StartLng",
                table: "Activities");
        }
    }
}

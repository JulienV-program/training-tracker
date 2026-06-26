using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartDateLocal = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Distance = table.Column<double>(type: "REAL", nullable: false),
                    MovingTime = table.Column<int>(type: "INTEGER", nullable: false),
                    ElevationGain = table.Column<double>(type: "REAL", nullable: false),
                    AverageHeartRate = table.Column<double>(type: "REAL", nullable: true),
                    MaxHeartRate = table.Column<double>(type: "REAL", nullable: true),
                    AverageWatts = table.Column<double>(type: "REAL", nullable: true),
                    MaxWatts = table.Column<double>(type: "REAL", nullable: true),
                    AverageCadence = table.Column<double>(type: "REAL", nullable: true),
                    Calories = table.Column<double>(type: "REAL", nullable: true),
                    SufferScore = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityCommentaries",
                columns: table => new
                {
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    CommentaryText = table.Column<string>(type: "TEXT", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModelUsed = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityCommentaries", x => x.ActivityId);
                });

            migrationBuilder.CreateTable(
                name: "ActivityDataPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    TimeOffset = table.Column<int>(type: "INTEGER", nullable: false),
                    Distance = table.Column<double>(type: "REAL", nullable: true),
                    HeartRate = table.Column<double>(type: "REAL", nullable: true),
                    Watts = table.Column<double>(type: "REAL", nullable: true),
                    Cadence = table.Column<double>(type: "REAL", nullable: true),
                    Altitude = table.Column<double>(type: "REAL", nullable: true),
                    Grade = table.Column<double>(type: "REAL", nullable: true),
                    Velocity = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDataPoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDataPoints_ActivityId_TimeOffset",
                table: "ActivityDataPoints",
                columns: new[] { "ActivityId", "TimeOffset" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "ActivityCommentaries");

            migrationBuilder.DropTable(
                name: "ActivityDataPoints");
        }
    }
}

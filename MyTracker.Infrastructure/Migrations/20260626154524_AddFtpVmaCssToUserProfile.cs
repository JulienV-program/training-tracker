using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFtpVmaCssToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CriticalSwimSpeedMinPer100m",
                table: "UserProfile",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FtpWatts",
                table: "UserProfile",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VmaMinPerKm",
                table: "UserProfile",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriticalSwimSpeedMinPer100m",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "FtpWatts",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "VmaMinPerKm",
                table: "UserProfile");
        }
    }
}

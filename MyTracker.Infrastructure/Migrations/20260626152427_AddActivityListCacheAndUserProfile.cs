using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityListCacheAndUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFullyImported",
                table: "Activities",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Backfill : les activités déjà importées avant cette migration (qui ont des points de
            // données en base) doivent rester marquées comme complètement importées.
            migrationBuilder.Sql(
                "UPDATE Activities SET IsFullyImported = 1 WHERE Id IN (SELECT DISTINCT ActivityId FROM ActivityDataPoints);");

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Sex = table.Column<string>(type: "TEXT", nullable: false),
                    HeightCm = table.Column<double>(type: "REAL", nullable: false),
                    WeightKg = table.Column<double>(type: "REAL", nullable: false),
                    MaxHeartRate = table.Column<int>(type: "INTEGER", nullable: false),
                    RestingHeartRate = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.DropColumn(
                name: "IsFullyImported",
                table: "Activities");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class LoginAttempt_LatLong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "AspNetLoginAttempts",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "AspNetLoginAttempts",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AspNetLoginAttempts");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AspNetLoginAttempts");
        }
    }
}

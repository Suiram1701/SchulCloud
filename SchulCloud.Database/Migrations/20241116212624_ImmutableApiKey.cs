using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class ImmutableApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "AspNetApiKeys");

            migrationBuilder.AddColumn<bool>(
                name: "AllPermissions",
                table: "AspNetApiKeys",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllPermissions",
                table: "AspNetApiKeys");

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "AspNetApiKeys",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}

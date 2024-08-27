using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class Passkeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsUsernameless",
                table: "AspNetCredentials",
                newName: "IsPasskey");

            migrationBuilder.AddColumn<bool>(
                name: "PasskeysEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasskeysEnabled",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "IsPasskey",
                table: "AspNetCredentials",
                newName: "IsUsernameless");
        }
    }
}

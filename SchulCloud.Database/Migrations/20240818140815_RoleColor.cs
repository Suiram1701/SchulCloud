using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class RoleColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "AspNetRoles");

            migrationBuilder.AddColumn<int>(
                name: "ArgbColor",
                table: "AspNetRoles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArgbColor",
                table: "AspNetRoles");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "AspNetRoles",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true);
        }
    }
}

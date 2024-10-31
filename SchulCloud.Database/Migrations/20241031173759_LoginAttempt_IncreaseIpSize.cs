using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class LoginAttempt_IncreaseIpSize : Migration
    {
        // Migration created manually because rge migration builder doesn't notice the change of the max length.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "IpAddress",
                table: "AspNetLoginAttempts",
                type: "bytea",
                oldMaxLength: 4,
                maxLength: 16);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "IpAddress",
                table: "AspNetLoginAttempts",
                type: "bytea",
                oldMaxLength: 16,
                maxLength: 4);
        }
    }
}

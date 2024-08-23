using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class Fido2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetCredentials",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "bytea", maxLength: 256, nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SecurityKeyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsUsernameless = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Transports = table.Column<int[]>(type: "integer[]", nullable: true),
                    IsBackupEligible = table.Column<bool>(type: "boolean", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "boolean", nullable: false),
                    AttestationObject = table.Column<byte[]>(type: "bytea", nullable: false),
                    AttestationClientDataJson = table.Column<byte[]>(type: "bytea", nullable: false),
                    AttestationFormat = table.Column<string>(type: "text", nullable: false),
                    RegDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AaGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetCredentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetCredentialDeviceKeys",
                columns: table => new
                {
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicDeviceKey = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetCredentialDeviceKeys", x => new { x.CredentialId, x.Id });
                    table.ForeignKey(
                        name: "FK_AspNetCredentialDeviceKeys_AspNetCredentials_CredentialId",
                        column: x => x.CredentialId,
                        principalTable: "AspNetCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetCredentials_UserId_SecurityKeyName",
                table: "AspNetCredentials",
                columns: new[] { "UserId", "SecurityKeyName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetCredentialDeviceKeys");

            migrationBuilder.DropTable(
                name: "AspNetCredentials");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class Remove_FIDO2_DPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetCredentialDeviceKeys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}

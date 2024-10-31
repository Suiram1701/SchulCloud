using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulCloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class LoginAttempt_SummarizeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            #region Added manually to reduce loss of data
            migrationBuilder.RenameColumn(
                name: "FailReason",
                newName: "Result",
                table: "AspNetLoginAttempts");

            if (migrationBuilder.IsNpgsql())
            {
                migrationBuilder.Sql("""
                    UPDATE public."AspNetLoginAttempts"
                        SET "Result" = COALESCE("Result", -1) + 1;
                    """);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Unable to update data of AspNetLoginAttempts because provider isn't Npgsql.");
            }

            migrationBuilder.AlterColumn<int>(
                name: "Result",
                table: "AspNetLoginAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            #endregion


            //migrationBuilder.DropColumn(
            //    name: "FailReason",
            //    table: "AspNetLoginAttempts");

            migrationBuilder.DropColumn(
                name: "Succeeded",
                table: "AspNetLoginAttempts");

            //migrationBuilder.AddColumn<int>(
            //    name: "Result",
            //    table: "AspNetLoginAttempts",
            //    type: "integer",
            //    nullable: false,
            //    defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "Result",
            //    table: "AspNetLoginAttempts");

            //migrationBuilder.AddColumn<int>(
            //    name: "FailReason",
            //    table: "AspNetLoginAttempts",
            //    type: "integer",
            //    nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Succeeded",
                table: "AspNetLoginAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            #region Added manually to reduce loss of data
            migrationBuilder.RenameColumn(
                name: "Result",
                newName: "FailReason",
                table: "AspNetLoginAttempts");

            migrationBuilder.AlterColumn<int>(
                name: "FailReason",
                table: "AspNetLoginAttempts",
                type: "integer",
                nullable: true,
                defaultValue: null);

            if (migrationBuilder.IsNpgsql())
            {
                migrationBuilder.Sql("""
                    UPDATE public."AspNetLoginAttempts"
                        SET "FailReason" = CASE
                                               WHEN "FailReason" = 0 THEN NULL
                                               ELSE "FailReason" - 1
                                           END,
                            "Succeeded" = ("FailReason" IS NULL);
                    """);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Unable to update data of AspNetLoginAttempts because provider isn't Npgsql.");
            }
            #endregion
        }
    }
}

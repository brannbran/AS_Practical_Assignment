using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AS_Practical_Assignment.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailOtpTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOtpTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailOtpTokens_CreatedDate",
                table: "EmailOtpTokens",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOtpTokens_Email",
                table: "EmailOtpTokens",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOtpTokens_ExpiryDate",
                table: "EmailOtpTokens",
                column: "ExpiryDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailOtpTokens");
        }
    }
}

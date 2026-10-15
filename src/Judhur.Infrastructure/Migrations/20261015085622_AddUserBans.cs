using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Judhur.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "AppUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BannedAtUtc",
                table: "AppUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "BannedAtUtc",
                table: "AppUsers");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Judhur.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportsModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_PropertyId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_PropertyId",
                table: "Reports");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Reports",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_PropertyId_Status",
                table: "Reports",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_PropertyId",
                table: "Reports",
                columns: new[] { "ReporterId", "PropertyId" },
                unique: true,
                filter: "[Status] = N'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_PropertyId_Status",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_PropertyId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Reports");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_PropertyId",
                table: "Reports",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_PropertyId",
                table: "Reports",
                columns: new[] { "ReporterId", "PropertyId" },
                unique: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Judhur.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_LastMessageAtUtc",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_SellerId",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_CreatedAtUtc",
                table: "Reports",
                columns: new[] { "ReporterId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_BuyerId_LastMessageAtUtc",
                table: "Conversations",
                columns: new[] { "BuyerId", "LastMessageAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_SellerId_LastMessageAtUtc",
                table: "Conversations",
                columns: new[] { "SellerId", "LastMessageAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_CreatedAtUtc",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_BuyerId_LastMessageAtUtc",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_SellerId_LastMessageAtUtc",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageAtUtc",
                table: "Conversations",
                column: "LastMessageAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_SellerId",
                table: "Conversations",
                column: "SellerId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Judhur.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_SenderId_ReadAtUtc",
                table: "Messages",
                columns: new[] { "ConversationId", "SenderId", "ReadAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId_SenderId_ReadAtUtc",
                table: "Messages");
        }
    }
}

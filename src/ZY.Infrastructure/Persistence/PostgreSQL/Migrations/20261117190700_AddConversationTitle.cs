using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZY.Infrastructure.Persistence.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "conversations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Nova conversa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "conversations");
        }
    }
}

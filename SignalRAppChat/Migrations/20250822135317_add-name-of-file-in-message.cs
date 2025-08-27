using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRAppChat.Migrations
{
    /// <inheritdoc />
    public partial class addnameoffileinmessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Messages");
        }
    }
}

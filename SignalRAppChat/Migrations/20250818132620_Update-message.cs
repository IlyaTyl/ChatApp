using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRAppChat.Migrations
{
    /// <inheritdoc />
    public partial class Updatemessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "Messages",
                newName: "FilePath");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Messages",
                newName: "ImagePath");
        }
    }
}

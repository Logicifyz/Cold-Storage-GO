using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cold_Storage_GO.Migrations
{
    /// <inheritdoc />
    public partial class UpdateResponseTypeStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AIResponseLogs",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "AIResponseLogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");
        }
    }
}

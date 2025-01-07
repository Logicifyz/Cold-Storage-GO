using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cold_Storage_GO.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "SupportTickets",  // Replace with your actual table name
                type: "varchar(255)",      // Replace with the correct type if necessary
                nullable: true,            // Allow null values
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: false);      // Remove the NOT NULL constraint
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "SupportTickets",  // Replace with your actual table name
                type: "varchar(255)",      // Replace with the correct type if necessary
                nullable: false,           // Revert back to NOT NULL
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true);       // Revert to NOT NULL constraint
        }
    }
}

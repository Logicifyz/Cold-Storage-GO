using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cold_Storage_GO.Migrations
{
    /// <inheritdoc />
    public partial class addedChatMessagesAGAIN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupportTicketTicketId",
                table: "ChatMessages",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketId",
                table: "ChatMessages",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SupportTicketTicketId",
                table: "ChatMessages",
                column: "SupportTicketTicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_SupportTickets_SupportTicketTicketId",
                table: "ChatMessages",
                column: "SupportTicketTicketId",
                principalTable: "SupportTickets",
                principalColumn: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_SupportTickets_SupportTicketTicketId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SupportTicketTicketId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "SupportTicketTicketId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "ChatMessages");
        }
    }
}

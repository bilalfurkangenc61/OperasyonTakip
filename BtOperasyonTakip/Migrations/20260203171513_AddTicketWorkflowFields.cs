using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CanliAcildiTarihi",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanliNotu",
                table: "Tickets",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntegrasyonNotu",
                table: "Tickets",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EntegreOlabilirMi",
                table: "Tickets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MailGonderildiMi",
                table: "Tickets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailNotu",
                table: "Tickets",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Operasyon1OnayTarihi",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Operasyon2OnayTarihi",
                table: "Tickets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanliAcildiTarihi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CanliNotu",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EntegrasyonNotu",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EntegreOlabilirMi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MailGonderildiMi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MailNotu",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Operasyon1OnayTarihi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Operasyon2OnayTarihi",
                table: "Tickets");
        }
    }
}

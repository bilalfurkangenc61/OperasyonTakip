using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketAssignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AtananOperasyonKullaniciAdi",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AtananOperasyonUserId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtanmaTarihi",
                table: "Tickets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtananOperasyonKullaniciAdi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AtananOperasyonUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AtanmaTarihi",
                table: "Tickets");
        }
    }
}

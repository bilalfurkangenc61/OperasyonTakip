using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddUyumApprovalFieldsToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UyumKararAciklamasi",
                table: "Tickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UyumOnayTarihi",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UyumOnaylayanKullaniciAdi",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UyumOnaylayanUserId",
                table: "Tickets",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UyumKararAciklamasi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UyumOnayTarihi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UyumOnaylayanKullaniciAdi",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UyumOnaylayanUserId",
                table: "Tickets");
        }
    }
}

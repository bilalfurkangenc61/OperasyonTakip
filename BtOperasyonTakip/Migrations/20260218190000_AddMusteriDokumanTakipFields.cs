using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    public partial class AddMusteriDokumanTakipFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DokumanKontrolBaslangicTarihi",
                table: "Musteriler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DokumanGonderimSayisi",
                table: "Musteriler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DokumanGonderildiMi",
                table: "Musteriler",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DokumanKontrolBaslangicTarihi",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DokumanGonderimSayisi",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DokumanGonderildiMi",
                table: "Musteriler");
        }
    }
}

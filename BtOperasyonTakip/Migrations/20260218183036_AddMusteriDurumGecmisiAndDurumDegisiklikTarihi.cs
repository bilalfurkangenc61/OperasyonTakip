using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddMusteriDurumGecmisiAndDurumDegisiklikTarihi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DurumDegisiklikTarihi",
                table: "Musteriler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MusteriDurumGecmisleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriID = table.Column<int>(type: "int", nullable: false),
                    EskiDurum = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    YeniDurum = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DegistirenKullanici = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusteriDurumGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusteriDurumGecmisleri_Musteriler_MusteriID",
                        column: x => x.MusteriID,
                        principalTable: "Musteriler",
                        principalColumn: "MusteriID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusteriDurumGecmisleri_MusteriID",
                table: "MusteriDurumGecmisleri",
                column: "MusteriID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusteriDurumGecmisleri");

            migrationBuilder.DropColumn(
                name: "DurumDegisiklikTarihi",
                table: "Musteriler");
        }
    }
}

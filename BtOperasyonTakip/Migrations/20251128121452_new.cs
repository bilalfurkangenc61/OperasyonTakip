using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hatalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HataAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HataAciklama = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KategoriBilgisi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlusturanUserId = table.Column<int>(type: "int", nullable: true),
                    OlusturanKullaniciAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecilenHataId = table.Column<int>(type: "int", nullable: true),
                    OperasyonCevabi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CevaplayanUserId = table.Column<int>(type: "int", nullable: true),
                    CevaplayanKullaniciAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CevaplaamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hatalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IletisimBilgileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IletisimBilgileri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JiraTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JiraId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TalepKonusu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TalepAcan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JiraTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Musteriler",
                columns: table => new
                {
                    MusteriID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Firma = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Teknoloji = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TalepSahibi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirmaYetkilisi = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Musteriler", x => x.MusteriID);
                });

            migrationBuilder.CreateTable(
                name: "Parametreler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tur = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametreler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriWebSitesi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    YazilimciAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    YazilimciSoyadi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IrtibatNumarasi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Mail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TeknolojiBilgisi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KararAciklamasi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturanUserId = table.Column<int>(type: "int", nullable: false),
                    OlusturanKullaniciAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnaylayanUserId = table.Column<int>(type: "int", nullable: true),
                    OnaylayanKullaniciAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OnaylamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MusteriID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToplantiNotlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotIcerigi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EkleyenKisi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToplantiNotlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JiraYorumlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JiraTaskId = table.Column<int>(type: "int", nullable: false),
                    YorumMetni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ekleyen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JiraYorumlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JiraYorumlar_JiraTasks_JiraTaskId",
                        column: x => x.JiraTaskId,
                        principalTable: "JiraTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detaylar",
                columns: table => new
                {
                    DetayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriID = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gorusulen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kekleyen = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detaylar", x => x.DetayID);
                    table.ForeignKey(
                        name: "FK_Detaylar_Musteriler_MusteriID",
                        column: x => x.MusteriID,
                        principalTable: "Musteriler",
                        principalColumn: "MusteriID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detaylar_MusteriID",
                table: "Detaylar",
                column: "MusteriID");

            migrationBuilder.CreateIndex(
                name: "IX_JiraYorumlar_JiraTaskId",
                table: "JiraYorumlar",
                column: "JiraTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detaylar");

            migrationBuilder.DropTable(
                name: "Hatalar");

            migrationBuilder.DropTable(
                name: "IletisimBilgileri");

            migrationBuilder.DropTable(
                name: "JiraYorumlar");

            migrationBuilder.DropTable(
                name: "Parametreler");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "ToplantiNotlari");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Musteriler");

            migrationBuilder.DropTable(
                name: "JiraTasks");
        }
    }
}

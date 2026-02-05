using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class TicketAtamaLoglari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketAtamaLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    EskiOperasyonUserId = table.Column<int>(type: "int", nullable: true),
                    EskiOperasyonKullaniciAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    YeniOperasyonUserId = table.Column<int>(type: "int", nullable: false),
                    YeniOperasyonKullaniciAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DegisiklikNedeni = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DegistirenUserId = table.Column<int>(type: "int", nullable: false),
                    DegistirenKullaniciAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DegisiklikTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAtamaLoglari", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketAtamaLoglari");
        }
    }
}

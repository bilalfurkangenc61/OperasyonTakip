using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmaAdiToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirmaAdi",
                table: "Tickets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirmaAdi",
                table: "Tickets");
        }
    }
}

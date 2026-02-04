using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketMusteriTipi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MusteriTipi",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MusteriTipi",
                table: "Tickets");
        }
    }
}

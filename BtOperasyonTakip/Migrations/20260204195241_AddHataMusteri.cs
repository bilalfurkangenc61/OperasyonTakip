using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtOperasyonTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddHataMusteri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MusteriID",
                table: "Hatalar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hatalar_MusteriID",
                table: "Hatalar",
                column: "MusteriID");

            migrationBuilder.AddForeignKey(
                name: "FK_Hatalar_Musteriler_MusteriID",
                table: "Hatalar",
                column: "MusteriID",
                principalTable: "Musteriler",
                principalColumn: "MusteriID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hatalar_Musteriler_MusteriID",
                table: "Hatalar");

            migrationBuilder.DropIndex(
                name: "IX_Hatalar_MusteriID",
                table: "Hatalar");

            migrationBuilder.DropColumn(
                name: "MusteriID",
                table: "Hatalar");
        }
    }
}

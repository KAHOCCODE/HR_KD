using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class DeleteFKMaLoaiNgayNghiToLoNgayNghiConnectToSoPhepDU : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoDuPhep_LoaiNgayNghi_LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep");

            migrationBuilder.DropIndex(
                name: "IX_SoDuPhep_LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep");

            migrationBuilder.DropColumn(
                name: "LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoDuPhep_LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep",
                column: "LoaiNgayNghiMaLoaiNgayNghi");

            migrationBuilder.AddForeignKey(
                name: "FK_SoDuPhep_LoaiNgayNghi_LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep",
                column: "LoaiNgayNghiMaLoaiNgayNghi",
                principalTable: "LoaiNgayNghi",
                principalColumn: "MaLoaiNgayNghi");
        }
    }
}

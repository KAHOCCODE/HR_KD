using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsToLoaiNgayNghi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoLanDangKyToiDa",
                table: "LoaiNgayNghi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoNgayNghiToiDa",
                table: "LoaiNgayNghi",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoLanDangKyToiDa",
                table: "LoaiNgayNghi");

            migrationBuilder.DropColumn(
                name: "SoNgayNghiToiDa",
                table: "LoaiNgayNghi");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumnsToLoaiNgayNghi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoTinhVaoLuong",
                table: "LoaiNgayNghi",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HuongLuong",
                table: "LoaiNgayNghi",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TinhVaoPhepNam",
                table: "LoaiNgayNghi",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoTinhVaoLuong",
                table: "LoaiNgayNghi");

            migrationBuilder.DropColumn(
                name: "HuongLuong",
                table: "LoaiNgayNghi");

            migrationBuilder.DropColumn(
                name: "TinhVaoPhepNam",
                table: "LoaiNgayNghi");
        }
    }
}

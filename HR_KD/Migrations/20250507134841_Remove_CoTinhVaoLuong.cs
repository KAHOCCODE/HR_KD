using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class Remove_CoTinhVaoLuong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoTinhVaoLuong",
                table: "LoaiNgayNghi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoTinhVaoLuong",
                table: "LoaiNgayNghi",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

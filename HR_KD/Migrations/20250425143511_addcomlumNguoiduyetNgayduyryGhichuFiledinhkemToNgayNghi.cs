using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class addcomlumNguoiduyetNgayduyryGhichuFiledinhkemToNgayNghi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileDinhKem",
                table: "NgayNghi",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "NgayNghi",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyet",
                table: "NgayNghi",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "NguoiDuyetId",
                table: "NgayNghi",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NgayNghi_NguoiDuyetId",
                table: "NgayNghi",
                column: "NguoiDuyetId");

            migrationBuilder.AddForeignKey(
                name: "FK_NgayNghi_NhanVien_NguoiDuyetId",
                table: "NgayNghi",
                column: "NguoiDuyetId",
                principalTable: "NhanVien",
                principalColumn: "MaNV");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NgayNghi_NhanVien_NguoiDuyetId",
                table: "NgayNghi");

            migrationBuilder.DropIndex(
                name: "IX_NgayNghi_NguoiDuyetId",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "FileDinhKem",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "NgayDuyet",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "NguoiDuyetId",
                table: "NgayNghi");
        }
    }
}

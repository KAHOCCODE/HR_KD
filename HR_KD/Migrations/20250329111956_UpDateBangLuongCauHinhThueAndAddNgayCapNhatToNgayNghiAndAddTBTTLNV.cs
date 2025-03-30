using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class UpDateBangLuongCauHinhThueAndAddNgayCapNhatToNgayNghiAndAddTBTTLNV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TongLuong",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "TenCauHinh",
                table: "CauHinhLuongThue");

            migrationBuilder.DropColumn(
                name: "PhuCap",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "ThuNhapKhac",
                table: "BangLuong");

            migrationBuilder.RenameColumn(
                name: "LuongCoBan",
                table: "BangLuong",
                newName: "ThueTNCN");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapNhat",
                table: "NgayNghi",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MucLuongDen",
                table: "CauHinhLuongThue",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MucLuongTu",
                table: "CauHinhLuongThue",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BHTN",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BHXH",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BHYT",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LuongTangCa",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LuongThem",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PhuCapThem",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThucNhan",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
            name: "TongLuong",
            table: "BangLuong",
            type: "decimal(20,2)",
            nullable: false,
            computedColumnSql: "([LuongThem] + [LuongTangCa] + [PhuCapThem] - ([BHXH] + [BHYT] + [BHTN] + [ThueTNCN]))");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayCapNhat",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "MucLuongDen",
                table: "CauHinhLuongThue");

            migrationBuilder.DropColumn(
                name: "MucLuongTu",
                table: "CauHinhLuongThue");

            migrationBuilder.DropColumn(
                name: "BHTN",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "BHXH",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "BHYT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "LuongTangCa",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "LuongThem",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "PhuCapThem",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "ThucNhan",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "TongLuong",
                table: "BangLuong");

            migrationBuilder.RenameColumn(
                name: "ThueTNCN",
                table: "BangLuong",
                newName: "LuongCoBan");

            migrationBuilder.AddColumn<string>(
                name: "TenCauHinh",
                table: "CauHinhLuongThue",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PhuCap",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThuNhapKhac",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: true,
                defaultValue: 0m);
        }
    }
}

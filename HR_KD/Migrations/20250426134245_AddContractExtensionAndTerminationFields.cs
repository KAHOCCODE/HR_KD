using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddContractExtensionAndTerminationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GiaHanToiDa",
                table: "LoaiHopDongs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TiLeLuong",
                table: "LoaiHopDongs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoNghiViec",
                table: "HopDongLaoDongs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgayNghiViec",
                table: "HopDongLaoDongs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiHopDong",
                table: "HopDongLaoDongs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiaHanToiDa",
                table: "LoaiHopDongs");

            migrationBuilder.DropColumn(
                name: "TiLeLuong",
                table: "LoaiHopDongs");

            migrationBuilder.DropColumn(
                name: "LyDoNghiViec",
                table: "HopDongLaoDongs");

            migrationBuilder.DropColumn(
                name: "NgayNghiViec",
                table: "HopDongLaoDongs");

            migrationBuilder.DropColumn(
                name: "TrangThaiHopDong",
                table: "HopDongLaoDongs");
        }
    }
}

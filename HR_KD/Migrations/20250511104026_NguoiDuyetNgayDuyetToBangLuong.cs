using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class NguoiDuyetNgayDuyetToBangLuong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyetGD",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyetKT",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyetNV",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayGuiKT",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayGuiNV",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTraVeTuGD",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTraVeTuKT",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTuChoiNV",
                table: "BangLuong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyetGD",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyetKT",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyetNV",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiGuiKT",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiGuiNV",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiTraVeGD",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiTraVeKT",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiTraVeTuGD",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiTraVeTuKT",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiTuChoiNV",
                table: "BangLuong",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayDuyetGD",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NgayDuyetKT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NgayDuyetNV",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NgayGuiKT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NgayGuiNV",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NgayTraVeTuGD",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NgayTraVeTuKT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NgayTuChoiNV",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiDuyetGD",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiDuyetKT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiDuyetNV",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiGuiKT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiGuiNV",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiTraVeGD",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiTraVeKT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiTraVeTuGD",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiTraVeTuKT",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "NguoiTuChoiNV",
                table: "BangLuong");
        }
    }
}

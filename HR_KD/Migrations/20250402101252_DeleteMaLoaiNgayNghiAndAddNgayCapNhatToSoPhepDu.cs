using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class DeleteMaLoaiNgayNghiAndAddNgayCapNhatToSoPhepDu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__SoDuPhep__MaLoai__5535A963",
                table: "SoDuPhep");

            migrationBuilder.DropPrimaryKey(
                name: "PK__SoDuPhep__070385985BD7438C",
                table: "SoDuPhep");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SoDuPhep_MaLoaiNgayNghi' AND object_id = OBJECT_ID('SoDuPhep'))
                DROP INDEX [IX_SoDuPhep_MaLoaiNgayNghi] ON [SoDuPhep];
            ");


            migrationBuilder.DropColumn(
                name: "MaLoaiNgayNghi",
                table: "SoDuPhep");

            migrationBuilder.AddColumn<int>(
                name: "LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapNhat",
                table: "SoDuPhep",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK__SoDuPhep__070385985BD7438C",
                table: "SoDuPhep",
                columns: new[] { "MaNV", "Nam" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoDuPhep_LoaiNgayNghi_LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep");

            migrationBuilder.DropPrimaryKey(
                name: "PK__SoDuPhep__070385985BD7438C",
                table: "SoDuPhep");

            migrationBuilder.DropIndex(
                name: "IX_SoDuPhep_LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep");

            migrationBuilder.DropColumn(
                name: "LoaiNgayNghiMaLoaiNgayNghi",
                table: "SoDuPhep");

            migrationBuilder.DropColumn(
                name: "NgayCapNhat",
                table: "SoDuPhep");

            migrationBuilder.AddColumn<int>(
                name: "MaLoaiNgayNghi",
                table: "SoDuPhep",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK__SoDuPhep__070385985BD7438C",
                table: "SoDuPhep",
                columns: new[] { "MaNV", "MaLoaiNgayNghi", "Nam" });

            migrationBuilder.CreateIndex(
                name: "IX_SoDuPhep_MaLoaiNgayNghi",
                table: "SoDuPhep",
                column: "MaLoaiNgayNghi");

            migrationBuilder.AddForeignKey(
                name: "FK__SoDuPhep__MaLoai__5535A963",
                table: "SoDuPhep",
                column: "MaLoaiNgayNghi",
                principalTable: "LoaiNgayNghi",
                principalColumn: "MaLoaiNgayNghi");
        }
    }
}

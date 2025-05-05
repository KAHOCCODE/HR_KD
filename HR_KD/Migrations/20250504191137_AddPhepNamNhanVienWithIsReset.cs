using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddPhepNamNhanVienWithIsReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhepNamNhanVien",
                columns: table => new
                {
                    MaNV = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "date", nullable: false),
                    SoNgayPhepDuocCap = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SoNgayDaSuDung = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CauHinhPhepNamId = table.Column<int>(type: "int", nullable: false),
                    IsReset = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhepNamNhanVien", x => new { x.MaNV, x.Nam });
                    table.ForeignKey(
                        name: "FK_PhepNamNhanVien_CauHinhPhepNam",
                        column: x => x.CauHinhPhepNamId,
                        principalTable: "CauHinhPhepNam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhepNamNhanVien_NhanVien",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhepNamNhanVien_CauHinhPhepNamId",
                table: "PhepNamNhanVien",
                column: "CauHinhPhepNamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhepNamNhanVien");
        }
    }
}

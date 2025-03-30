using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddTableThongTinLuongNV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThongTinLuongNVs",
                columns: table => new
                {
                    MaLuongNV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNv = table.Column<int>(type: "int", nullable: false),
                    LuongCoBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuCapCoDinh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThuongCoDinh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NgayApDng = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongTinLuongNVs", x => x.MaLuongNV);
                    table.ForeignKey(
                        name: "FK_ThongTinLuongNVs_NhanVien_MaNv",
                        column: x => x.MaNv,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThongTinLuongNVs_MaNv",
                table: "ThongTinLuongNVs",
                column: "MaNv");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThongTinLuongNVs");
        }
    }
}

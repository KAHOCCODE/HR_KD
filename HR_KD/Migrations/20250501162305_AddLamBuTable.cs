using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddLamBuTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LamBus",
                columns: table => new
                {
                    MaLamBu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<int>(type: "int", nullable: false),
                    NgayLamViec = table.Column<DateOnly>(type: "date", nullable: false),
                    GioVao = table.Column<TimeOnly>(type: "time", nullable: true),
                    GioRa = table.Column<TimeOnly>(type: "time", nullable: true),
                    TongGio = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NhanVienMaNv = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LamBus", x => x.MaLamBu);
                    table.ForeignKey(
                        name: "FK_LamBus_NhanVien_NhanVienMaNv",
                        column: x => x.NhanVienMaNv,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LamBus_NhanVienMaNv",
                table: "LamBus",
                column: "NhanVienMaNv");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LamBus");
        }
    }
}

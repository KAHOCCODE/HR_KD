using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddTongGioThieuToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TongGioThieus",
                columns: table => new
                {
                    MaTongGioThieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNv = table.Column<int>(type: "int", nullable: false),
                    NgayBatDauThieu = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayKetThucThieu = table.Column<DateOnly>(type: "date", nullable: false),
                    TongGioConThieu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongGioLamBu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaNvNavigationMaNv = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TongGioThieus", x => x.MaTongGioThieu);
                    table.ForeignKey(
                        name: "FK_TongGioThieus_NhanVien_MaNvNavigationMaNv",
                        column: x => x.MaNvNavigationMaNv,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TongGioThieus_MaNvNavigationMaNv",
                table: "TongGioThieus",
                column: "MaNvNavigationMaNv");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TongGioThieus");
        }
    }
}

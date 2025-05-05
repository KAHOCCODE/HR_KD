using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddGioThieuToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GioThieus",
                columns: table => new
                {
                    MaGioThieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayThieu = table.Column<DateOnly>(type: "date", nullable: false),
                    TongGioThieu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaNv = table.Column<int>(type: "int", nullable: false),
                    MaNvNavigationMaNv = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioThieus", x => x.MaGioThieu);
                    table.ForeignKey(
                        name: "FK_GioThieus_NhanVien_MaNvNavigationMaNv",
                        column: x => x.MaNvNavigationMaNv,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GioThieus_MaNvNavigationMaNv",
                table: "GioThieus",
                column: "MaNvNavigationMaNv");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GioThieus");
        }
    }
}

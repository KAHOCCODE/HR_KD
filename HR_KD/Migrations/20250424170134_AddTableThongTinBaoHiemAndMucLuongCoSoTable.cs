using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddTableThongTinBaoHiemAndMucLuongCoSoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MucLuongCoSos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LuongCoSo = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    NgayHieuLuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHieuLuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucLuongCoSos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThongTinBaoHiems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiBaoHiem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TyLeNguoiLaoDong = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TyLeNhaTuyenDung = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VungLuongTheoDiaPhuongId = table.Column<int>(type: "int", nullable: false),
                    MucLuongCoSoId = table.Column<int>(type: "int", nullable: false),
                    NgayHieuLuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHieuLuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongTinBaoHiems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongTinBaoHiems_MucLuongCoSos_MucLuongCoSoId",
                        column: x => x.MucLuongCoSoId,
                        principalTable: "MucLuongCoSos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThongTinBaoHiems_VungLuongTheoDiaPhuongs_VungLuongTheoDiaPhuongId",
                        column: x => x.VungLuongTheoDiaPhuongId,
                        principalTable: "VungLuongTheoDiaPhuongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThongTinBaoHiems_MucLuongCoSoId",
                table: "ThongTinBaoHiems",
                column: "MucLuongCoSoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongTinBaoHiems_VungLuongTheoDiaPhuongId",
                table: "ThongTinBaoHiems",
                column: "VungLuongTheoDiaPhuongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThongTinBaoHiems");

            migrationBuilder.DropTable(
                name: "MucLuongCoSos");
        }
    }
}

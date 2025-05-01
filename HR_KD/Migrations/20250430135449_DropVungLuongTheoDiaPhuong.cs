using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class DropVungLuongTheoDiaPhuong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThongTinBaoHiems_VungLuongTheoDiaPhuongs_VungLuongTheoDiaPhuongId",
                table: "ThongTinBaoHiems");

            migrationBuilder.DropTable(
                name: "VungLuongTheoDiaPhuongs");

            migrationBuilder.RenameColumn(
                name: "VungLuongTheoDiaPhuongId",
                table: "ThongTinBaoHiems",
                newName: "MucLuongToiThieuVungId");

            migrationBuilder.RenameIndex(
                name: "IX_ThongTinBaoHiems_VungLuongTheoDiaPhuongId",
                table: "ThongTinBaoHiems",
                newName: "IX_ThongTinBaoHiems_MucLuongToiThieuVungId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThongTinBaoHiems_MucLuongToiThieuVungs_MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems",
                column: "MucLuongToiThieuVungId",
                principalTable: "MucLuongToiThieuVungs",
                principalColumn: "VungLuong",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThongTinBaoHiems_MucLuongToiThieuVungs_MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems");

            migrationBuilder.RenameColumn(
                name: "MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems",
                newName: "VungLuongTheoDiaPhuongId");

            migrationBuilder.RenameIndex(
                name: "IX_ThongTinBaoHiems_MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems",
                newName: "IX_ThongTinBaoHiems_VungLuongTheoDiaPhuongId");

            migrationBuilder.CreateTable(
                name: "VungLuongTheoDiaPhuongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VungLuong = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    QuanHuyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TinhThanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VungLuongTheoDiaPhuongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VungLuongTheoDiaPhuongs_MucLuongToiThieuVungs_VungLuong",
                        column: x => x.VungLuong,
                        principalTable: "MucLuongToiThieuVungs",
                        principalColumn: "VungLuong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VungLuongTheoDiaPhuongs_VungLuong",
                table: "VungLuongTheoDiaPhuongs",
                column: "VungLuong");

            migrationBuilder.AddForeignKey(
                name: "FK_ThongTinBaoHiems_VungLuongTheoDiaPhuongs_VungLuongTheoDiaPhuongId",
                table: "ThongTinBaoHiems",
                column: "VungLuongTheoDiaPhuongId",
                principalTable: "VungLuongTheoDiaPhuongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

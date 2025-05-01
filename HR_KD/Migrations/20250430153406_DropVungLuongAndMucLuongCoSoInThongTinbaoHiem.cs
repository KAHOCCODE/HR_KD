using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class DropVungLuongAndMucLuongCoSoInThongTinbaoHiem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThongTinBaoHiems_MucLuongCoSos_MucLuongCoSoId",
                table: "ThongTinBaoHiems");

            migrationBuilder.DropForeignKey(
                name: "FK_ThongTinBaoHiems_MucLuongToiThieuVungs_MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems");

            migrationBuilder.DropIndex(
                name: "IX_ThongTinBaoHiems_MucLuongCoSoId",
                table: "ThongTinBaoHiems");

            migrationBuilder.DropIndex(
                name: "IX_ThongTinBaoHiems_MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems");

            migrationBuilder.DropColumn(
                name: "MucLuongCoSoId",
                table: "ThongTinBaoHiems");

            migrationBuilder.DropColumn(
                name: "MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MucLuongCoSoId",
                table: "ThongTinBaoHiems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ThongTinBaoHiems_MucLuongCoSoId",
                table: "ThongTinBaoHiems",
                column: "MucLuongCoSoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongTinBaoHiems_MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems",
                column: "MucLuongToiThieuVungId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThongTinBaoHiems_MucLuongCoSos_MucLuongCoSoId",
                table: "ThongTinBaoHiems",
                column: "MucLuongCoSoId",
                principalTable: "MucLuongCoSos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ThongTinBaoHiems_MucLuongToiThieuVungs_MucLuongToiThieuVungId",
                table: "ThongTinBaoHiems",
                column: "MucLuongToiThieuVungId",
                principalTable: "MucLuongToiThieuVungs",
                principalColumn: "VungLuong",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

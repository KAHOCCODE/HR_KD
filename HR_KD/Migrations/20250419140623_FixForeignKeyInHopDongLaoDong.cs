using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeyInHopDongLaoDong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HopDongLaoDongs_NhanVien_MaNvNavigationMaNv",
                table: "HopDongLaoDongs");

            migrationBuilder.DropIndex(
                name: "IX_HopDongLaoDongs_MaNvNavigationMaNv",
                table: "HopDongLaoDongs");

            migrationBuilder.DropColumn(
                name: "MaNvNavigationMaNv",
                table: "HopDongLaoDongs");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongLaoDongs_MaNv",
                table: "HopDongLaoDongs",
                column: "MaNv");

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongLaoDongs_NhanVien_MaNv",
                table: "HopDongLaoDongs",
                column: "MaNv",
                principalTable: "NhanVien",
                principalColumn: "MaNV",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HopDongLaoDongs_NhanVien_MaNv",
                table: "HopDongLaoDongs");

            migrationBuilder.DropIndex(
                name: "IX_HopDongLaoDongs_MaNv",
                table: "HopDongLaoDongs");

            migrationBuilder.AddColumn<int>(
                name: "MaNvNavigationMaNv",
                table: "HopDongLaoDongs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_HopDongLaoDongs_MaNvNavigationMaNv",
                table: "HopDongLaoDongs",
                column: "MaNvNavigationMaNv");

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongLaoDongs_NhanVien_MaNvNavigationMaNv",
                table: "HopDongLaoDongs",
                column: "MaNvNavigationMaNv",
                principalTable: "NhanVien",
                principalColumn: "MaNV",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

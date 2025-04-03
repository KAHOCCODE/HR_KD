using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class FixColumnDư : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoanQuyenHans_TaiKhoan_TaiKhoanUsername",
                table: "TaiKhoanQuyenHans");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoanQuyenHans_TaiKhoanUsername",
                table: "TaiKhoanQuyenHans");

            migrationBuilder.DropColumn(
                name: "TaiKhoanUsername",
                table: "TaiKhoanQuyenHans");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoanQuyenHans_QuyenHan_QuyenHanMaQuyenHan",
                table: "TaiKhoanQuyenHans");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoanQuyenHans_QuyenHanMaQuyenHan",
                table: "TaiKhoanQuyenHans");

            migrationBuilder.DropColumn(
                name: "QuyenHanMaQuyenHan",
                table: "TaiKhoanQuyenHans");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaiKhoanUsername",
                table: "TaiKhoanQuyenHans",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanQuyenHans_TaiKhoanUsername",
                table: "TaiKhoanQuyenHans",
                column: "TaiKhoanUsername");

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoanQuyenHans_TaiKhoan_TaiKhoanUsername",
                table: "TaiKhoanQuyenHans",
                column: "TaiKhoanUsername",
                principalTable: "TaiKhoan",
                principalColumn: "Username",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddColumn<string>(
                name: "QuyenHanMaQuyenHan",
                table: "TaiKhoanQuyenHans",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanQuyenHans_QuyenHanMaQuyenHan",
                table: "TaiKhoanQuyenHans",
                column: "QuyenHanMaQuyenHan");

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoanQuyenHans_QuyenHan_QuyenHanMaQuyenHan",
                table: "TaiKhoanQuyenHans",
                column: "QuyenHanMaQuyenHan",
                principalTable: "QuyenHan",
                principalColumn: "MaQuyenHan",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

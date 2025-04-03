using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyToTaiKhoanQuyenHan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "TaiKhoanQuyenHans",
                type: "nvarchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MaQuyenHan",
                table: "TaiKhoanQuyenHans",
                type: "nvarchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanQuyenHans_MaQuyenHan",
                table: "TaiKhoanQuyenHans",
                column: "MaQuyenHan");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanQuyenHans_Username",
                table: "TaiKhoanQuyenHans",
                column: "Username");

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoanQuyenHans_QuyenHan_MaQuyenHan",
                table: "TaiKhoanQuyenHans",
                column: "MaQuyenHan",
                principalTable: "QuyenHan",
                principalColumn: "MaQuyenHan",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoanQuyenHans_TaiKhoan_Username",
                table: "TaiKhoanQuyenHans",
                column: "Username",
                principalTable: "TaiKhoan",
                principalColumn: "Username",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "TaiKhoanQuyenHans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "MaQuyenHan",
                table: "TaiKhoanQuyenHans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)");

        }
    }
}

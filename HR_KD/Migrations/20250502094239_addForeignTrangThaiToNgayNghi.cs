using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class addForeignTrangThaiToNgayNghi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NgayNghi_TrangThais_TrangThaiMaTrangThai",
                table: "NgayNghi");

            migrationBuilder.DropIndex(
                name: "IX_NgayNghi_TrangThaiMaTrangThai",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "TrangThaiMaTrangThai",
                table: "NgayNghi");

            migrationBuilder.AddColumn<int>(
                name: "MaTrangThai",
                table: "NgayNghi",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NgayNghi_MaTrangThai",
                table: "NgayNghi",
                column: "MaTrangThai");

            migrationBuilder.AddForeignKey(
                name: "FK_NgayNghi_TrangThais_MaTrangThai",
                table: "NgayNghi",
                column: "MaTrangThai",
                principalTable: "TrangThais",
                principalColumn: "MaTrangThai",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NgayNghi_TrangThais_MaTrangThai",
                table: "NgayNghi");

            migrationBuilder.DropIndex(
                name: "IX_NgayNghi_MaTrangThai",
                table: "NgayNghi");

            migrationBuilder.DropColumn(
                name: "MaTrangThai",
                table: "NgayNghi");

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "NgayNghi",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "Chưa Duyệt");

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiMaTrangThai",
                table: "NgayNghi",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NgayNghi_TrangThaiMaTrangThai",
                table: "NgayNghi",
                column: "TrangThaiMaTrangThai");

            migrationBuilder.AddForeignKey(
                name: "FK_NgayNghi_TrangThais_TrangThaiMaTrangThai",
                table: "NgayNghi",
                column: "TrangThaiMaTrangThai",
                principalTable: "TrangThais",
                principalColumn: "MaTrangThai");
        }
    }
}

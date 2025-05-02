using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class DeleteForeignKeyTableTrangThai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DaoTao_PhongBan_MaPhongBanNavigationMaPhongBan",
                table: "DaoTao");

            migrationBuilder.DropIndex(
                name: "IX_DaoTao_MaPhongBanNavigationMaPhongBan",
                table: "DaoTao");

            migrationBuilder.DropColumn(
                name: "MaPhongBanNavigationMaPhongBan",
                table: "DaoTao");

            migrationBuilder.CreateIndex(
                name: "IX_DaoTao_MaPhongBan",
                table: "DaoTao",
                column: "MaPhongBan");

            migrationBuilder.AddForeignKey(
                name: "FK_DaoTao_PhongBan_MaPhongBan",
                table: "DaoTao",
                column: "MaPhongBan",
                principalTable: "PhongBan",
                principalColumn: "MaPhongBan",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DaoTao_PhongBan_MaPhongBan",
                table: "DaoTao");

            migrationBuilder.DropIndex(
                name: "IX_DaoTao_MaPhongBan",
                table: "DaoTao");

            migrationBuilder.AddColumn<int>(
                name: "MaPhongBanNavigationMaPhongBan",
                table: "DaoTao",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DaoTao_MaPhongBanNavigationMaPhongBan",
                table: "DaoTao",
                column: "MaPhongBanNavigationMaPhongBan");

            migrationBuilder.AddForeignKey(
                name: "FK_DaoTao_PhongBan_MaPhongBanNavigationMaPhongBan",
                table: "DaoTao",
                column: "MaPhongBanNavigationMaPhongBan",
                principalTable: "PhongBan",
                principalColumn: "MaPhongBan",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddTableLoaiHopDongAndFixHopDongLaoDongs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoaiHopDong",
                table: "HopDongLaoDongs");

            migrationBuilder.AddColumn<int>(
                name: "MaLoaiHopDong",
                table: "HopDongLaoDongs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LoaiHopDongs",
                columns: table => new
                {
                    MaLoaiHopDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiHopDong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThoiHanMacDinh = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiHopDongs", x => x.MaLoaiHopDong);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HopDongLaoDongs_MaLoaiHopDong",
                table: "HopDongLaoDongs",
                column: "MaLoaiHopDong");

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongLaoDongs_LoaiHopDongs_MaLoaiHopDong",
                table: "HopDongLaoDongs",
                column: "MaLoaiHopDong",
                principalTable: "LoaiHopDongs",
                principalColumn: "MaLoaiHopDong",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HopDongLaoDongs_LoaiHopDongs_MaLoaiHopDong",
                table: "HopDongLaoDongs");

            migrationBuilder.DropTable(
                name: "LoaiHopDongs");

            migrationBuilder.DropIndex(
                name: "IX_HopDongLaoDongs_MaLoaiHopDong",
                table: "HopDongLaoDongs");

            migrationBuilder.DropColumn(
                name: "MaLoaiHopDong",
                table: "HopDongLaoDongs");

            migrationBuilder.AddColumn<string>(
                name: "LoaiHopDong",
                table: "HopDongLaoDongs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

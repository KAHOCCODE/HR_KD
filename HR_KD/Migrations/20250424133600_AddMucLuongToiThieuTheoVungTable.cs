using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddMucLuongToiThieuTheoVungTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MucLuongToiThieuVungs",
                columns: table => new
                {
                    VungLuong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MucLuongToiThieuThang = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    MucLuongToiThieuGio = table.Column<decimal>(type: "decimal(18,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucLuongToiThieuVungs", x => x.VungLuong);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VungLuongTheoDiaPhuongs_VungLuong",
                table: "VungLuongTheoDiaPhuongs",
                column: "VungLuong");

            migrationBuilder.AddForeignKey(
                name: "FK_VungLuongTheoDiaPhuongs_MucLuongToiThieuVungs_VungLuong",
                table: "VungLuongTheoDiaPhuongs",
                column: "VungLuong",
                principalTable: "MucLuongToiThieuVungs",
                principalColumn: "VungLuong",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VungLuongTheoDiaPhuongs_MucLuongToiThieuVungs_VungLuong",
                table: "VungLuongTheoDiaPhuongs");

            migrationBuilder.DropTable(
                name: "MucLuongToiThieuVungs");

            migrationBuilder.DropIndex(
                name: "IX_VungLuongTheoDiaPhuongs_VungLuong",
                table: "VungLuongTheoDiaPhuongs");
        }
    }
}

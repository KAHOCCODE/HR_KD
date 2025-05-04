using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddPhepNam_ChinhsachTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CauHinhPhepNam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    SoNgayPhepMacDinh = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHinhPhepNam", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChinhSachPhepNam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChinhSach = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoNam = table.Column<int>(type: "int", nullable: false),
                    SoNgayCongThem = table.Column<int>(type: "int", nullable: false),
                    ApDungTuNam = table.Column<int>(type: "int", nullable: false),
                    ConHieuLuc = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChinhSachPhepNam", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CauHinhPhep_ChinhSach",
                columns: table => new
                {
                    CauHinhPhepNamId = table.Column<int>(type: "int", nullable: false),
                    ChinhSachPhepNamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHinhPhep_ChinhSach", x => new { x.CauHinhPhepNamId, x.ChinhSachPhepNamId });
                    table.ForeignKey(
                        name: "FK_CauHinhPhep_ChinhSach_CauHinhPhepNam",
                        column: x => x.CauHinhPhepNamId,
                        principalTable: "CauHinhPhepNam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CauHinhPhep_ChinhSach_ChinhSachPhepNam",
                        column: x => x.ChinhSachPhepNamId,
                        principalTable: "ChinhSachPhepNam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CauHinhPhep_ChinhSach_ChinhSachPhepNamId",
                table: "CauHinhPhep_ChinhSach",
                column: "ChinhSachPhepNamId");

            migrationBuilder.CreateIndex(
                name: "IX_CauHinhPhepNam_Nam",
                table: "CauHinhPhepNam",
                column: "Nam",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CauHinhPhep_ChinhSach");

            migrationBuilder.DropTable(
                name: "CauHinhPhepNam");

            migrationBuilder.DropTable(
                name: "ChinhSachPhepNam");
        }
    }
}

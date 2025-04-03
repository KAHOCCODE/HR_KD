using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddTableTaiKhoanQuyenHan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__TaiKhoan__MaQuye__4316F928",
                table: "TaiKhoan");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TaiKhoan_MaQuyenHan' AND object_id = OBJECT_ID('TaiKhoan'))
                DROP INDEX [IX_TaiKhoan_MaQuyenHan] ON [TaiKhoan];
            ");

            migrationBuilder.DropColumn(
                name: "MaQuyenHan",
                table: "TaiKhoan");

            migrationBuilder.CreateTable(
                name: "TaiKhoanQuyenHans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaQuyenHan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaiKhoanUsername = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    QuyenHanMaQuyenHan = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanQuyenHans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiKhoanQuyenHans_QuyenHan_QuyenHanMaQuyenHan",
                        column: x => x.QuyenHanMaQuyenHan,
                        principalTable: "QuyenHan",
                        principalColumn: "MaQuyenHan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaiKhoanQuyenHans_TaiKhoan_TaiKhoanUsername",
                        column: x => x.TaiKhoanUsername,
                        principalTable: "TaiKhoan",
                        principalColumn: "Username",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanQuyenHans_QuyenHanMaQuyenHan",
                table: "TaiKhoanQuyenHans",
                column: "QuyenHanMaQuyenHan");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanQuyenHans_TaiKhoanUsername",
                table: "TaiKhoanQuyenHans",
                column: "TaiKhoanUsername");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaiKhoanQuyenHans");

            migrationBuilder.AddColumn<string>(
                name: "MaQuyenHan",
                table: "TaiKhoan",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_MaQuyenHan",
                table: "TaiKhoan",
                column: "MaQuyenHan");

            migrationBuilder.AddForeignKey(
                name: "FK__TaiKhoan__MaQuye__4316F928",
                table: "TaiKhoan",
                column: "MaQuyenHan",
                principalTable: "QuyenHan",
                principalColumn: "MaQuyenHan");
        }
    }
}

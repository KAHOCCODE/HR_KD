using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class DeleteTableTrangThai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrangThais");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrangThais",
                columns: table => new
                {
                    MaTrangThai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrangThais", x => x.MaTrangThai);
                });

            migrationBuilder.InsertData(
                table: "TrangThais",
                columns: new[] { "MaTrangThai", "TenTrangThai" },
                values: new object[,]
                {
                    { 1, "Chờ duyệt" },
                    { 2, "Đã duyệt" },
                    { 3, "Từ chối" },
                    { 4, "Đã hủy" }
                });
        }
    }
}

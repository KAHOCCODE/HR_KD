using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddSoNgayChuaSuDungAndIsTinhLuongToPhepNamNhanVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTinhLuong",
                table: "PhepNamNhanVien",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SoNgayChuaSuDung",
                table: "PhepNamNhanVien",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTinhLuong",
                table: "PhepNamNhanVien");

            migrationBuilder.DropColumn(
                name: "SoNgayChuaSuDung",
                table: "PhepNamNhanVien");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class FixColumnNgayApDungInThongTinLuongNVs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NgayApDng",
                table: "ThongTinLuongNVs",
                newName: "NgayApDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NgayApDung",
                table: "ThongTinLuongNVs",
                newName: "NgayApDng");
        }
    }
}

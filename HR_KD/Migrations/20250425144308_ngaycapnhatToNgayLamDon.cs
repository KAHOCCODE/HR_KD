using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class ngaycapnhatToNgayLamDon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NgayCapNhat",
                table: "NgayNghi",
                newName: "NgayNgayLamDon");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NgayNgayLamDon",
                table: "NgayNghi",
                newName: "NgayCapNhat");
        }
    }
}

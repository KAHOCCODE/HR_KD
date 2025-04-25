using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class RenameNgayNgayLamDonToNgayLamDon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NgayNgayLamDon",
                table: "NgayNghi",
                newName: "NgayLamDon");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NgayLamDon",
                table: "NgayNghi",
                newName: "NgayNgayLamDon");
        }
    }
}

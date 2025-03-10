using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarToNhanVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm cột AvatarUrl vào bảng NhanVien
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "NhanVien",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa cột AvatarUrl nếu rollback migration
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "NhanVien");
        }
    }
}

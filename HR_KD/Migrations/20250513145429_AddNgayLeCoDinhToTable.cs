using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddNgayLeCoDinhToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NgayLeCoDinhs",
                columns: table => new
                {
                    MaNgayLe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNgayLe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayLe1 = table.Column<DateOnly>(type: "date", nullable: false),
                    SoNgayNghi = table.Column<int>(type: "int", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NgayLeCoDinhs", x => x.MaNgayLe);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NgayLeCoDinhs");
        }
    }
}

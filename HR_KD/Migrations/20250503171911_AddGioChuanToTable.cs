using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddGioChuanToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GioChuans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Thang1 = table.Column<int>(type: "int", nullable: false),
                    Thang2 = table.Column<int>(type: "int", nullable: false),
                    Thang3 = table.Column<int>(type: "int", nullable: false),
                    Thang4 = table.Column<int>(type: "int", nullable: false),
                    Thang5 = table.Column<int>(type: "int", nullable: false),
                    Thang6 = table.Column<int>(type: "int", nullable: false),
                    Thang7 = table.Column<int>(type: "int", nullable: false),
                    Thang8 = table.Column<int>(type: "int", nullable: false),
                    Thang9 = table.Column<int>(type: "int", nullable: false),
                    Thang10 = table.Column<int>(type: "int", nullable: false),
                    Thang11 = table.Column<int>(type: "int", nullable: false),
                    Thang12 = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    KichHoat = table.Column<bool>(type: "bit", nullable: false),
                    IdLichLamViec = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioChuans", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GioChuans");
        }
    }
}

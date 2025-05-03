using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class AddTongGiToChamCongGioRaVao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TongGio",
                table: "ChamCongGioRaVaos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TongGio",
                table: "ChamCongGioRaVaos");
        }
    }
}

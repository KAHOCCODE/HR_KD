using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class FixAddContractExtensionAndTerminationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThaiHopDong",
                table: "HopDongLaoDongs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrangThaiHopDong",
                table: "HopDongLaoDongs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}

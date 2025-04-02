using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class MoveInsuranceFieldsToThongTinLuongNV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Alter the TongLuong column to remove its computed column definition
            migrationBuilder.AlterColumn<decimal>(
                name: "TongLuong",
                table: "BangLuong",
                type: "decimal(20,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,2)",
                oldNullable: true,
                oldComputedColumnSql: "([LuongThem] + [LuongTangCa] + [PhuCapThem] - ([BHXH] + [BHYT] + [BHTN] + [ThueTNCN]))");

            // Step 2: Drop the dependent columns (BHTN, BHXH, BHYT)
            migrationBuilder.DropColumn(
                name: "BHTN",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "BHXH",
                table: "BangLuong");

            migrationBuilder.DropColumn(
                name: "BHYT",
                table: "BangLuong");

            // Step 3: Add the new columns to ThongTinLuongNVs
            migrationBuilder.AddColumn<decimal>(
                name: "BHTN",
                table: "ThongTinLuongNVs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BHXH",
                table: "ThongTinLuongNVs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BHYT",
                table: "ThongTinLuongNVs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the columns from ThongTinLuongNVs
            migrationBuilder.DropColumn(
                name: "BHTN",
                table: "ThongTinLuongNVs");

            migrationBuilder.DropColumn(
                name: "BHXH",
                table: "ThongTinLuongNVs");

            migrationBuilder.DropColumn(
                name: "BHYT",
                table: "ThongTinLuongNVs");

            // Step 2: Add the columns back to BangLuong
            migrationBuilder.AddColumn<decimal>(
                name: "BHTN",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BHXH",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BHYT",
                table: "BangLuong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Step 3: Restore the computed column definition for TongLuong
            migrationBuilder.AlterColumn<decimal>(
                name: "TongLuong",
                table: "BangLuong",
                type: "decimal(20,2)",
                nullable: true,
                computedColumnSql: "([LuongThem] + [LuongTangCa] + [PhuCapThem] - ([BHXH] + [BHYT] + [BHTN] + [ThueTNCN]))",
                stored: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,2)",
                oldNullable: true);
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_KD.Migrations
{
    /// <inheritdoc />
    public partial class fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'TrangThais'
                )
                BEGIN
                    CREATE TABLE TrangThais (
                        MaTrangThai NVARCHAR(450) NOT NULL PRIMARY KEY,
                        TenTrangThai NVARCHAR(MAX) NOT NULL,
                        MoTa NVARCHAR(MAX) NULL
                    )
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'TrangThais'
                )
                BEGIN
                    DROP TABLE TrangThais
                END
            ");
        }
    }
}

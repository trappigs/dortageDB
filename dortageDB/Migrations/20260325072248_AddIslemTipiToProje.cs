using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class AddIslemTipiToProje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IslemTipi",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IslemTipi",
                table: "Projeler");
        }
    }
}

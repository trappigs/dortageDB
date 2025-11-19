using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class AddProjeImageAltTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GaleriGorselleriAlt",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KapakGorseliAlt",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GaleriGorselleriAlt",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "KapakGorseliAlt",
                table: "Projeler");
        }
    }
}

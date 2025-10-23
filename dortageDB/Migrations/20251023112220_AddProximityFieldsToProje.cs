using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class AddProximityFieldsToProje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SosyalTesisler",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UlasimBilgileri",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YakinBolgeler",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YakinProjeler",
                table: "Projeler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SosyalTesisler",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "UlasimBilgileri",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "YakinBolgeler",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "YakinProjeler",
                table: "Projeler");
        }
    }
}

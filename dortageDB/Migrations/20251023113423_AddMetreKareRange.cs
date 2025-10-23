using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class AddMetreKareRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxMetreKare",
                table: "Projeler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinMetreKare",
                table: "Projeler",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxMetreKare",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "MinMetreKare",
                table: "Projeler");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSehirAndEpostaFromMusteri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop index only if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Musteriler_Eposta' AND object_id = OBJECT_ID('Musteriler'))
                BEGIN
                    DROP INDEX [IX_Musteriler_Eposta] ON [Musteriler];
                END
            ");

            // Drop columns only if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'Eposta' AND object_id = OBJECT_ID('Musteriler'))
                BEGIN
                    ALTER TABLE [Musteriler] DROP COLUMN [Eposta];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'Sehir' AND object_id = OBJECT_ID('Musteriler'))
                BEGIN
                    ALTER TABLE [Musteriler] DROP COLUMN [Sehir];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Eposta",
                table: "Musteriler",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sehir",
                table: "Musteriler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Musteriler_Eposta",
                table: "Musteriler",
                column: "Eposta");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class AddEgitimVideoEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EgitimVideolar",
                columns: table => new
                {
                    VideoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    YoutubeVideoID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Kategori = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sure = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IzlenmeSayisi = table.Column<int>(type: "int", nullable: false),
                    BegeniSayisi = table.Column<int>(type: "int", nullable: false),
                    OneEikan = table.Column<bool>(type: "bit", nullable: false),
                    Yeni = table.Column<bool>(type: "bit", nullable: false),
                    Populer = table.Column<bool>(type: "bit", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EgitimVideolar", x => x.VideoID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EgitimVideolar_Aktif_Sira",
                table: "EgitimVideolar",
                columns: new[] { "Aktif", "Sira" });

            migrationBuilder.CreateIndex(
                name: "IX_EgitimVideolar_Kategori",
                table: "EgitimVideolar",
                column: "Kategori");

            migrationBuilder.CreateIndex(
                name: "IX_EgitimVideolar_OneEikan",
                table: "EgitimVideolar",
                column: "OneEikan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EgitimVideolar");
        }
    }
}

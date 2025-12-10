using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class AddBasvuruEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Basvurular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Il = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ilce = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Meslek = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EgitimDurumu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GayrimenkulTecrubesi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NeredenDuydunuz = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KendiniziTanitin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Beklentiniz = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CvDosyaYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SosyalMedyaLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KvkkOnay = table.Column<bool>(type: "bit", nullable: false),
                    BasvuruTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Basvurular", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Basvurular");
        }
    }
}

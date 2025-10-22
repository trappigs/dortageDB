using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class AddProjeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjeID",
                table: "Satislar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjeID",
                table: "Randevular",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Projeler",
                columns: table => new
                {
                    ProjeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjeAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Konum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sehir = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Ilce = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinFiyat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxFiyat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ToplamParsel = table.Column<int>(type: "int", nullable: false),
                    SatilanParsel = table.Column<int>(type: "int", nullable: false),
                    KapakGorseli = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GaleriGorselleri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Imarlimi = table.Column<bool>(type: "bit", nullable: false),
                    MustakilTapu = table.Column<bool>(type: "bit", nullable: false),
                    TaksitImkani = table.Column<bool>(type: "bit", nullable: false),
                    TakasImkani = table.Column<bool>(type: "bit", nullable: false),
                    Altyapi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetreKare = table.Column<int>(type: "int", nullable: true),
                    KrediyeUygunluk = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OzelliklerJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    Oncelik = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeler", x => x.ProjeID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Satislar_ProjeID",
                table: "Satislar",
                column: "ProjeID");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_ProjeID",
                table: "Randevular",
                column: "ProjeID");

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_AktifMi",
                table: "Projeler",
                column: "AktifMi");

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_AktifMi_Oncelik",
                table: "Projeler",
                columns: new[] { "AktifMi", "Oncelik" });

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_Sehir",
                table: "Projeler",
                column: "Sehir");

            migrationBuilder.AddForeignKey(
                name: "FK_Randevular_Projeler_ProjeID",
                table: "Randevular",
                column: "ProjeID",
                principalTable: "Projeler",
                principalColumn: "ProjeID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Satislar_Projeler_ProjeID",
                table: "Satislar",
                column: "ProjeID",
                principalTable: "Projeler",
                principalColumn: "ProjeID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Randevular_Projeler_ProjeID",
                table: "Randevular");

            migrationBuilder.DropForeignKey(
                name: "FK_Satislar_Projeler_ProjeID",
                table: "Satislar");

            migrationBuilder.DropTable(
                name: "Projeler");

            migrationBuilder.DropIndex(
                name: "IX_Satislar_ProjeID",
                table: "Satislar");

            migrationBuilder.DropIndex(
                name: "IX_Randevular_ProjeID",
                table: "Randevular");

            migrationBuilder.DropColumn(
                name: "ProjeID",
                table: "Satislar");

            migrationBuilder.DropColumn(
                name: "ProjeID",
                table: "Randevular");
        }
    }
}

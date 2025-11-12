using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class RenameVisionerToVekarer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Musteriler_AspNetUsers_VisionerID",
                table: "Musteriler");

            migrationBuilder.DropForeignKey(
                name: "FK_Randevular_AspNetUsers_VisionerID",
                table: "Randevular");

            migrationBuilder.DropForeignKey(
                name: "FK_Satislar_AspNetUsers_VisionerID",
                table: "Satislar");

            migrationBuilder.DropTable(
                name: "VisionerProfiles");

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Satislar",
                newName: "VekarerID");

            migrationBuilder.RenameIndex(
                name: "IX_Satislar_VisionerID_SatilmaTarihi",
                table: "Satislar",
                newName: "IX_Satislar_VekarerID_SatilmaTarihi");

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Randevular",
                newName: "VekarerID");

            migrationBuilder.RenameIndex(
                name: "IX_Randevular_VisionerID_RandevuZaman",
                table: "Randevular",
                newName: "IX_Randevular_VekarerID_RandevuZaman");

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Musteriler",
                newName: "VekarerID");

            migrationBuilder.RenameIndex(
                name: "IX_Musteriler_VisionerID",
                table: "Musteriler",
                newName: "IX_Musteriler_VekarerID");

            migrationBuilder.CreateTable(
                name: "VekarerProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IBAN = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UsedReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSales = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VekarerProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_VekarerProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VekarerProfiles_ReferralCode",
                table: "VekarerProfiles",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Musteriler_AspNetUsers_VekarerID",
                table: "Musteriler",
                column: "VekarerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Randevular_AspNetUsers_VekarerID",
                table: "Randevular",
                column: "VekarerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Satislar_AspNetUsers_VekarerID",
                table: "Satislar",
                column: "VekarerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Musteriler_AspNetUsers_VekarerID",
                table: "Musteriler");

            migrationBuilder.DropForeignKey(
                name: "FK_Randevular_AspNetUsers_VekarerID",
                table: "Randevular");

            migrationBuilder.DropForeignKey(
                name: "FK_Satislar_AspNetUsers_VekarerID",
                table: "Satislar");

            migrationBuilder.DropTable(
                name: "VekarerProfiles");

            migrationBuilder.RenameColumn(
                name: "VekarerID",
                table: "Satislar",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Satislar_VekarerID_SatilmaTarihi",
                table: "Satislar",
                newName: "IX_Satislar_VisionerID_SatilmaTarihi");

            migrationBuilder.RenameColumn(
                name: "VekarerID",
                table: "Randevular",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Randevular_VekarerID_RandevuZaman",
                table: "Randevular",
                newName: "IX_Randevular_VisionerID_RandevuZaman");

            migrationBuilder.RenameColumn(
                name: "VekarerID",
                table: "Musteriler",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Musteriler_VekarerID",
                table: "Musteriler",
                newName: "IX_Musteriler_VisionerID");

            migrationBuilder.CreateTable(
                name: "VisionerProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IBAN = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSales = table.Column<int>(type: "int", nullable: false),
                    UsedReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisionerProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_VisionerProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisionerProfiles_ReferralCode",
                table: "VisionerProfiles",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Musteriler_AspNetUsers_VisionerID",
                table: "Musteriler",
                column: "VisionerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Randevular_AspNetUsers_VisionerID",
                table: "Randevular",
                column: "VisionerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Satislar_AspNetUsers_VisionerID",
                table: "Satislar",
                column: "VisionerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

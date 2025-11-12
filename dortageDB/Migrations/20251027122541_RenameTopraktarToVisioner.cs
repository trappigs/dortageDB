using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dortageDB.Migrations
{
    /// <inheritdoc />
    public partial class RenameVisionerToVisioner : Migration
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

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Satislar",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Satislar_VisionerID_SatilmaTarihi",
                table: "Satislar",
                newName: "IX_Satislar_VisionerID_SatilmaTarihi");

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Randevular",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Randevular_VisionerID_RandevuZaman",
                table: "Randevular",
                newName: "IX_Randevular_VisionerID_RandevuZaman");

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Musteriler",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Musteriler_VisionerID",
                table: "Musteriler",
                newName: "IX_Musteriler_VisionerID");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Satislar",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Satislar_VisionerID_SatilmaTarihi",
                table: "Satislar",
                newName: "IX_Satislar_VisionerID_SatilmaTarihi");

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Randevular",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Randevular_VisionerID_RandevuZaman",
                table: "Randevular",
                newName: "IX_Randevular_VisionerID_RandevuZaman");

            migrationBuilder.RenameColumn(
                name: "VisionerID",
                table: "Musteriler",
                newName: "VisionerID");

            migrationBuilder.RenameIndex(
                name: "IX_Musteriler_VisionerID",
                table: "Musteriler",
                newName: "IX_Musteriler_VisionerID");

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

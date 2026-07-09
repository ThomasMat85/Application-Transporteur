using Application_Camion_API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application_Camion_API.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260708094500_AddUniqueIndexes")]
    public partial class AddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tournees_CodeUnique",
                table: "Tournees",
                column: "CodeUnique",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Nom",
                table: "Utilisateurs",
                column: "Nom",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tournees_CodeUnique",
                table: "Tournees");

            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_Nom",
                table: "Utilisateurs");
        }
    }
}

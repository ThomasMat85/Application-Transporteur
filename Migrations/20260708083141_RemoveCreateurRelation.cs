using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application_Camion_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreateurRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournees_Utilisateurs_CreateurId",
                table: "Tournees");

            migrationBuilder.DropIndex(
                name: "IX_Tournees_CreateurId",
                table: "Tournees");

            migrationBuilder.DropColumn(
                name: "CreateurId",
                table: "Tournees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreateurId",
                table: "Tournees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tournees_CreateurId",
                table: "Tournees",
                column: "CreateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournees_Utilisateurs_CreateurId",
                table: "Tournees",
                column: "CreateurId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

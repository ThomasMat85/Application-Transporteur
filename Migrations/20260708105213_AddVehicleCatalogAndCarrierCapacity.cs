using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Application_Camion_API.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCatalogAndCarrierCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseLivraison",
                table: "Vehicules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientLivraison",
                table: "Vehicules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HauteurCm",
                table: "Vehicules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LargeurCm",
                table: "Vehicules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LongueurCm",
                table: "Vehicules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModeleVehiculeId",
                table: "Vehicules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoidsKg",
                table: "Vehicules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CamionPorteurId",
                table: "Tournees",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CamionsPorteurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    LongueurUtileCm = table.Column<int>(type: "integer", nullable: false),
                    LargeurUtileCm = table.Column<int>(type: "integer", nullable: false),
                    HauteurMaxCm = table.Column<int>(type: "integer", nullable: false),
                    ChargeUtileKg = table.Column<int>(type: "integer", nullable: false),
                    NombreNiveaux = table.Column<int>(type: "integer", nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CamionsPorteurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelesVehicules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Marque = table.Column<string>(type: "text", nullable: false),
                    Modele = table.Column<string>(type: "text", nullable: false),
                    LongueurCm = table.Column<int>(type: "integer", nullable: false),
                    LargeurCm = table.Column<int>(type: "integer", nullable: false),
                    HauteurCm = table.Column<int>(type: "integer", nullable: false),
                    PoidsKg = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelesVehicules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_ModeleVehiculeId",
                table: "Vehicules",
                column: "ModeleVehiculeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournees_CamionPorteurId",
                table: "Tournees",
                column: "CamionPorteurId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelesVehicules_Marque_Modele",
                table: "ModelesVehicules",
                columns: new[] { "Marque", "Modele" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tournees_CamionsPorteurs_CamionPorteurId",
                table: "Tournees",
                column: "CamionPorteurId",
                principalTable: "CamionsPorteurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicules_ModelesVehicules_ModeleVehiculeId",
                table: "Vehicules",
                column: "ModeleVehiculeId",
                principalTable: "ModelesVehicules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournees_CamionsPorteurs_CamionPorteurId",
                table: "Tournees");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicules_ModelesVehicules_ModeleVehiculeId",
                table: "Vehicules");

            migrationBuilder.DropTable(
                name: "CamionsPorteurs");

            migrationBuilder.DropTable(
                name: "ModelesVehicules");

            migrationBuilder.DropIndex(
                name: "IX_Vehicules_ModeleVehiculeId",
                table: "Vehicules");

            migrationBuilder.DropIndex(
                name: "IX_Tournees_CamionPorteurId",
                table: "Tournees");

            migrationBuilder.DropColumn(
                name: "AdresseLivraison",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "ClientLivraison",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "HauteurCm",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "LargeurCm",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "LongueurCm",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "ModeleVehiculeId",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "PoidsKg",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "CamionPorteurId",
                table: "Tournees");
        }
    }
}

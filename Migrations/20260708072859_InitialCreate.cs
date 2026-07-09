using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Application_Camion_API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    MotDePasse = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeUnique = table.Column<string>(type: "text", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Terminee = table.Column<bool>(type: "boolean", nullable: false),
                    CreateurId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tournees_Utilisateurs_CreateurId",
                        column: x => x.CreateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Etapes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    Garage = table.Column<string>(type: "text", nullable: false),
                    Adresse = table.Column<string>(type: "text", nullable: false),
                    TourneeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etapes_Tournees_TourneeId",
                        column: x => x.TourneeId,
                        principalTable: "Tournees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vehicules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Marque = table.Column<string>(type: "text", nullable: false),
                    Modele = table.Column<string>(type: "text", nullable: false),
                    Immatriculation = table.Column<string>(type: "text", nullable: false),
                    Recupere = table.Column<bool>(type: "boolean", nullable: false),
                    EtapeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicules_Etapes_EtapeId",
                        column: x => x.EtapeId,
                        principalTable: "Etapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Etapes_TourneeId",
                table: "Etapes",
                column: "TourneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournees_CreateurId",
                table: "Tournees",
                column: "CreateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_EtapeId",
                table: "Vehicules",
                column: "EtapeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vehicules");

            migrationBuilder.DropTable(
                name: "Etapes");

            migrationBuilder.DropTable(
                name: "Tournees");

            migrationBuilder.DropTable(
                name: "Utilisateurs");
        }
    }
}

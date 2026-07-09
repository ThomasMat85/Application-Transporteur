using Application_Camion_API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application_Camion_API.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260708133000_AddCarrierHeightZones")]
    public partial class AddCarrierHeightZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HauteurMaxAvantCm",
                table: "CamionsPorteurs",
                type: "integer",
                nullable: false,
                defaultValue: 190);

            migrationBuilder.AddColumn<int>(
                name: "HauteurMaxArriereCm",
                table: "CamionsPorteurs",
                type: "integer",
                nullable: false,
                defaultValue: 280);

            migrationBuilder.AddColumn<int>(
                name: "LongueurZoneArriereCm",
                table: "CamionsPorteurs",
                type: "integer",
                nullable: false,
                defaultValue: 900);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HauteurMaxAvantCm",
                table: "CamionsPorteurs");

            migrationBuilder.DropColumn(
                name: "HauteurMaxArriereCm",
                table: "CamionsPorteurs");

            migrationBuilder.DropColumn(
                name: "LongueurZoneArriereCm",
                table: "CamionsPorteurs");
        }
    }
}

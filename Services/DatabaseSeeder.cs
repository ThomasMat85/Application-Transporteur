using Application_Camion_API.Data;
using Application_Camion_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Application_Camion_API.Services;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        await EnsureUserAsync(dbContext, "admin", "1234", "admin");
        await EnsureUserAsync(dbContext, "chauffeur", "1234", "chauffeur");
        await EnsureUserAsync(dbContext, "TheoB", "1234", "chauffeur");

        await EnsureModeleVehiculeAsync(dbContext, "Renault", "Clio", 405, 180, 144, 1200);
        await EnsureModeleVehiculeAsync(dbContext, "Peugeot", "208", 406, 175, 143, 1180);
        await EnsureModeleVehiculeAsync(dbContext, "Citroen", "C3", 400, 175, 147, 1160);
        await EnsureModeleVehiculeAsync(dbContext, "Dacia", "Sandero", 409, 185, 150, 1120);
        await EnsureModeleVehiculeAsync(dbContext, "Renault", "Captur", 423, 180, 158, 1320);
        await EnsureModeleVehiculeAsync(dbContext, "Utilitaire", "9m3", 555, 207, 250, 2200);

        await EnsureCamionPorteurAsync(
            dbContext,
            "Scania P460 + LOHR CGKMHTTBOB",
            2035,
            250,
            280,
            190,
            280,
            900,
            9500,
            2,
            "Scania P460 porte-voitures",
            "Porteur 29 m utiles",
            "Porteur 7 places");

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        ApplicationDbContext dbContext,
        string nom,
        string motDePasse,
        string role)
    {
        if (await dbContext.Utilisateurs.AnyAsync(u => u.Nom == nom))
            return;

        dbContext.Utilisateurs.Add(new Utilisateur
        {
            Nom = nom,
            MotDePasse = motDePasse,
            Role = role
        });
    }

    private static async Task EnsureModeleVehiculeAsync(
        ApplicationDbContext dbContext,
        string marque,
        string modele,
        int longueurCm,
        int largeurCm,
        int hauteurCm,
        int poidsKg)
    {
        if (await dbContext.ModelesVehicules.AnyAsync(m => m.Marque == marque && m.Modele == modele))
            return;

        dbContext.ModelesVehicules.Add(new ModeleVehicule
        {
            Marque = marque,
            Modele = modele,
            LongueurCm = longueurCm,
            LargeurCm = largeurCm,
            HauteurCm = hauteurCm,
            PoidsKg = poidsKg
        });
    }

    private static async Task EnsureCamionPorteurAsync(
        ApplicationDbContext dbContext,
        string nom,
        int longueurUtileCm,
        int largeurUtileCm,
        int hauteurMaxCm,
        int hauteurMaxAvantCm,
        int hauteurMaxArriereCm,
        int longueurZoneArriereCm,
        int chargeUtileKg,
        int nombreNiveaux,
        params string[] anciensNoms)
    {
        var camion = await dbContext.CamionsPorteurs
            .FirstOrDefaultAsync(c => c.Nom == nom || anciensNoms.Contains(c.Nom));

        if (camion != null)
        {
            camion.Nom = nom;
            camion.LongueurUtileCm = longueurUtileCm;
            camion.LargeurUtileCm = largeurUtileCm;
            camion.HauteurMaxCm = hauteurMaxCm;
            camion.HauteurMaxAvantCm = hauteurMaxAvantCm;
            camion.HauteurMaxArriereCm = hauteurMaxArriereCm;
            camion.LongueurZoneArriereCm = longueurZoneArriereCm;
            camion.ChargeUtileKg = chargeUtileKg;
            camion.NombreNiveaux = nombreNiveaux;
            camion.Actif = true;
            return;
        }

        dbContext.CamionsPorteurs.Add(new CamionPorteur
        {
            Nom = nom,
            LongueurUtileCm = longueurUtileCm,
            LargeurUtileCm = largeurUtileCm,
            HauteurMaxCm = hauteurMaxCm,
            HauteurMaxAvantCm = hauteurMaxAvantCm,
            HauteurMaxArriereCm = hauteurMaxArriereCm,
            LongueurZoneArriereCm = longueurZoneArriereCm,
            ChargeUtileKg = chargeUtileKg,
            NombreNiveaux = nombreNiveaux,
            Actif = true
        });
    }
}

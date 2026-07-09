using Application_Camion_API.DTOs;
using Application_Camion_API.Models;

namespace Application_Camion_API.Services;

public static class MappingExtensions
{
    public static TourneeDto ToDto(this Tournee tournee)
    {
        return new TourneeDto
        {
            Id = tournee.Id,
            CodeUnique = tournee.CodeUnique,
            DateCreation = tournee.DateCreation,
            Terminee = tournee.Terminee,
            PlanOptimise = tournee.PlanOptimise,
            CamionPorteurId = tournee.CamionPorteurId,
            CamionPorteur = tournee.CamionPorteur?.ToDto(),
            Etapes = tournee.Etapes
                .OrderBy(e => e.Ordre)
                .Select(e => e.ToDto())
                .ToList()
        };
    }

    public static EtapeDto ToDto(this Etape etape)
    {
        return new EtapeDto
        {
            Id = etape.Id,
            Ordre = etape.Ordre,
            Garage = etape.Garage,
            Adresse = etape.Adresse,
            Vehicules = etape.Vehicules
                .OrderBy(v => v.Id)
                .Select(v => v.ToDto())
                .ToList()
        };
    }

    public static VehiculeDto ToDto(this Vehicule vehicule)
    {
        return new VehiculeDto
        {
            Id = vehicule.Id,
            Marque = vehicule.Marque,
            Modele = vehicule.Modele,
            Immatriculation = vehicule.Immatriculation,
            ModeleVehiculeId = vehicule.ModeleVehiculeId,
            LongueurCm = vehicule.LongueurCm,
            LargeurCm = vehicule.LargeurCm,
            HauteurCm = vehicule.HauteurCm,
            PoidsKg = vehicule.PoidsKg,
            AdresseLivraison = vehicule.AdresseLivraison,
            ClientLivraison = vehicule.ClientLivraison,
            Recupere = vehicule.Recupere
        };
    }

    public static ModeleVehiculeDto ToDto(this ModeleVehicule modele)
    {
        return new ModeleVehiculeDto
        {
            Id = modele.Id,
            Marque = modele.Marque,
            Modele = modele.Modele,
            LongueurCm = modele.LongueurCm,
            LargeurCm = modele.LargeurCm,
            HauteurCm = modele.HauteurCm,
            PoidsKg = modele.PoidsKg
        };
    }

    public static CamionPorteurDto ToDto(this CamionPorteur camion)
    {
        return new CamionPorteurDto
        {
            Id = camion.Id,
            Nom = camion.Nom,
            LongueurUtileCm = camion.LongueurUtileCm,
            LargeurUtileCm = camion.LargeurUtileCm,
            HauteurMaxCm = camion.HauteurMaxCm,
            HauteurMaxAvantCm = camion.HauteurMaxAvantCm,
            HauteurMaxArriereCm = camion.HauteurMaxArriereCm,
            LongueurZoneArriereCm = camion.LongueurZoneArriereCm,
            ChargeUtileKg = camion.ChargeUtileKg,
            NombreNiveaux = camion.NombreNiveaux,
            Actif = camion.Actif
        };
    }

    public static UtilisateurDto ToDto(this Utilisateur utilisateur)
    {
        return new UtilisateurDto
        {
            Id = utilisateur.Id,
            Nom = utilisateur.Nom,
            Role = utilisateur.Role
        };
    }
}

namespace Application_Camion_API.DTOs;

public class OptimisationTourneeDto
{
    public int TourneeId { get; set; }

    public string CodeUnique { get; set; } = "";

    public string AdresseDepartRetour { get; set; } = "";

    public double DistanceApproxKm { get; set; }

    public List<OptimisationArretDto> Arrets { get; set; } = new();

    public List<OptimisationChargementDto> Chargement { get; set; } = new();

    public List<string> Alertes { get; set; } = new();
}

public class OptimisationArretDto
{
    public int? EtapeId { get; set; }

    public int Ordre { get; set; }

    public string Type { get; set; } = "";

    public string Nom { get; set; } = "";

    public string Adresse { get; set; } = "";

    public double DistanceDepuisPrecedentKm { get; set; }

    public List<string> Vehicules { get; set; } = new();
}

public class OptimisationChargementDto
{
    public string Vehicule { get; set; } = "";

    public string Immatriculation { get; set; } = "";

    public string PositionConseillee { get; set; } = "";

    public string Raison { get; set; } = "";
}

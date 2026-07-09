namespace Application_Camion_API.Models;

public class Vehicule
{
    public int Id { get; set; }

    public string Marque { get; set; } = "";

    public string Modele { get; set; } = "";

    public string Immatriculation { get; set; } = "";

    public int? ModeleVehiculeId { get; set; }

    public ModeleVehicule? ModeleVehicule { get; set; }

    public int LongueurCm { get; set; }

    public int LargeurCm { get; set; }

    public int HauteurCm { get; set; }

    public int PoidsKg { get; set; }

    public string AdresseLivraison { get; set; } = "";

    public string ClientLivraison { get; set; } = "";

    public bool Recupere { get; set; }

    public int EtapeId { get; set; }

    public Etape Etape { get; set; } = null!;
}

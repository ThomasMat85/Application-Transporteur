namespace Application_Camion_API.Models;

public class ModeleVehicule
{
    public int Id { get; set; }

    public string Marque { get; set; } = "";

    public string Modele { get; set; } = "";

    public int LongueurCm { get; set; }

    public int LargeurCm { get; set; }

    public int HauteurCm { get; set; }

    public int PoidsKg { get; set; }

    public List<Vehicule> Vehicules { get; set; } = new();
}

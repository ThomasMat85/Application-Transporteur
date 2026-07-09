namespace Application_Camion_API.DTOs;

public class ModeleVehiculeDto
{
    public int Id { get; set; }

    public string Marque { get; set; } = "";

    public string Modele { get; set; } = "";

    public int LongueurCm { get; set; }

    public int LargeurCm { get; set; }

    public int HauteurCm { get; set; }

    public int PoidsKg { get; set; }
}

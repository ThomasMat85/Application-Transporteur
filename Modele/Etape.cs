namespace Application_Camion_API.Models;

public class Etape
{
    public int Id { get; set; }

    public int Ordre { get; set; }

    public string Garage { get; set; } = "";

    public string Adresse { get; set; } = "";

    public int TourneeId { get; set; }

    public Tournee Tournee { get; set; } = null!;

    public List<Vehicule> Vehicules { get; set; } = new();
}
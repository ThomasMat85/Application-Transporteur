namespace Application_Camion_API.DTOs;

public class EtapeDto
{
    public int Id { get; set; }

    public int Ordre { get; set; }

    public string Garage { get; set; } = "";

    public string Adresse { get; set; } = "";

    public List<VehiculeDto> Vehicules { get; set; } = new();
}

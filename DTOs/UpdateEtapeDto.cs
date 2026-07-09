namespace Application_Camion_API.DTOs;

public class UpdateEtapeDto
{
    public int Ordre { get; set; }

    public string Garage { get; set; } = "";

    public string Adresse { get; set; } = "";
}

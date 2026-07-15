namespace Application_Camion_API.DTOs;

public class CreateTourneeDto
{
    public string? CodeUnique { get; set; }

    public bool Terminee { get; set; }

    public string AdresseDepartRetour { get; set; } = "";

    public int? CamionPorteurId { get; set; }

    public List<CreateEtapeDto> Etapes { get; set; } = new();
}

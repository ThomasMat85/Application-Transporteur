namespace Application_Camion_API.DTOs;

public class UpdateTourneeDetailsDto
{
    public bool Terminee { get; set; }

    public List<CreateEtapeDto> Etapes { get; set; } = new();
}

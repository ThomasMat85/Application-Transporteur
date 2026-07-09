namespace Application_Camion_API.DTOs;

public class TourneeDto
{
    public int Id { get; set; }

    public string CodeUnique { get; set; } = "";

    public DateTime DateCreation { get; set; }

    public bool Terminee { get; set; }

    public string PlanOptimise { get; set; } = "";

    public int? CamionPorteurId { get; set; }

    public CamionPorteurDto? CamionPorteur { get; set; }

    public List<EtapeDto> Etapes { get; set; } = new();
}

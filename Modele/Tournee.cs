namespace Application_Camion_API.Models
{
    public class Tournee
    {
        public int Id { get; set; }

        public string CodeUnique { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }

        public bool Terminee { get; set; }

        public string PlanOptimise { get; set; } = string.Empty;

        public string AdresseDepartRetour { get; set; } = string.Empty;

        public int? CamionPorteurId { get; set; }

        public CamionPorteur? CamionPorteur { get; set; }

        public List<Etape> Etapes { get; set; } = new();
    }
}

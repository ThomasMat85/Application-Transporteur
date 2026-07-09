namespace Application_Camion_API.Models;

public class CamionPorteur
{
    public int Id { get; set; }

    public string Nom { get; set; } = "";

    public int LongueurUtileCm { get; set; }

    public int LargeurUtileCm { get; set; }

    public int HauteurMaxCm { get; set; }

    public int HauteurMaxAvantCm { get; set; }

    public int HauteurMaxArriereCm { get; set; }

    public int LongueurZoneArriereCm { get; set; }

    public int ChargeUtileKg { get; set; }

    public int NombreNiveaux { get; set; } = 1;

    public bool Actif { get; set; } = true;

    public List<Tournee> Tournees { get; set; } = new();
}

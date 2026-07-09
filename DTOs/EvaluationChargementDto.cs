namespace Application_Camion_API.DTOs;

public class EvaluationChargementDto
{
    public int TourneeId { get; set; }

    public string CodeUnique { get; set; } = "";

    public string Camion { get; set; } = "";

    public int VehiculesTotal { get; set; }

    public int VehiculesQuiRentrent { get; set; }

    public int VehiculesQuiNeRentrentPas { get; set; }

    public int LongueurDisponibleCm { get; set; }

    public int LongueurUtiliseeCm { get; set; }

    public int LongueurZoneArriereCm { get; set; }

    public int LongueurZoneArriereUtiliseeCm { get; set; }

    public int HauteurMaxAvantCm { get; set; }

    public int HauteurMaxArriereCm { get; set; }

    public int ChargeDisponibleKg { get; set; }

    public int ChargeUtiliseeKg { get; set; }

    public bool ToutRentre { get; set; }

    public List<string> Raisons { get; set; } = new();
}

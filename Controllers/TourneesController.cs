using Application_Camion_API.Data;
using Application_Camion_API.DTOs;
using Application_Camion_API.Models;
using Application_Camion_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Application_Camion_API.Controllers;

[Route("api/tournees")]
[ApiController]
public class TourneesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public TourneesController(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    // GET : api/tournees
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TourneeDto>>> GetTournees()
    {
        var tournees = await _context.Tournees
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .Include(t => t.CamionPorteur)
            .OrderByDescending(t => t.DateCreation)
            .ToListAsync();

        return tournees.Select(t => t.ToDto()).ToList();
    }

    // GET : api/tournees/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TourneeDto>> GetTournee(int id)
    {
        var tournee = await _context.Tournees
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .Include(t => t.CamionPorteur)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournee == null)
            return NotFound();

        await SAssurerPlanOptimiseAJourAsync(tournee);

        return tournee.ToDto();
    }

    // GET : api/tournees/code/A82K91
    [HttpGet("code/{code}")]
    public async Task<ActionResult<TourneeDto>> GetTourneeByCode(string code)
    {
        var normalizedCode = (code ?? "").Trim().ToUpperInvariant();

        var tournee = await _context.Tournees
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .Include(t => t.CamionPorteur)
            .FirstOrDefaultAsync(t => t.CodeUnique == normalizedCode);

        if (tournee == null)
            return NotFound();

        await SAssurerPlanOptimiseAJourAsync(tournee);

        return tournee.ToDto();
    }

    // POST : api/tournees
    [HttpPost]
    public async Task<ActionResult<TourneeDto>> CreateTournee(CreateTourneeDto request)
    {
        var requestedEtapes = request.Etapes ?? new List<CreateEtapeDto>();

        if (requestedEtapes.Count == 0)
            return BadRequest("Une tournee doit contenir au moins une etape.");

        if (requestedEtapes.Any(e => e.Ordre <= 0))
            return BadRequest("Chaque etape doit avoir un ordre superieur a 0.");

        var code = string.IsNullOrWhiteSpace(request.CodeUnique)
            ? await GenerateUniqueCode()
            : request.CodeUnique.Trim().ToUpperInvariant();

        if (await _context.Tournees.AnyAsync(t => t.CodeUnique == code))
            return Conflict($"Une tournee avec le code {code} existe deja.");

        var modelesVehicules = await ChargerModelesVehiculesAsync(requestedEtapes);

        var tournee = new Tournee
        {
            CodeUnique = code,
            DateCreation = DateTime.UtcNow,
            Terminee = request.Terminee,
            CamionPorteurId = request.CamionPorteurId,
            Etapes = requestedEtapes
                .OrderBy(e => e.Ordre)
                .Select(e => new Etape
                {
                    Ordre = e.Ordre,
                    Garage = (e.Garage ?? "").Trim(),
                    Adresse = (e.Adresse ?? "").Trim(),
                    Vehicules = (e.Vehicules ?? new List<CreateVehiculeDto>())
                        .Select(v => CreerVehicule(v, modelesVehicules))
                        .ToList()
                }).ToList()
        };

        _context.Tournees.Add(tournee);

        await _context.SaveChangesAsync();

        var createdTournee = await _context.Tournees
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .Include(t => t.CamionPorteur)
            .FirstAsync(t => t.Id == tournee.Id);

        await AppliquerOptimisationAsync(createdTournee);

        return CreatedAtAction(
            nameof(GetTournee),
            new { id = tournee.Id },
            createdTournee.ToDto());
    }

    // PUT : api/tournees/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTournee(int id, UpdateTourneeDto request)
    {
        var tournee = await _context.Tournees.FindAsync(id);

        if (tournee == null)
            return NotFound();

        tournee.Terminee = request.Terminee;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT : api/tournees/5/details
    [HttpPut("{id}/details")]
    public async Task<ActionResult<TourneeDto>> UpdateTourneeDetails(int id, UpdateTourneeDetailsDto request)
    {
        var requestedEtapes = request.Etapes ?? new List<CreateEtapeDto>();

        if (requestedEtapes.Count == 0)
            return BadRequest("Une tournee doit contenir au moins une etape.");

        if (requestedEtapes.Any(e => e.Ordre <= 0))
            return BadRequest("Chaque etape doit avoir un ordre superieur a 0.");

        var tournee = await _context.Tournees
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .Include(t => t.CamionPorteur)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournee == null)
            return NotFound();

        var modelesVehicules = await ChargerModelesVehiculesAsync(requestedEtapes);

        foreach (var etape in tournee.Etapes)
        {
            _context.Vehicules.RemoveRange(etape.Vehicules);
        }

        _context.Etapes.RemoveRange(tournee.Etapes);

        tournee.Terminee = request.Terminee;
        tournee.Etapes = requestedEtapes
            .OrderBy(e => e.Ordre)
            .Select(e => new Etape
            {
                Ordre = e.Ordre,
                Garage = (e.Garage ?? "").Trim(),
                Adresse = (e.Adresse ?? "").Trim(),
                Vehicules = (e.Vehicules ?? new List<CreateVehiculeDto>())
                    .Select(v => CreerVehicule(v, modelesVehicules))
                    .ToList()
            })
            .ToList();

        await _context.SaveChangesAsync();

        var updatedTournee = await _context.Tournees
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .Include(t => t.CamionPorteur)
            .FirstAsync(t => t.Id == id);

        await AppliquerOptimisationAsync(updatedTournee);

        return updatedTournee.ToDto();
    }

    // GET : api/tournees/5/optimisation
    [HttpGet("{id}/optimisation")]
    public async Task<ActionResult<OptimisationTourneeDto>> OptimiserTournee(int id)
    {
        var tournee = await _context.Tournees
            .Include(t => t.CamionPorteur)
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournee == null)
            return NotFound();

        return await ConstruireOptimisationAsync(tournee);
    }

    private async Task<OptimisationTourneeDto> ConstruireOptimisationAsync(Tournee tournee)
    {
        var resultat = new OptimisationTourneeDto
        {
            TourneeId = tournee.Id,
            CodeUnique = tournee.CodeUnique
        };

        resultat.Alertes.Add(
            "Optimisation indicative : elle aide a preparer l'ordre, mais le chauffeur garde la decision finale sur le terrain.");

        resultat.Alertes.Add(
            "Les livraisons deja chargees sont priorisees quand elles restent dans une distance raisonnable.");

        var etapesRestantes = tournee.Etapes
            .OrderBy(e => e.Ordre)
            .ToList();

        var vehiculesCharges = new List<Vehicule>();
        var coordonneesCache = new Dictionary<string, Coordonnees?>();
        Coordonnees? positionActuelle = null;
        int ordre = 1;
        double distanceTotale = 0;

        while (etapesRestantes.Count > 0 || vehiculesCharges.Count > 0)
        {
            var candidats = new List<CandidatOptimisation>();

            foreach (var etape in etapesRestantes)
            {
                candidats.Add(new CandidatOptimisation
                {
                    Type = "Chargement",
                    Nom = string.IsNullOrWhiteSpace(etape.Garage) ? $"Etape {etape.Ordre}" : etape.Garage,
                    Adresse = etape.Adresse,
                    Vehicules = etape.Vehicules.ToList(),
                    Etape = etape
                });
            }

            foreach (var livraison in vehiculesCharges
                .Where(v => !string.IsNullOrWhiteSpace(v.AdresseLivraison))
                .GroupBy(v => v.AdresseLivraison.Trim()))
            {
                candidats.Add(new CandidatOptimisation
                {
                    Type = "Livraison",
                    Nom = string.Join(", ", livraison
                        .Select(v => v.ClientLivraison)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct()),
                    Adresse = livraison.Key,
                    Vehicules = livraison.ToList()
                });
            }

            if (candidats.Count == 0)
            {
                resultat.Alertes.Add(
                    "Certains vehicules n'ont pas d'adresse de livraison, impossible de les optimiser automatiquement.");
                break;
            }

            foreach (var candidat in candidats)
            {
                candidat.Coordonnees =
                    await GeocoderAsync(candidat.Adresse, coordonneesCache);

                candidat.DistanceDepuisPrecedentKm =
                    positionActuelle == null || candidat.Coordonnees == null
                    ? 0
                    : CalculerDistanceKm(positionActuelle, candidat.Coordonnees);
            }

            var choisi = ChoisirProchainArret(candidats, positionActuelle);

            resultat.Arrets.Add(new OptimisationArretDto
            {
                EtapeId = choisi.Etape?.Id,
                Ordre = ordre++,
                Type = choisi.Type,
                Nom = choisi.Nom,
                Adresse = choisi.Adresse,
                DistanceDepuisPrecedentKm = Math.Round(choisi.DistanceDepuisPrecedentKm, 1),
                Vehicules = choisi.Vehicules
                    .Select(ConstruireNomVehicule)
                    .ToList()
            });

            distanceTotale += choisi.DistanceDepuisPrecedentKm;

            if (choisi.Coordonnees != null)
                positionActuelle = choisi.Coordonnees;

            if (choisi.Type == "Chargement" && choisi.Etape != null)
            {
                etapesRestantes.Remove(choisi.Etape);
                vehiculesCharges.AddRange(choisi.Etape.Vehicules);
            }
            else
            {
                foreach (var vehicule in choisi.Vehicules)
                {
                    vehiculesCharges.Remove(vehicule);
                }
            }
        }

        resultat.DistanceApproxKm = Math.Round(distanceTotale, 1);
        resultat.Chargement = CreerPlanChargement(tournee);

        return resultat;
    }

    private static CandidatOptimisation ChoisirProchainArret(
        List<CandidatOptimisation> candidats,
        Coordonnees? positionActuelle)
    {
        if (positionActuelle != null)
        {
            var livraisons = candidats
                .Where(c => c.Type == "Livraison")
                .ToList();

            if (livraisons.Count > 0)
            {
                var meilleureLivraison =
                    TrierCandidats(livraisons).First();

                var chargements = candidats
                    .Where(c => c.Type == "Chargement")
                    .ToList();

                if (chargements.Count == 0)
                    return meilleureLivraison;

                var meilleurChargement =
                    TrierCandidats(chargements).First();

                if (DoitPrioriserLivraison(
                    meilleureLivraison,
                    meilleurChargement))
                {
                    return meilleureLivraison;
                }
            }
        }

        return TrierCandidats(candidats).First();
    }

    private static IOrderedEnumerable<CandidatOptimisation> TrierCandidats(
        IEnumerable<CandidatOptimisation> candidats)
    {
        return candidats
            .OrderBy(c => c.Coordonnees == null ? 1 : 0)
            .ThenBy(c => c.DistanceDepuisPrecedentKm)
            .ThenBy(c => c.Type == "Livraison" ? 0 : 1)
            .ThenBy(c => c.Nom);
    }

    private static bool DoitPrioriserLivraison(
        CandidatOptimisation livraison,
        CandidatOptimisation chargement)
    {
        if (livraison.Coordonnees != null && chargement.Coordonnees == null)
            return true;

        if (livraison.Coordonnees == null && chargement.Coordonnees != null)
            return false;

        if (livraison.Coordonnees == null && chargement.Coordonnees == null)
            return true;

        const double ratioDetourAcceptable = 1.35;

        if (chargement.DistanceDepuisPrecedentKm <= 0.1)
            return livraison.DistanceDepuisPrecedentKm <= 2;

        return livraison.DistanceDepuisPrecedentKm <=
            chargement.DistanceDepuisPrecedentKm * ratioDetourAcceptable;
    }

    private async Task AppliquerOptimisationAsync(Tournee tournee)
    {
        try
        {
            OptimisationTourneeDto optimisation =
                await ConstruireOptimisationAsync(tournee);

            tournee.PlanOptimise =
                ConstruirePlanOptimiseTexte(optimisation);

            var etapesOrdonnees = optimisation.Arrets
                .Where(a => a.Type == "Chargement" && a.EtapeId.HasValue)
                .Select(a => a.EtapeId!.Value)
                .ToList();

            int ordre = 1;

            foreach (int etapeId in etapesOrdonnees)
            {
                var etape = tournee.Etapes.FirstOrDefault(e => e.Id == etapeId);

                if (etape != null)
                    etape.Ordre = ordre++;
            }

            foreach (var etape in tournee.Etapes
                .Where(e => !etapesOrdonnees.Contains(e.Id))
                .OrderBy(e => e.Ordre))
            {
                etape.Ordre = ordre++;
            }

            await _context.SaveChangesAsync();
        }
        catch
        {
            tournee.PlanOptimise =
                "Optimisation impossible pour cette tournee.";

            await _context.SaveChangesAsync();
        }
    }

    private async Task SAssurerPlanOptimiseAJourAsync(Tournee tournee)
    {
        if (string.IsNullOrWhiteSpace(tournee.PlanOptimise) ||
            !tournee.PlanOptimise.Contains("Ordre de chargement camion") ||
            !tournee.PlanOptimise.Contains("premiere livraison sur etage 1") ||
            !tournee.PlanOptimise.Contains("livraisons deja chargees"))
        {
            await AppliquerOptimisationAsync(tournee);
        }
    }

    // GET : api/tournees/5/chargement
    [HttpGet("{id}/chargement")]
    public async Task<ActionResult<EvaluationChargementDto>> EvaluerChargement(int id)
    {
        var tournee = await _context.Tournees
            .Include(t => t.CamionPorteur)
            .Include(t => t.Etapes)
                .ThenInclude(e => e.Vehicules)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournee == null)
            return NotFound();

        if (tournee.CamionPorteur == null)
            return BadRequest("Aucun camion porteur n'est associe a cette tournee.");

        var camion = tournee.CamionPorteur;
        var vehicules = tournee.Etapes
            .OrderBy(e => e.Ordre)
            .SelectMany(e => e.Vehicules)
            .ToList();

        int nombreNiveaux = Math.Max(1, camion.NombreNiveaux);
        int longueurDisponible = camion.LongueurUtileCm * nombreNiveaux;
        int[] longueurUtiliseeParNiveau = new int[nombreNiveaux];
        int[] longueurHauteUtiliseeParNiveau = new int[nombreNiveaux];
        int chargeUtilisee = 0;
        int vehiculesQuiRentrent = 0;
        var raisons = new List<string>();

        int hauteurAvant =
            camion.HauteurMaxAvantCm > 0
            ? camion.HauteurMaxAvantCm
            : camion.HauteurMaxCm;

        int hauteurArriere =
            camion.HauteurMaxArriereCm > 0
            ? camion.HauteurMaxArriereCm
            : camion.HauteurMaxCm;

        int longueurZoneArriere =
            camion.LongueurZoneArriereCm > 0
            ? Math.Min(camion.LongueurZoneArriereCm, camion.LongueurUtileCm)
            : camion.LongueurUtileCm;

        foreach (var vehicule in vehicules
            .OrderByDescending(v => v.HauteurCm > hauteurAvant)
            .ThenByDescending(v => v.HauteurCm)
            .ThenByDescending(v => v.LongueurCm))
        {
            var nomVehicule = $"{vehicule.Marque} {vehicule.Modele}".Trim();

            if (vehicule.LongueurCm <= 0 || vehicule.PoidsKg <= 0)
            {
                raisons.Add($"{nomVehicule} : dimensions incompletes.");
                continue;
            }

            if (vehicule.LongueurCm > camion.LongueurUtileCm)
            {
                raisons.Add($"{nomVehicule} : trop long pour un niveau du camion.");
                continue;
            }

            if (vehicule.LargeurCm > camion.LargeurUtileCm)
            {
                raisons.Add($"{nomVehicule} : trop large pour le camion.");
                continue;
            }

            if (vehicule.HauteurCm > hauteurArriere)
            {
                raisons.Add($"{nomVehicule} : trop haut pour le camion.");
                continue;
            }

            bool doitAllerArriere =
                vehicule.HauteurCm > hauteurAvant;

            int niveauDisponible = -1;

            for (int i = longueurUtiliseeParNiveau.Length - 1; i >= 0; i--)
            {
                bool longueurOk =
                    longueurUtiliseeParNiveau[i] + vehicule.LongueurCm <= camion.LongueurUtileCm;

                bool hauteurOk =
                    !doitAllerArriere ||
                    longueurHauteUtiliseeParNiveau[i] + vehicule.LongueurCm <= longueurZoneArriere;

                if (longueurOk && hauteurOk)
                {
                    niveauDisponible = i;
                    break;
                }
            }

            if (niveauDisponible < 0)
            {
                raisons.Add(
                    doitAllerArriere
                    ? $"{nomVehicule} : plus assez de place dans la zone arriere haute."
                    : $"{nomVehicule} : plus assez de longueur utile dans le camion.");
                continue;
            }

            if (chargeUtilisee + vehicule.PoidsKg > camion.ChargeUtileKg)
            {
                raisons.Add($"{nomVehicule} : charge utile du camion depassee.");
                continue;
            }

            longueurUtiliseeParNiveau[niveauDisponible] += vehicule.LongueurCm;

            if (doitAllerArriere)
                longueurHauteUtiliseeParNiveau[niveauDisponible] += vehicule.LongueurCm;

            chargeUtilisee += vehicule.PoidsKg;
            vehiculesQuiRentrent++;
        }

        int longueurUtilisee = longueurUtiliseeParNiveau.Sum();
        int longueurZoneArriereUtilisee = longueurHauteUtiliseeParNiveau.Sum();

        return new EvaluationChargementDto
        {
            TourneeId = tournee.Id,
            CodeUnique = tournee.CodeUnique,
            Camion = camion.Nom,
            VehiculesTotal = vehicules.Count,
            VehiculesQuiRentrent = vehiculesQuiRentrent,
            VehiculesQuiNeRentrentPas = vehicules.Count - vehiculesQuiRentrent,
            LongueurDisponibleCm = longueurDisponible,
            LongueurUtiliseeCm = longueurUtilisee,
            LongueurZoneArriereCm = longueurZoneArriere * nombreNiveaux,
            LongueurZoneArriereUtiliseeCm = longueurZoneArriereUtilisee,
            HauteurMaxAvantCm = hauteurAvant,
            HauteurMaxArriereCm = hauteurArriere,
            ChargeDisponibleKg = camion.ChargeUtileKg,
            ChargeUtiliseeKg = chargeUtilisee,
            ToutRentre = vehiculesQuiRentrent == vehicules.Count,
            Raisons = raisons
        };
    }

    // DELETE : api/tournees/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTournee(int id)
    {
        var tournee = await _context.Tournees.FindAsync(id);

        if (tournee == null)
            return NotFound();

        _context.Tournees.Remove(tournee);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string> GenerateUniqueCode()
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string code;

        do
        {
            code = new string(
                Enumerable.Range(0, 6)
                    .Select(_ => characters[Random.Shared.Next(characters.Length)])
                    .ToArray());
        }
        while (await _context.Tournees.AnyAsync(t => t.CodeUnique == code));

        return code;
    }

    private async Task<Coordonnees?> GeocoderAsync(
        string adresse,
        Dictionary<string, Coordonnees?> cache)
    {
        adresse = (adresse ?? "").Trim();

        if (adresse.Length < 3)
            return null;

        if (cache.TryGetValue(adresse, out var coordonneesEnCache))
            return coordonneesEnCache;

        string url =
            "https://data.geopf.fr/geocodage/completion/" +
            $"?text={Uri.EscapeDataString(adresse)}" +
            "&type=StreetAddress" +
            "&maximumResponses=1";

        try
        {
            using HttpClient httpClient =
                _httpClientFactory.CreateClient();

            using HttpResponseMessage response =
                await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return cache[adresse] = null;

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("results", out JsonElement results))
                return cache[adresse] = null;

            JsonElement premierResultat =
                results.EnumerateArray().FirstOrDefault();

            if (premierResultat.ValueKind == JsonValueKind.Undefined)
                return cache[adresse] = null;

            if (!premierResultat.TryGetProperty("x", out JsonElement x) ||
                !premierResultat.TryGetProperty("y", out JsonElement y))
                return cache[adresse] = null;

            return cache[adresse] =
                new Coordonnees(x.GetDouble(), y.GetDouble());
        }
        catch
        {
            return cache[adresse] = null;
        }
    }

    private static double CalculerDistanceKm(
        Coordonnees depart,
        Coordonnees arrivee)
    {
        const double rayonTerreKm = 6371;

        double dLat = ConvertirDegresEnRadians(arrivee.Latitude - depart.Latitude);
        double dLon = ConvertirDegresEnRadians(arrivee.Longitude - depart.Longitude);

        double lat1 = ConvertirDegresEnRadians(depart.Latitude);
        double lat2 = ConvertirDegresEnRadians(arrivee.Latitude);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
            Math.Cos(lat1) * Math.Cos(lat2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return rayonTerreKm * c;
    }

    private static double ConvertirDegresEnRadians(double degres)
    {
        return degres * Math.PI / 180;
    }

    private static string ConstruirePlanOptimiseTexte(OptimisationTourneeDto optimisation)
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("Ordre conseille chauffeur :");
        builder.AppendLine($"Distance approx. : {optimisation.DistanceApproxKm:0.0} km");
        builder.AppendLine();

        foreach (OptimisationArretDto arret in optimisation.Arrets)
        {
            builder.AppendLine($"{arret.Ordre}. {arret.Type} - {arret.Nom}");
            builder.AppendLine($"   {arret.Adresse}");

            if (arret.DistanceDepuisPrecedentKm > 0)
                builder.AppendLine($"   +{arret.DistanceDepuisPrecedentKm:0.0} km");

            foreach (string vehicule in arret.Vehicules)
            {
                builder.AppendLine($"   - {vehicule}");
            }
        }

        var livraisons = optimisation.Arrets
            .Where(a => a.Type == "Livraison")
            .SelectMany(a => a.Vehicules.Select(v => (
                Vehicule: v,
                OrdreLivraison: a.Ordre,
                AdresseLivraison: a.Adresse)))
            .ToList();

        var ordreChargement = CreerOrdreChargement(optimisation);

        if (ordreChargement.Count > 0)
        {
            var positions = optimisation.Chargement
                .GroupBy(c => c.Vehicule)
                .ToDictionary(g => g.Key, g => g.First().PositionConseillee);

            builder.AppendLine();
            builder.AppendLine("Ordre de chargement camion :");
            builder.AppendLine("Principe : garder la premiere livraison sur etage 1, puis charger l'etage 2 en priorite.");
            builder.AppendLine("La route favorise une livraison deja chargee si elle reste dans une distance raisonnable.");
            builder.AppendLine("A un meme chargement, le vehicule livre le plus tard est place avant celui qui sort plus tot.");

            for (int i = 0; i < ordreChargement.Count; i++)
            {
                var item = ordreChargement[i];

                string action = i == 0
                    ? "Charger en premier"
                    : i == ordreChargement.Count - 1
                        ? "Charger en dernier (sort en premier)"
                        : "Charger ensuite";

                string position = positions.TryGetValue(item.Vehicule, out string? positionConseillee)
                    ? $" - {positionConseillee}"
                    : "";

                builder.AppendLine($"{i + 1}. {action} : {item.Vehicule}{position}");

                if (item.OrdreLivraison.HasValue)
                {
                    builder.AppendLine(
                        $"   Livraison arret {item.OrdreLivraison} : {item.AdresseLivraison}");
                }
                else
                {
                    builder.AppendLine("   Livraison non optimisee");
                }
            }

            var vehiculesAvecLivraison = livraisons
                .Select(l => l.Vehicule)
                .ToHashSet();

            var vehiculesSansLivraison = optimisation.Arrets
                .Where(a => a.Type == "Chargement")
                .SelectMany(a => a.Vehicules)
                .Distinct()
                .Where(v => !vehiculesAvecLivraison.Contains(v))
                .ToList();

            if (vehiculesSansLivraison.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Vehicules sans adresse de livraison optimisee :");

                foreach (string vehicule in vehiculesSansLivraison)
                {
                    builder.AppendLine($"- {vehicule}");
                }
            }
        }

        if (optimisation.Chargement.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Contraintes de position :");

            foreach (OptimisationChargementDto chargement in optimisation.Chargement)
            {
                builder.AppendLine(
                    $"- {chargement.Vehicule} : {chargement.PositionConseillee}");
            }
        }

        return builder.ToString().Trim();
    }

    private static List<VehiculeACharger> CreerOrdreChargement(
        OptimisationTourneeDto optimisation)
    {
        var livraisonsParVehicule = optimisation.Arrets
            .Where(a => a.Type == "Livraison")
            .SelectMany(a => a.Vehicules.Select(v => new
            {
                Vehicule = v,
                OrdreLivraison = a.Ordre,
                AdresseLivraison = a.Adresse
            }))
            .GroupBy(l => l.Vehicule)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(l => l.OrdreLivraison).First());

        return optimisation.Arrets
            .Where(a => a.Type == "Chargement")
            .SelectMany(a => a.Vehicules.Select(v =>
            {
                bool livraisonTrouvee =
                    livraisonsParVehicule.TryGetValue(v, out var livraison);

                return new VehiculeACharger
                {
                    Vehicule = v,
                    OrdreChargement = a.Ordre,
                    OrdreLivraison = livraisonTrouvee
                        ? livraison!.OrdreLivraison
                        : null,
                    AdresseLivraison = livraisonTrouvee
                        ? livraison!.AdresseLivraison
                        : ""
                };
            }))
            .OrderBy(v => v.OrdreChargement)
            .ThenByDescending(v => v.OrdreLivraison ?? int.MaxValue)
            .ThenBy(v => v.Vehicule)
            .ToList();
    }

    private static List<OptimisationChargementDto> CreerPlanChargement(Tournee tournee)
    {
        var camion = tournee.CamionPorteur;

        if (camion == null)
            return new List<OptimisationChargementDto>();

        int hauteurAvant =
            camion.HauteurMaxAvantCm > 0
            ? camion.HauteurMaxAvantCm
            : camion.HauteurMaxCm;

        return tournee.Etapes
            .OrderBy(e => e.Ordre)
            .SelectMany(e => e.Vehicules.Select(v =>
            {
                bool haut = v.HauteurCm > hauteurAvant;

                return new OptimisationChargementDto
                {
                    Vehicule = ConstruireNomVehicule(v),
                    Immatriculation = v.Immatriculation,
                    PositionConseillee = haut ? "Arriere haut" : "Avant ou milieu",
                    Raison = haut
                        ? $"Hauteur {v.HauteurCm} cm > zone avant {hauteurAvant} cm"
                        : $"Hauteur {v.HauteurCm} cm compatible avec la zone avant"
                };
            }))
            .ToList();
    }

    private static string ConstruireNomVehicule(Vehicule vehicule)
    {
        string nom = $"{vehicule.Marque} {vehicule.Modele}".Trim();

        if (!string.IsNullOrWhiteSpace(vehicule.Immatriculation))
            nom += $" - {vehicule.Immatriculation}";

        return nom;
    }

    private sealed class CandidatOptimisation
    {
        public string Type { get; set; } = "";

        public string Nom { get; set; } = "";

        public string Adresse { get; set; } = "";

        public List<Vehicule> Vehicules { get; set; } = new();

        public Etape? Etape { get; set; }

        public Coordonnees? Coordonnees { get; set; }

        public double DistanceDepuisPrecedentKm { get; set; }
    }

    private sealed class VehiculeACharger
    {
        public string Vehicule { get; set; } = "";

        public int OrdreChargement { get; set; }

        public int? OrdreLivraison { get; set; }

        public string AdresseLivraison { get; set; } = "";
    }

    private sealed record Coordonnees(double Longitude, double Latitude);

    private async Task<Dictionary<int, ModeleVehicule>> ChargerModelesVehiculesAsync(
        IEnumerable<CreateEtapeDto> etapes)
    {
        var ids = etapes
            .SelectMany(e => e.Vehicules ?? new List<CreateVehiculeDto>())
            .Where(v => v.ModeleVehiculeId.HasValue)
            .Select(v => v.ModeleVehiculeId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return new Dictionary<int, ModeleVehicule>();

        return await _context.ModelesVehicules
            .Where(m => ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);
    }

    private static Vehicule CreerVehicule(
        CreateVehiculeDto request,
        Dictionary<int, ModeleVehicule> modelesVehicules)
    {
        ModeleVehicule? modeleVehicule = null;

        if (request.ModeleVehiculeId.HasValue)
            modelesVehicules.TryGetValue(request.ModeleVehiculeId.Value, out modeleVehicule);

        return new Vehicule
        {
            ModeleVehiculeId = modeleVehicule?.Id ?? request.ModeleVehiculeId,
            Marque = modeleVehicule?.Marque ?? (request.Marque ?? "").Trim(),
            Modele = modeleVehicule?.Modele ?? (request.Modele ?? "").Trim(),
            Immatriculation = (request.Immatriculation ?? "").Trim(),
            LongueurCm = modeleVehicule?.LongueurCm ?? request.LongueurCm,
            LargeurCm = modeleVehicule?.LargeurCm ?? request.LargeurCm,
            HauteurCm = modeleVehicule?.HauteurCm ?? request.HauteurCm,
            PoidsKg = modeleVehicule?.PoidsKg ?? request.PoidsKg,
            AdresseLivraison = (request.AdresseLivraison ?? "").Trim(),
            ClientLivraison = (request.ClientLivraison ?? "").Trim(),
            Recupere = request.Recupere
        };
    }
}

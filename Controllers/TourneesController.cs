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
    private const double CoutKmParVehiculeCharge = 0.25;
    private const double DistanceRegroupementChargementKm = 15;

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
            AdresseDepartRetour = (request.AdresseDepartRetour ?? "").Trim(),
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
        tournee.AdresseDepartRetour = (request.AdresseDepartRetour ?? "").Trim();
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
            CodeUnique = tournee.CodeUnique,
            AdresseDepartRetour = tournee.AdresseDepartRetour
        };

        resultat.Alertes.Add(
            "Optimisation indicative : elle aide a preparer l'ordre, mais le chauffeur garde la decision finale sur le terrain.");

        resultat.Alertes.Add(
            "L'optimisation tient compte du point depart / retour quand il est renseigne.");

        var etapes = tournee.Etapes
            .OrderBy(e => e.Ordre)
            .ToList();

        var coordonneesCache = new Dictionary<string, Coordonnees?>();
        var coordonneesEtapes = new Dictionary<int, Coordonnees?>();
        var coordonneesLivraisons =
            new Dictionary<string, Coordonnees?>(StringComparer.OrdinalIgnoreCase);

        Coordonnees? coordonneesDepartRetour =
            await GeocoderAsync(tournee.AdresseDepartRetour, coordonneesCache);

        if (!string.IsNullOrWhiteSpace(tournee.AdresseDepartRetour) &&
            coordonneesDepartRetour == null)
        {
            resultat.Alertes.Add(
                "Le point depart / retour n'a pas pu etre geocode, il est affiche mais pas chiffre.");
        }

        foreach (Etape etape in etapes)
        {
            coordonneesEtapes[etape.Id] =
                await GeocoderAsync(etape.Adresse, coordonneesCache);
        }

        foreach (string adresseLivraison in etapes
            .SelectMany(e => e.Vehicules)
            .Select(v => (v.AdresseLivraison ?? "").Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            coordonneesLivraisons[adresseLivraison] =
                await GeocoderAsync(adresseLivraison, coordonneesCache);
        }

        SolutionOptimisation solution =
            ConstruireRouteOptimale(
                etapes,
                tournee.AdresseDepartRetour,
                coordonneesDepartRetour,
                coordonneesEtapes,
                coordonneesLivraisons);

        int ordre = 1;

        foreach (CandidatOptimisation arret in solution.Arrets)
        {
            arret.Ordre = ordre++;

            resultat.Arrets.Add(new OptimisationArretDto
            {
                EtapeId = arret.Etape?.Id,
                Ordre = arret.Ordre,
                Type = arret.Type,
                Nom = arret.Nom,
                Adresse = arret.Adresse,
                DistanceDepuisPrecedentKm = Math.Round(arret.DistanceDepuisPrecedentKm, 1),
                Vehicules = arret.Vehicules
                    .Select(ConstruireNomVehicule)
                    .ToList()
            });
        }

        resultat.DistanceApproxKm = Math.Round(solution.DistanceTotaleKm, 1);
        resultat.Chargement = CreerPlanChargement(tournee);

        return resultat;
    }

    private static SolutionOptimisation ConstruireRouteOptimale(
        List<Etape> etapes,
        string adresseDepartRetour,
        Coordonnees? coordonneesDepartRetour,
        Dictionary<int, Coordonnees?> coordonneesEtapes,
        Dictionary<string, Coordonnees?> coordonneesLivraisons)
    {
        SolutionOptimisation meilleure =
            ConstruireRouteGloutonne(
                etapes,
                adresseDepartRetour,
                coordonneesDepartRetour,
                coordonneesEtapes,
                coordonneesLivraisons);

        var etapesChargees = new HashSet<int>();
        var vehiculesCharges = new List<Vehicule>();
        var route = new List<CandidatOptimisation>();
        var memoire = new Dictionary<string, double>();
        int visites = 0;
        const int limiteVisites = 250000;

        ChercherMeilleureRoute(
            etapes,
            adresseDepartRetour,
            coordonneesDepartRetour,
            coordonneesEtapes,
            coordonneesLivraisons,
            etapesChargees,
            vehiculesCharges,
            coordonneesDepartRetour,
            "DEPOT",
            0,
            0,
            route,
            memoire,
            ref meilleure,
            ref visites,
            limiteVisites);

        return meilleure;
    }

    private static void ChercherMeilleureRoute(
        List<Etape> etapes,
        string adresseDepartRetour,
        Coordonnees? coordonneesDepartRetour,
        Dictionary<int, Coordonnees?> coordonneesEtapes,
        Dictionary<string, Coordonnees?> coordonneesLivraisons,
        HashSet<int> etapesChargees,
        List<Vehicule> vehiculesCharges,
        Coordonnees? positionActuelle,
        string positionCle,
        double distanceActuelle,
        double scoreActuel,
        List<CandidatOptimisation> route,
        Dictionary<string, double> memoire,
        ref SolutionOptimisation meilleure,
        ref int visites,
        int limiteVisites)
    {
        if (visites++ > limiteVisites)
            return;

        if (scoreActuel >= meilleure.ScoreTotal)
            return;

        string etatCle =
            ConstruireCleEtat(positionCle, etapesChargees, vehiculesCharges);

        if (memoire.TryGetValue(etatCle, out double distanceDejaVue) &&
            distanceDejaVue <= scoreActuel)
        {
            return;
        }

        memoire[etatCle] = scoreActuel;

        if (etapesChargees.Count == etapes.Count &&
            vehiculesCharges.Count == 0)
        {
            double distanceRetour =
                string.IsNullOrWhiteSpace(adresseDepartRetour)
                    ? 0
                    : CalculerDistanceKmNullable(
                        positionActuelle,
                        coordonneesDepartRetour);

            double distanceTotale = distanceActuelle + distanceRetour;
            double scoreTotal =
                scoreActuel +
                CalculerScoreSegment(distanceRetour, vehiculesCharges.Count);

            if (scoreTotal < meilleure.ScoreTotal ||
                (Math.Abs(scoreTotal - meilleure.ScoreTotal) < 0.001 &&
                    distanceTotale < meilleure.DistanceTotaleKm))
            {
                var arrets = route
                    .Select(CopierCandidat)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(adresseDepartRetour))
                {
                    arrets.Add(new CandidatOptimisation
                    {
                        Cle = "DEPOT",
                        Type = "Retour",
                        Nom = "Domicile camionneur",
                        Adresse = adresseDepartRetour,
                        Coordonnees = coordonneesDepartRetour,
                        DistanceDepuisPrecedentKm = distanceRetour
                    });
                }

                meilleure = new SolutionOptimisation
                {
                    DistanceTotaleKm = distanceTotale,
                    ScoreTotal = scoreTotal,
                    Arrets = arrets
                };
            }

            return;
        }

        List<CandidatOptimisation> candidats =
            CreerCandidatsRoute(
                etapes,
                etapesChargees,
                vehiculesCharges,
                adresseDepartRetour,
                coordonneesEtapes,
                coordonneesLivraisons);

        foreach (CandidatOptimisation candidat in candidats
            .OrderBy(c => CalculerDistanceKmNullable(positionActuelle, c.Coordonnees))
            .ThenBy(c => c.Type == "Livraison" ? 0 : 1)
            .ThenBy(c => c.Etape?.Ordre ?? int.MaxValue)
            .ThenBy(c => c.Nom))
        {
            double distance =
                CalculerDistanceKmNullable(positionActuelle, candidat.Coordonnees);

            double nouvelleDistance = distanceActuelle + distance;
            double nouveauScore =
                scoreActuel +
                CalculerScoreSegment(
                    distance,
                    vehiculesCharges.Count,
                    positionCle,
                    candidat);

            if (nouveauScore >= meilleure.ScoreTotal)
                continue;

            var prochainesEtapesChargees =
                new HashSet<int>(etapesChargees);

            var prochainsVehiculesCharges =
                vehiculesCharges.ToList();

            if (candidat.Type == "Chargement" && candidat.Etape != null)
            {
                prochainesEtapesChargees.Add(candidat.Etape.Id);
                prochainsVehiculesCharges.AddRange(candidat.Etape.Vehicules);
            }
            else if (candidat.Type == "Livraison")
            {
                var idsLivres = candidat.Vehicules
                    .Select(v => v.Id)
                    .ToHashSet();

                prochainsVehiculesCharges =
                    prochainsVehiculesCharges
                        .Where(v => !idsLivres.Contains(v.Id))
                        .ToList();
            }

            CandidatOptimisation arret =
                CopierCandidat(candidat);

            arret.DistanceDepuisPrecedentKm = distance;
            route.Add(arret);

            ChercherMeilleureRoute(
                etapes,
                adresseDepartRetour,
                coordonneesDepartRetour,
                coordonneesEtapes,
                coordonneesLivraisons,
                prochainesEtapesChargees,
                prochainsVehiculesCharges,
                candidat.Coordonnees,
                candidat.Cle,
                nouvelleDistance,
                nouveauScore,
                route,
                memoire,
                ref meilleure,
                ref visites,
                limiteVisites);

            route.RemoveAt(route.Count - 1);
        }
    }

    private static SolutionOptimisation ConstruireRouteGloutonne(
        List<Etape> etapes,
        string adresseDepartRetour,
        Coordonnees? coordonneesDepartRetour,
        Dictionary<int, Coordonnees?> coordonneesEtapes,
        Dictionary<string, Coordonnees?> coordonneesLivraisons)
    {
        var etapesChargees = new HashSet<int>();
        var vehiculesCharges = new List<Vehicule>();
        var arrets = new List<CandidatOptimisation>();
        Coordonnees? positionActuelle = coordonneesDepartRetour;
        string positionCle = "DEPOT";
        double distanceTotale = 0;
        double scoreTotal = 0;

        while (etapesChargees.Count < etapes.Count ||
            vehiculesCharges.Count > 0)
        {
            List<CandidatOptimisation> candidats =
                CreerCandidatsRoute(
                    etapes,
                    etapesChargees,
                    vehiculesCharges,
                    adresseDepartRetour,
                    coordonneesEtapes,
                    coordonneesLivraisons);

            if (candidats.Count == 0)
                break;

            CandidatOptimisation choisi = candidats
                .OrderBy(c =>
                {
                    double distanceCandidat =
                        CalculerDistanceKmNullable(positionActuelle, c.Coordonnees);

                    return CalculerScoreSegment(
                        distanceCandidat,
                        vehiculesCharges.Count,
                        positionCle,
                        c);
                })
                .ThenBy(c => CalculerDistanceKmNullable(positionActuelle, c.Coordonnees))
                .ThenBy(c => c.Type == "Livraison" ? 0 : 1)
                .ThenBy(c => c.Etape?.Ordre ?? int.MaxValue)
                .First();

            double distance =
                CalculerDistanceKmNullable(positionActuelle, choisi.Coordonnees);

            distanceTotale += distance;
            scoreTotal += CalculerScoreSegment(
                distance,
                vehiculesCharges.Count,
                positionCle,
                choisi);

            CandidatOptimisation arret =
                CopierCandidat(choisi);

            arret.DistanceDepuisPrecedentKm = distance;
            arrets.Add(arret);

            if (choisi.Type == "Chargement" && choisi.Etape != null)
            {
                etapesChargees.Add(choisi.Etape.Id);
                vehiculesCharges.AddRange(choisi.Etape.Vehicules);
            }
            else if (choisi.Type == "Livraison")
            {
                var idsLivres = choisi.Vehicules
                    .Select(v => v.Id)
                    .ToHashSet();

                vehiculesCharges =
                    vehiculesCharges
                        .Where(v => !idsLivres.Contains(v.Id))
                        .ToList();
            }

            positionActuelle = choisi.Coordonnees;
            positionCle = choisi.Cle;
        }

        if (!string.IsNullOrWhiteSpace(adresseDepartRetour))
        {
            double retour =
                CalculerDistanceKmNullable(positionActuelle, coordonneesDepartRetour);

            distanceTotale += retour;
            scoreTotal += CalculerScoreSegment(retour, vehiculesCharges.Count);

            arrets.Add(new CandidatOptimisation
            {
                Cle = "DEPOT",
                Type = "Retour",
                Nom = "Domicile camionneur",
                Adresse = adresseDepartRetour,
                Coordonnees = coordonneesDepartRetour,
                DistanceDepuisPrecedentKm = retour
            });
        }

        return new SolutionOptimisation
        {
            DistanceTotaleKm = distanceTotale,
            ScoreTotal = scoreTotal,
            Arrets = arrets
        };
    }

    private static List<CandidatOptimisation> CreerCandidatsRoute(
        List<Etape> etapes,
        HashSet<int> etapesChargees,
        List<Vehicule> vehiculesCharges,
        string adresseDepartRetour,
        Dictionary<int, Coordonnees?> coordonneesEtapes,
        Dictionary<string, Coordonnees?> coordonneesLivraisons)
    {
        var candidats = new List<CandidatOptimisation>();

        var etapesRestantes = etapes
            .Where(e => !etapesChargees.Contains(e.Id))
            .OrderBy(e => e.Ordre)
            .ToList();

        if (string.IsNullOrWhiteSpace(adresseDepartRetour) &&
            etapesChargees.Count == 0 &&
            etapesRestantes.Count > 0)
        {
            etapesRestantes = etapesRestantes
                .Take(1)
                .ToList();
        }

        foreach (Etape etape in etapesRestantes)
        {
            candidats.Add(new CandidatOptimisation
            {
                Cle = $"C:{etape.Id}",
                Type = "Chargement",
                Nom = string.IsNullOrWhiteSpace(etape.Garage)
                    ? $"Etape {etape.Ordre}"
                    : etape.Garage,
                Adresse = etape.Adresse,
                Vehicules = etape.Vehicules.ToList(),
                Etape = etape,
                Coordonnees = coordonneesEtapes.TryGetValue(etape.Id, out Coordonnees? coordonnees)
                    ? coordonnees
                    : null
            });
        }

        foreach (var livraison in vehiculesCharges
            .Where(v => !string.IsNullOrWhiteSpace(v.AdresseLivraison))
            .GroupBy(v => v.AdresseLivraison.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            candidats.Add(new CandidatOptimisation
            {
                Cle = $"L:{livraison.Key.ToUpperInvariant()}",
                Type = "Livraison",
                Nom = string.Join(", ", livraison
                    .Select(v => v.ClientLivraison)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)),
                Adresse = livraison.Key,
                Vehicules = livraison.ToList(),
                Coordonnees = coordonneesLivraisons.TryGetValue(livraison.Key, out Coordonnees? coordonnees)
                    ? coordonnees
                    : null
            });
        }

        return candidats;
    }

    private static string ConstruireCleEtat(
        string positionCle,
        HashSet<int> etapesChargees,
        List<Vehicule> vehiculesCharges)
    {
        string etapes = string.Join(
            ",",
            etapesChargees.OrderBy(id => id));

        string vehicules = string.Join(
            ",",
            vehiculesCharges.Select(v => v.Id).OrderBy(id => id));

        return $"{positionCle}|E:{etapes}|V:{vehicules}";
    }

    private static CandidatOptimisation CopierCandidat(
        CandidatOptimisation candidat)
    {
        return new CandidatOptimisation
        {
            Cle = candidat.Cle,
            Ordre = candidat.Ordre,
            Type = candidat.Type,
            Nom = candidat.Nom,
            Adresse = candidat.Adresse,
            Vehicules = candidat.Vehicules.ToList(),
            Etape = candidat.Etape,
            Coordonnees = candidat.Coordonnees,
            DistanceDepuisPrecedentKm = candidat.DistanceDepuisPrecedentKm
        };
    }

    private static double CalculerDistanceKmNullable(
        Coordonnees? depart,
        Coordonnees? arrivee)
    {
        if (depart == null || arrivee == null)
            return 0;

        return CalculerDistanceKm(depart, arrivee);
    }

    private static double CalculerScoreSegment(
        double distanceKm,
        int nombreVehiculesCharges)
    {
        return distanceKm * (1 + Math.Max(0, nombreVehiculesCharges) * CoutKmParVehiculeCharge);
    }

    private static double CalculerScoreSegment(
        double distanceKm,
        int nombreVehiculesCharges,
        string positionCle,
        CandidatOptimisation candidat)
    {
        bool chargementLocal =
            candidat.Type == "Chargement" &&
            positionCle.StartsWith("C:", StringComparison.OrdinalIgnoreCase) &&
            distanceKm <= DistanceRegroupementChargementKm;

        return CalculerScoreSegment(
            distanceKm,
            chargementLocal ? 0 : nombreVehiculesCharges);
    }

    private async Task AppliquerOptimisationAsync(Tournee tournee)
    {
        try
        {
            OptimisationTourneeDto optimisation =
                await ConstruireOptimisationAsync(tournee);

            tournee.PlanOptimise =
                ConstruirePlanOptimiseTexte(optimisation);

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
            !tournee.PlanOptimise.Contains("point depart / retour") ||
            !tournee.PlanOptimise.Contains("kilometres avec vehicules charges") ||
            !tournee.PlanOptimise.Contains("meme zone de chargement"))
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

        if (!string.IsNullOrWhiteSpace(optimisation.AdresseDepartRetour))
            builder.AppendLine($"Depart / retour : {optimisation.AdresseDepartRetour}");

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
            builder.AppendLine("La route est optimisee depuis le point depart / retour du chauffeur.");
            builder.AppendLine("La route limite aussi les kilometres avec vehicules charges pour eviter les detours inutiles.");
            builder.AppendLine("Les garages proches restent groupes dans la meme zone de chargement.");
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
        public string Cle { get; set; } = "";

        public int Ordre { get; set; }

        public string Type { get; set; } = "";

        public string Nom { get; set; } = "";

        public string Adresse { get; set; } = "";

        public List<Vehicule> Vehicules { get; set; } = new();

        public Etape? Etape { get; set; }

        public Coordonnees? Coordonnees { get; set; }

        public double DistanceDepuisPrecedentKm { get; set; }
    }

    private sealed class SolutionOptimisation
    {
        public double DistanceTotaleKm { get; set; } = double.MaxValue;

        public double ScoreTotal { get; set; } = double.MaxValue;

        public List<CandidatOptimisation> Arrets { get; set; } = new();
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

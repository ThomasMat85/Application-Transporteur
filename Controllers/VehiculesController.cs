using Application_Camion_API.Data;
using Application_Camion_API.DTOs;
using Application_Camion_API.Models;
using Application_Camion_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application_Camion_API.Controllers;

[Route("api/vehicules")]
[ApiController]
public class VehiculesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VehiculesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET : api/vehicules
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehiculeDto>>> GetVehicules()
    {
        var vehicules = await _context.Vehicules
            .OrderBy(v => v.Id)
            .ToListAsync();

        return vehicules.Select(v => v.ToDto()).ToList();
    }

    // GET : api/vehicules/5
    [HttpGet("{id}")]
    public async Task<ActionResult<VehiculeDto>> GetVehicule(int id)
    {
        var vehicule = await _context.Vehicules.FindAsync(id);

        if (vehicule == null)
            return NotFound();

        return vehicule.ToDto();
    }

    // POST : api/vehicules
    [HttpPost]
    public async Task<ActionResult<VehiculeDto>> CreateVehicule(CreateVehiculeForEtapeDto request)
    {
        if (!await _context.Etapes.AnyAsync(e => e.Id == request.EtapeId))
            return NotFound("Etape introuvable.");

        var modeleVehicule = request.ModeleVehiculeId.HasValue
            ? await _context.ModelesVehicules.FindAsync(request.ModeleVehiculeId.Value)
            : null;

        var vehicule = new Vehicule
        {
            EtapeId = request.EtapeId,
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

        _context.Vehicules.Add(vehicule);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetVehicule),
            new { id = vehicule.Id },
            vehicule.ToDto());
    }

    // PUT : api/vehicules/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVehicule(int id, UpdateVehiculeDto request)
    {
        var vehicule = await _context.Vehicules.FindAsync(id);

        if (vehicule == null)
            return NotFound();

        var modeleVehicule = request.ModeleVehiculeId.HasValue
            ? await _context.ModelesVehicules.FindAsync(request.ModeleVehiculeId.Value)
            : null;

        vehicule.ModeleVehiculeId = modeleVehicule?.Id ?? request.ModeleVehiculeId;
        vehicule.Marque = modeleVehicule?.Marque ?? (request.Marque ?? "").Trim();
        vehicule.Modele = modeleVehicule?.Modele ?? (request.Modele ?? "").Trim();
        vehicule.Immatriculation = (request.Immatriculation ?? "").Trim();
        vehicule.LongueurCm = modeleVehicule?.LongueurCm ?? request.LongueurCm;
        vehicule.LargeurCm = modeleVehicule?.LargeurCm ?? request.LargeurCm;
        vehicule.HauteurCm = modeleVehicule?.HauteurCm ?? request.HauteurCm;
        vehicule.PoidsKg = modeleVehicule?.PoidsKg ?? request.PoidsKg;
        vehicule.AdresseLivraison = (request.AdresseLivraison ?? "").Trim();
        vehicule.ClientLivraison = (request.ClientLivraison ?? "").Trim();
        vehicule.Recupere = request.Recupere;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH : api/vehicules/5/recupere
    [HttpPatch("{id}/recupere")]
    public async Task<IActionResult> UpdateRecupere(int id, UpdateVehiculeRecupereDto request)
    {
        var vehicule = await _context.Vehicules.FindAsync(id);

        if (vehicule == null)
            return NotFound();

        vehicule.Recupere = request.Recupere;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE : api/vehicules/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicule(int id)
    {
        var vehicule = await _context.Vehicules.FindAsync(id);

        if (vehicule == null)
            return NotFound();

        _context.Vehicules.Remove(vehicule);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

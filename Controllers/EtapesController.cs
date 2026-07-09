using Application_Camion_API.Data;
using Application_Camion_API.DTOs;
using Application_Camion_API.Models;
using Application_Camion_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application_Camion_API.Controllers;

[Route("api/etapes")]
[ApiController]
public class EtapesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EtapesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET : api/etapes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EtapeDto>>> GetEtapes()
    {
        var etapes = await _context.Etapes
            .Include(e => e.Vehicules)
            .OrderBy(e => e.TourneeId)
            .ThenBy(e => e.Ordre)
            .ToListAsync();

        return etapes.Select(e => e.ToDto()).ToList();
    }

    // GET : api/etapes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<EtapeDto>> GetEtape(int id)
    {
        var etape = await _context.Etapes
            .Include(e => e.Vehicules)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (etape == null)
            return NotFound();

        return etape.ToDto();
    }

    // POST : api/etapes
    [HttpPost]
    public async Task<ActionResult<EtapeDto>> CreateEtape(CreateEtapeForTourneeDto request)
    {
        if (request.Ordre <= 0)
            return BadRequest("L'ordre de l'etape doit etre superieur a 0.");

        if (!await _context.Tournees.AnyAsync(t => t.Id == request.TourneeId))
            return NotFound("Tournee introuvable.");

        var etape = new Etape
        {
            TourneeId = request.TourneeId,
            Ordre = request.Ordre,
            Garage = (request.Garage ?? "").Trim(),
            Adresse = (request.Adresse ?? "").Trim(),
            Vehicules = (request.Vehicules ?? new List<CreateVehiculeDto>()).Select(v => new Vehicule
            {
                Marque = (v.Marque ?? "").Trim(),
                Modele = (v.Modele ?? "").Trim(),
                Immatriculation = (v.Immatriculation ?? "").Trim(),
                Recupere = v.Recupere
            }).ToList()
        };

        _context.Etapes.Add(etape);

        await _context.SaveChangesAsync();

        var createdEtape = await _context.Etapes
            .Include(e => e.Vehicules)
            .FirstAsync(e => e.Id == etape.Id);

        return CreatedAtAction(
            nameof(GetEtape),
            new { id = etape.Id },
            createdEtape.ToDto());
    }

    // PUT : api/etapes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEtape(int id, UpdateEtapeDto request)
    {
        var etape = await _context.Etapes.FindAsync(id);

        if (etape == null)
            return NotFound();

        if (request.Ordre <= 0)
            return BadRequest("L'ordre de l'etape doit etre superieur a 0.");

        etape.Ordre = request.Ordre;
        etape.Garage = (request.Garage ?? "").Trim();
        etape.Adresse = (request.Adresse ?? "").Trim();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE : api/etapes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEtape(int id)
    {
        var etape = await _context.Etapes.FindAsync(id);

        if (etape == null)
            return NotFound();

        _context.Etapes.Remove(etape);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

using Application_Camion_API.Data;
using Application_Camion_API.DTOs;
using Application_Camion_API.Models;
using Application_Camion_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application_Camion_API.Controllers;

[Route("api/modeles-vehicules")]
[ApiController]
public class ModelesVehiculesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ModelesVehiculesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ModeleVehiculeDto>>> GetModeles()
    {
        var modeles = await _context.ModelesVehicules
            .OrderBy(m => m.Marque)
            .ThenBy(m => m.Modele)
            .ToListAsync();

        return modeles.Select(m => m.ToDto()).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<ModeleVehiculeDto>> CreateModele(CreateModeleVehiculeDto request)
    {
        var marque = (request.Marque ?? "").Trim();
        var modele = (request.Modele ?? "").Trim();

        if (string.IsNullOrWhiteSpace(marque) || string.IsNullOrWhiteSpace(modele))
            return BadRequest("La marque et le modele sont obligatoires.");

        if (request.LongueurCm <= 0 || request.LargeurCm <= 0 || request.HauteurCm <= 0 || request.PoidsKg <= 0)
            return BadRequest("Les dimensions et le poids doivent etre superieurs a 0.");

        if (await _context.ModelesVehicules.AnyAsync(m => m.Marque == marque && m.Modele == modele))
            return Conflict("Ce modele existe deja.");

        var modeleVehicule = new ModeleVehicule
        {
            Marque = marque,
            Modele = modele,
            LongueurCm = request.LongueurCm,
            LargeurCm = request.LargeurCm,
            HauteurCm = request.HauteurCm,
            PoidsKg = request.PoidsKg
        };

        _context.ModelesVehicules.Add(modeleVehicule);
        await _context.SaveChangesAsync();

        return modeleVehicule.ToDto();
    }
}

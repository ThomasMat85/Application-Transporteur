using Application_Camion_API.Data;
using Application_Camion_API.DTOs;
using Application_Camion_API.Models;
using Application_Camion_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application_Camion_API.Controllers;

[Route("api/camions-porteurs")]
[ApiController]
public class CamionsPorteursController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CamionsPorteursController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CamionPorteurDto>>> GetCamions()
    {
        var camions = await _context.CamionsPorteurs
            .Where(c => c.Actif)
            .OrderBy(c => c.Nom)
            .ToListAsync();

        return camions.Select(c => c.ToDto()).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<CamionPorteurDto>> CreateCamion(CreateCamionPorteurDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom du camion est obligatoire.");

        if (request.LongueurUtileCm <= 0 || request.LargeurUtileCm <= 0 ||
            request.HauteurMaxCm <= 0 || request.ChargeUtileKg <= 0 || request.NombreNiveaux <= 0)
            return BadRequest("Les capacites du camion doivent etre superieures a 0.");

        int hauteurAvant =
            request.HauteurMaxAvantCm > 0
            ? request.HauteurMaxAvantCm
            : request.HauteurMaxCm;

        int hauteurArriere =
            request.HauteurMaxArriereCm > 0
            ? request.HauteurMaxArriereCm
            : request.HauteurMaxCm;

        int longueurZoneArriere =
            request.LongueurZoneArriereCm > 0
            ? request.LongueurZoneArriereCm
            : request.LongueurUtileCm;

        if (hauteurAvant > hauteurArriere)
            return BadRequest("La hauteur arriere doit etre superieure ou egale a la hauteur avant.");

        if (longueurZoneArriere > request.LongueurUtileCm)
            return BadRequest("La zone arriere ne peut pas etre plus longue que le camion.");

        var camion = new CamionPorteur
        {
            Nom = request.Nom.Trim(),
            LongueurUtileCm = request.LongueurUtileCm,
            LargeurUtileCm = request.LargeurUtileCm,
            HauteurMaxCm = request.HauteurMaxCm,
            HauteurMaxAvantCm = hauteurAvant,
            HauteurMaxArriereCm = hauteurArriere,
            LongueurZoneArriereCm = longueurZoneArriere,
            ChargeUtileKg = request.ChargeUtileKg,
            NombreNiveaux = request.NombreNiveaux,
            Actif = true
        };

        _context.CamionsPorteurs.Add(camion);
        await _context.SaveChangesAsync();

        return camion.ToDto();
    }
}

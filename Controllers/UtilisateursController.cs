using Application_Camion_API.Data;
using Application_Camion_API.DTOs;
using Application_Camion_API.Models;
using Application_Camion_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application_Camion_API.Controllers;

[Route("api/utilisateurs")]
[ApiController]
public class UtilisateursController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UtilisateursController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET : api/utilisateurs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UtilisateurDto>>> GetUtilisateurs()
    {
        var utilisateurs = await _context.Utilisateurs
            .OrderBy(u => u.Nom)
            .ToListAsync();

        return utilisateurs.Select(u => u.ToDto()).ToList();
    }

    // GET : api/utilisateurs/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UtilisateurDto>> GetUtilisateur(int id)
    {
        var utilisateur = await _context.Utilisateurs.FindAsync(id);

        if (utilisateur == null)
            return NotFound();

        return utilisateur.ToDto();
    }

    // POST : api/utilisateurs
    [HttpPost]
    public async Task<ActionResult<UtilisateurDto>> CreateUtilisateur(CreateUtilisateurDto request)
    {
        var nom = (request.Nom ?? "").Trim();

        if (string.IsNullOrWhiteSpace(nom))
            return BadRequest("Le nom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.MotDePasse))
            return BadRequest("Le mot de passe est obligatoire.");

        if (await _context.Utilisateurs.AnyAsync(u => u.Nom == nom))
            return Conflict($"L'utilisateur {nom} existe deja.");

        var utilisateur = new Utilisateur
        {
            Nom = nom,
            MotDePasse = request.MotDePasse,
            Role = (request.Role ?? "").Trim().ToLowerInvariant()
        };

        _context.Utilisateurs.Add(utilisateur);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetUtilisateur),
            new { id = utilisateur.Id },
            utilisateur.ToDto());
    }

    // POST : api/utilisateurs/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto request)
    {
        var nom = (request.Nom ?? "").Trim();

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Nom == nom && u.MotDePasse == request.MotDePasse);

        if (utilisateur == null)
        {
            return Unauthorized(new LoginResponseDto
            {
                Success = false,
                Message = "Identifiants incorrects."
            });
        }

        return new LoginResponseDto
        {
            Success = true,
            Message = "Connexion reussie.",
            Utilisateur = utilisateur.ToDto()
        };
    }

    // DELETE : api/utilisateurs/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUtilisateur(int id)
    {
        var utilisateur = await _context.Utilisateurs.FindAsync(id);

        if (utilisateur == null)
            return NotFound();

        _context.Utilisateurs.Remove(utilisateur);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

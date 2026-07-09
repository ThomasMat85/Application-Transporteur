namespace Application_Camion_API.DTOs;

public class LoginResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = "";

    public UtilisateurDto? Utilisateur { get; set; }
}

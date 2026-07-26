namespace KONTAXPRO.Application.Models;

public class LoginResult
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public long? UsuarioId { get; set; }

    public string? NombreCompleto { get; set; }
}
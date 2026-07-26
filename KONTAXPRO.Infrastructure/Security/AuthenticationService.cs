using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Security;

public class AuthenticationService : IAuthenticationService
{
    private readonly IDbContextFactory<KontaxDbContext> _dbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly CurrentSession _currentSession;

    public AuthenticationService(
        IDbContextFactory<KontaxDbContext> dbContextFactory,
        IPasswordHasher passwordHasher,
        CurrentSession currentSession)
    {
        _dbContextFactory = dbContextFactory;
        _passwordHasher = passwordHasher;
        _currentSession = currentSession;
    }

    public async Task<LoginResult> LoginAsync(
        string numeroIdentificacion,
        string password)
    {
        if (string.IsNullOrWhiteSpace(numeroIdentificacion))
        {
            return new LoginResult
            {
                Success = false,
                Message = "Debe ingresar el número de identificación."
            };
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new LoginResult
            {
                Success = false,
                Message = "Debe ingresar la contraseña."
            };
        }

        await using var context =
            await _dbContextFactory.CreateDbContextAsync();

        var usuario = await context.Usuarios
            .FirstOrDefaultAsync(x =>
                x.NumeroIdentificacion == numeroIdentificacion.Trim() &&
                x.Estado == 1);

        if (usuario is null)
        {
            return new LoginResult
            {
                Success = false,
                Message = "Usuario o contraseña incorrectos."
            };
        }

        var passwordValido = _passwordHasher.Verify(
            password,
            usuario.PasswordHash);

        if (!passwordValido)
        {
            return new LoginResult
            {
                Success = false,
                Message = "Usuario o contraseña incorrectos."
            };
        }

        // Limpiar cualquier sesión anterior
        _currentSession.Clear();

        // Registrar usuario autenticado en la sesión actual
        _currentSession.UsuarioId = usuario.Id;
        _currentSession.NumeroIdentificacion = usuario.NumeroIdentificacion;
        _currentSession.NombreCompleto = usuario.NombreCompleto;

        usuario.UltimoAccesoAt = DateTime.UtcNow;
        usuario.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            UsuarioId = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Message = "Inicio de sesión correcto."
        };
    }
}

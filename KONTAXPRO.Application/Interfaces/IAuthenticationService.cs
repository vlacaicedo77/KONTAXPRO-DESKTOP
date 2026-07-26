using KONTAXPRO.Application.Models;

namespace KONTAXPRO.Application.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(
        string numeroIdentificacion,
        string password);
}
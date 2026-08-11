using KONTAXPRO.Application.Models.Tesoreria;

namespace KONTAXPRO.Application.Interfaces;

public interface ISoporteSinSustentoStorage
{
    Task<SoporteSinSustentoGuardadoDto> GuardarAsync(
        long empresaId,
        string nombreArchivo,
        ReadOnlyMemory<byte> contenido,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(
        string rutaRelativa,
        CancellationToken cancellationToken = default);

    Task<SoporteSinSustentoContenidoDto> LeerVerificadoAsync(
        string rutaRelativa,
        string nombreArchivo,
        string sha256Esperado,
        long tamanoEsperado,
        CancellationToken cancellationToken = default);
}

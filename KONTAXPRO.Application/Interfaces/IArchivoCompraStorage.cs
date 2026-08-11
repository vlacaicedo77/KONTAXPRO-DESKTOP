using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Application.Interfaces;

public interface IArchivoCompraStorage
{
    Task<ArchivoCompraGuardadoDto> GuardarXmlOriginalAsync(
        long empresaId,
        string claveAcceso,
        string sha256Esperado,
        ReadOnlyMemory<byte> contenido,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(
        string rutaRelativa,
        CancellationToken cancellationToken = default);

    Task<Stream> AbrirLecturaAsync(
        string rutaRelativa,
        CancellationToken cancellationToken = default);
}


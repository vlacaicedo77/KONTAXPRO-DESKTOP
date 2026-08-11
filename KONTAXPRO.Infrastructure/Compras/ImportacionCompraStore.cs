using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class ImportacionCompraStore(TimeProvider timeProvider)
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);
    private readonly object _gate = new();
    private readonly Dictionary<Guid, ImportacionCompraTemporal> _items = [];

    public Guid Add(
        long usuarioId,
        long empresaId,
        FacturaCompraXmlDto factura,
        byte[] contenido)
    {
        var protectedContent = contenido.ToArray();
        var id = Guid.NewGuid();
        lock (_gate)
        {
            PurgeExpiredCore();
            foreach (var previous in _items
                         .Where(x => x.Value.UsuarioId == usuarioId &&
                                     x.Value.EmpresaId == empresaId)
                         .Select(x => x.Key)
                         .ToList())
                _items.Remove(previous);
            _items[id] = new ImportacionCompraTemporal
            {
                Id = id,
                UsuarioId = usuarioId,
                EmpresaId = empresaId,
                Factura = factura,
                Contenido = protectedContent,
                ExpiraAt = timeProvider.GetUtcNow().Add(Vigencia)
            };
        }
        return id;
    }

    internal bool TryGet(
        Guid id,
        long usuarioId,
        long empresaId,
        out ImportacionCompraTemporal? item)
    {
        lock (_gate)
        {
            PurgeExpiredCore();
            if (_items.TryGetValue(id, out var found) &&
                found.UsuarioId == usuarioId && found.EmpresaId == empresaId)
            {
                item = found;
                return true;
            }
            item = null;
            return false;
        }
    }

    internal void Remove(Guid id)
    {
        lock (_gate) _items.Remove(id);
    }

    private void PurgeExpiredCore()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var expired in _items
                     .Where(x => x.Value.ExpiraAt <= now)
                     .Select(x => x.Key)
                     .ToList())
            _items.Remove(expired);
    }
}

internal sealed class ImportacionCompraTemporal
{
    public Guid Id { get; init; }
    public long UsuarioId { get; init; }
    public long EmpresaId { get; init; }
    public required FacturaCompraXmlDto Factura { get; init; }
    public required byte[] Contenido { get; init; }
    public DateTimeOffset ExpiraAt { get; init; }
}

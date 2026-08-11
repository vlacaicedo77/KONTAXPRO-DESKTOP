using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Infrastructure.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class ImportacionCompraStoreTests
{
    [Fact]
    public void Add_ReemplazaImportacionAnteriorDelMismoUsuarioYEmpresa()
    {
        var store = new ImportacionCompraStore(TimeProvider.System);
        var first = store.Add(3, 7, CreateInvoice(), [1, 2, 3]);
        var second = store.Add(3, 7, CreateInvoice(), [4, 5, 6]);

        Assert.False(store.TryGet(first, 3, 7, out _));
        Assert.True(store.TryGet(second, 3, 7, out var current));
        Assert.Equal(new byte[] { 4, 5, 6 }, current!.Contenido);
    }

    [Fact]
    public void Add_ConservaImportacionesDeOtraSesion()
    {
        var store = new ImportacionCompraStore(TimeProvider.System);
        var firstUser = store.Add(3, 7, CreateInvoice(), [1]);
        var secondUser = store.Add(4, 7, CreateInvoice(), [2]);
        var otherCompany = store.Add(3, 8, CreateInvoice(), [3]);

        Assert.True(store.TryGet(firstUser, 3, 7, out _));
        Assert.True(store.TryGet(secondUser, 4, 7, out _));
        Assert.True(store.TryGet(otherCompany, 3, 8, out _));
    }

    [Fact]
    public void Add_ProtegeContenidoContraCambiosDelBufferOriginal()
    {
        var source = new byte[] { 1, 2, 3 };
        var store = new ImportacionCompraStore(TimeProvider.System);
        var id = store.Add(3, 7, CreateInvoice(), source);

        source[0] = 9;

        Assert.True(store.TryGet(id, 3, 7, out var current));
        Assert.Equal(new byte[] { 1, 2, 3 }, current!.Contenido);
    }

    [Fact]
    public void TryGet_DescartaImportacionExpirada()
    {
        var clock = new AdjustableTimeProvider(
            new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var store = new ImportacionCompraStore(clock);
        var id = store.Add(3, 7, CreateInvoice(), [1]);

        clock.Advance(TimeSpan.FromMinutes(31));

        Assert.False(store.TryGet(id, 3, 7, out _));
    }

    private static FacturaCompraXmlDto CreateInvoice() => new();

    private sealed class AdjustableTimeProvider(DateTimeOffset now)
        : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}

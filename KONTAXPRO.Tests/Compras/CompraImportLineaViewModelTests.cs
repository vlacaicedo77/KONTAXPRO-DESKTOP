using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Desktop.ViewModels.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraImportLineaViewModelTests
{
    [Fact]
    public void Reconocida_QuedaResueltaSinAccionesDeCreacionOBusqueda()
    {
        using var line = Create(new LineaCompraResueltaDto
        {
            Estado = "RECONOCIDA",
            ProductoId = 10,
            ProductoPresentacionId = 20,
            ProductoNombre = "Producto",
            PresentacionNombre = "Unidad"
        });

        Assert.True(line.IsClassified);
        Assert.True(line.IsAutomaticallyResolved);
        Assert.Equal("AUTOMÁTICO", line.MatchStatus);
        Assert.False(line.ShowCreateAction);
        Assert.False(line.ShowSearchAction);
        Assert.True(line.ShowBonusOption);
    }

    [Fact]
    public void Sugerida_MuestraCoincidenciasYAlElegirQuedaRelacionada()
    {
        var candidate = Candidate();
        using var line = Create(new LineaCompraResueltaDto
        {
            Estado = "SUGERIDA",
            Candidatos = [candidate]
        });

        Assert.True(line.HasSuggestions);
        Assert.True(line.ShowCreateAction);
        Assert.True(line.ShowSearchAction);

        line.SelectedCandidate = candidate;

         Assert.True(line.IsClassified);
         Assert.Equal("RELACIONADO", line.MatchStatus);
         Assert.Equal("Producto · Marca · Unidad", line.ProductDisplay);
        Assert.True(line.ShowBonusOption);
        Assert.False(line.HasSuggestions);
        Assert.False(line.ShowCreateAction);
        Assert.True(line.ShowRememberEquivalence);
        Assert.True(line.RememberEquivalence);
        Assert.True(line.HandlesLots);
        Assert.Equal(10m, line.FactorConversion);
    }

    [Fact]
    public void Relacionada_AlLimpiarPermiteCrearOBuscarNuevamente()
    {
        var candidate = Candidate();
        using var line = Create(new LineaCompraResueltaDto
        {
            Estado = "SUGERIDA",
            Candidatos = [candidate]
        });
        line.SelectedCandidate = candidate;
        line.IsBonus = true;

        line.ClearRelationCommand.Execute(null);

        Assert.False(line.IsClassified);
        Assert.Null(line.ProductId);
        Assert.Null(line.PresentationId);
        Assert.Equal("Sin relacionar", line.ProductDisplay);
        Assert.Equal("NO RELACIONADO", line.MatchStatus);
        Assert.True(line.ShowCreateAction);
        Assert.True(line.ShowSearchAction);
        Assert.True(line.HasSuggestions);
        Assert.False(line.ShowClearRelationAction);
        Assert.False(line.ShowBonusOption);
        Assert.False(line.IsBonus);

        line.SelectedCandidate = candidate;

        Assert.True(line.ShowBonusOption);
        Assert.False(line.IsBonus);
    }

    [Fact]
    public void NoRelacionada_MarcadaNoInventariable_QuedaClasificada()
    {
        using var line = Create(null);

        line.SelectedCandidate = Candidate();
        line.IsBonus = true;
        line.SetNonInventoryCommand.Execute(true);

        Assert.True(line.IsNonInventory);
        Assert.True(line.IsClassified);
        Assert.Equal("GASTO", line.AccountingClassification);
        Assert.Equal(100, line.SelectedAccountingAccount?.Id);
        Assert.False(line.NeedsResolution);
        Assert.Equal("NO INVENTARIO", line.MatchStatus);
        Assert.True(line.ShowNonInventoryOption);
        Assert.False(line.ShowBonusOption);
        Assert.False(line.IsBonus);
    }

    [Fact]
    public void NoInventariable_SinCuentaContable_PermanecePendiente()
    {
        using var line = Create(null, includeAccountingAccount: false);

        line.SetNonInventoryCommand.Execute(true);

        Assert.True(line.IsNonInventory);
        Assert.False(line.IsClassified);
        Assert.True(line.NeedsResolution);
        Assert.False(line.ShowCreateAction);
    }

    [Fact]
    public async Task BusquedaManual_ConsultaDespuesDeDosCaracteres()
    {
        var searches = 0;
        var searchStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var line = Create(null, (search, _, _) =>
        {
            Interlocked.Increment(ref searches);
            searchStarted.TrySetResult();
            return Task.FromResult<IReadOnlyList<CandidatoProductoCompraDto>>(
                [Candidate()]);
        });

        line.OpenManualSearchCommand.Execute(null);
        line.ManualSearchText = "pr";
        await searchStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await WaitUntilAsync(() => line.HasManualResults,
            TimeSpan.FromSeconds(3));

        Assert.Equal(1, searches);
        Assert.True(line.HasManualResults);
        Assert.Single(line.ManualCandidates);
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!condition())
            await Task.Delay(10, cancellation.Token);
    }

    private static CompraImportLineaViewModel Create(
        LineaCompraResueltaDto? resolved,
        Func<string?, long?, CancellationToken,
            Task<IReadOnlyList<CandidatoProductoCompraDto>>>? search = null,
        bool includeAccountingAccount = true) =>
        new(new DetalleFacturaCompraXmlDto
        {
            Orden = 1,
            CodigoPrincipal = "ABC-1",
            Descripcion = "Producto de prueba",
            Cantidad = 1,
            PrecioUnitario = 10,
            PrecioTotalSinImpuesto = 10
        }, resolved, search ?? ((_, _, _) => Task.FromResult<
            IReadOnlyList<CandidatoProductoCompraDto>>([])),
        includeAccountingAccount
            ? [new CompraCuentaContableItemDto
              {
                  Id = 100,
                  Codigo = "5.1.01",
                  Nombre = "Gastos generales"
              }]
            : []);

    private static CandidatoProductoCompraDto Candidate() => new()
    {
        ProductoId = 10,
         ProductoPresentacionId = 20,
         ProductoNombre = "Producto",
         MarcaNombre = "Marca",
         PresentacionNombre = "Unidad",
        FactorConversion = 10,
        ManejaLotes = true,
        ManejaFechaCaducidad = true,
        Confianza = 0.82m,
        Motivo = "SIMILITUD_TEXTO"
    };
}

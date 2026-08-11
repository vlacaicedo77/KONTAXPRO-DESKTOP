using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraAccountingModelTests
{
    [Fact]
    public void AsientoCompra_DebeYHaberIguales_EstaBalanceado()
    {
        var entry = new Asiento
        {
            Detalles =
            [
                new AsientoDetalle { Debe = 100m, Haber = 0m },
                new AsientoDetalle { Debe = 15m, Haber = 0m },
                new AsientoDetalle { Debe = 0m, Haber = 115m }
            ]
        };

        Assert.True(entry.EstaBalanceado());
    }

    [Fact]
    public void AsientoCompra_ConDiferencia_NoEstaBalanceado()
    {
        var entry = new Asiento
        {
            Detalles =
            [
                new AsientoDetalle { Debe = 100m, Haber = 0m },
                new AsientoDetalle { Debe = 0m, Haber = 99.99m }
            ]
        };

        Assert.False(entry.EstaBalanceado());
    }

    [Fact]
    public void AsientoDetalle_ProtegeEmpresaCuentaYTercero()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);
        var entity = context.Model.FindEntityType(typeof(AsientoDetalle))!;

        AssertCompositeForeignKey(entity, "Asiento",
            [nameof(AsientoDetalle.AsientoId), nameof(AsientoDetalle.EmpresaId)],
            [nameof(Asiento.Id), nameof(Asiento.EmpresaId)]);
        AssertCompositeForeignKey(entity, "CuentaContable",
            [nameof(AsientoDetalle.CuentaContableId),
             nameof(AsientoDetalle.EmpresaId)],
            ["Id", "EmpresaId"]);
        AssertCompositeForeignKey(entity, "EmpresaTercero",
            [nameof(AsientoDetalle.EmpresaTerceroId),
             nameof(AsientoDetalle.EmpresaId)],
             ["Id", "EmpresaId"]);
    }

    [Fact]
    public void FechaReverso_UsaDiaCivilDeEcuador()
    {
        var processor = typeof(KontaxDbContext).Assembly.GetType(
            "KONTAXPRO.Infrastructure.Compras.CompraAccountingProcessor")!;
        var method = processor.GetMethod("EcuadorDate",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        var date = (DateOnly)method.Invoke(null,
            [new DateTime(2026, 8, 11, 2, 0, 0, DateTimeKind.Utc)])!;

        Assert.Equal(new DateOnly(2026, 8, 10), date);
    }

    [Fact]
    public void Compra_ComprobanteUnico_IgnoraComprasAnuladas()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);

        var index = context.Model.FindEntityType(typeof(Compra))!
            .GetIndexes().Single(x => x.GetDatabaseName() ==
                "ux_compras_empresa_proveedor_tipo_numero");

        Assert.True(index.IsUnique);
        Assert.Equal(
            "numero_documento IS NOT NULL AND estado <> 'ANULADA'",
            index.GetFilter());
    }

    [Fact]
    public void Compra_ConservaIdentificacionYRazonSocialHistoricas()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Compra))!;

        var identification = entity.FindProperty(
            nameof(Compra.ProveedorIdentificacion))!;
        var businessName = entity.FindProperty(
            nameof(Compra.ProveedorRazonSocial))!;

        Assert.False(identification.IsNullable);
        Assert.Equal(20, identification.GetMaxLength());
        Assert.False(businessName.IsNullable);
        Assert.Equal(256, businessName.GetMaxLength());
    }

    [Fact]
    public void CompraSustituta_EnlazaUnaSolaVersionYSinCascada()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Compra))!;
        var index = entity.GetIndexes().Single(x => x.GetDatabaseName() ==
            "ux_compras_compra_sustituida");
        var foreignKey = entity.GetForeignKeys().Single(x =>
            x.Properties.Select(p => p.Name).SequenceEqual(
                [nameof(Compra.CompraSustituidaId), nameof(Compra.EmpresaId)]));

        Assert.True(index.IsUnique);
        Assert.Equal("compra_sustituida_id IS NOT NULL", index.GetFilter());
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(
            [nameof(Compra.Id), nameof(Compra.EmpresaId)],
            foreignKey.PrincipalKey.Properties.Select(p => p.Name));
    }

    [Fact]
    public void PagosDeclaradosXml_PertenecenAlDocumentoYNoALaCompra()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);
        var payment = context.Model.FindEntityType(
            typeof(DocumentoRecibidoSriPago))!;
        var foreignKey = payment.GetForeignKeys().Single();

        Assert.Equal("documentos_recibidos_sri_pagos",
            payment.GetTableName());
        Assert.Equal(typeof(DocumentoRecibidoSri),
            foreignKey.PrincipalEntityType.ClrType);
        Assert.DoesNotContain(context.Model.FindEntityType(typeof(Compra))!
            .GetNavigations(), x => x.Name == "Pagos");
    }

    [Fact]
    public void CompraDetalle_ProtegeEmpresaProductoPresentacionYCuenta()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);
        var entity = context.Model.FindEntityType(typeof(CompraDetalle))!;

        AssertCompositeForeignKey(entity, "Compra",
            [nameof(CompraDetalle.CompraId), nameof(CompraDetalle.EmpresaId)],
            [nameof(Compra.Id), nameof(Compra.EmpresaId)]);
        AssertCompositeForeignKey(entity, "CuentaContable",
            [nameof(CompraDetalle.CuentaContableId), nameof(CompraDetalle.EmpresaId)],
            ["Id", "EmpresaId"]);
        AssertCompositeForeignKey(entity, "Producto",
            [nameof(CompraDetalle.ProductoId), nameof(CompraDetalle.EmpresaId)],
            ["Id", "EmpresaId"]);
        AssertCompositeForeignKey(entity, "ProductoPresentacion",
            [nameof(CompraDetalle.ProductoPresentacionId),
             nameof(CompraDetalle.ProductoId), nameof(CompraDetalle.EmpresaId)],
            ["Id", "ProductoId", "EmpresaId"]);
    }

    [Fact]
    public void RecepcionDetalle_ConservaMismoAislamientoMultiempresa()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);
        var entity = context.Model.FindEntityType(
            typeof(CompraRecepcionDetalle))!;

        AssertCompositeForeignKey(entity, "CompraRecepcion",
            [nameof(CompraRecepcionDetalle.CompraRecepcionId),
             nameof(CompraRecepcionDetalle.CompraId),
             nameof(CompraRecepcionDetalle.BodegaId),
             nameof(CompraRecepcionDetalle.EmpresaId)],
            [nameof(CompraRecepcion.Id), nameof(CompraRecepcion.CompraId),
             nameof(CompraRecepcion.BodegaId),
             nameof(CompraRecepcion.EmpresaId)]);
        AssertCompositeForeignKey(entity, "CompraDetalle",
            [nameof(CompraRecepcionDetalle.CompraDetalleId),
             nameof(CompraRecepcionDetalle.CompraId),
             nameof(CompraRecepcionDetalle.EmpresaId)],
            [nameof(CompraDetalle.Id), nameof(CompraDetalle.CompraId),
             nameof(CompraDetalle.EmpresaId)]);
        AssertCompositeForeignKey(entity, "ProductoPresentacion",
            [nameof(CompraRecepcionDetalle.ProductoPresentacionId),
             nameof(CompraRecepcionDetalle.ProductoId),
             nameof(CompraRecepcionDetalle.EmpresaId)],
            ["Id", "ProductoId", "EmpresaId"]);
    }

    [Fact]
    public void RecepcionLotesYSeries_PertenecenAlProductoYBodegaDeLaLinea()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new KontaxDbContext(options);
        var lot = context.Model.FindEntityType(
            typeof(CompraRecepcionDetalleLote))!;
        var serial = context.Model.FindEntityType(
            typeof(CompraRecepcionDetalleSerie))!;
        var productSerial = context.Model.FindEntityType(typeof(ProductoSerie))!;

        AssertCompositeForeignKey(lot, "CompraRecepcionDetalle",
            [nameof(CompraRecepcionDetalleLote.CompraRecepcionDetalleId),
             nameof(CompraRecepcionDetalleLote.ProductoId),
             nameof(CompraRecepcionDetalleLote.EmpresaId)],
            [nameof(CompraRecepcionDetalle.Id),
             nameof(CompraRecepcionDetalle.ProductoId),
             nameof(CompraRecepcionDetalle.EmpresaId)]);
        AssertCompositeForeignKey(lot, "ProductoLote",
            [nameof(CompraRecepcionDetalleLote.ProductoLoteId),
             nameof(CompraRecepcionDetalleLote.ProductoId)],
            [nameof(ProductoLote.Id), nameof(ProductoLote.ProductoId)]);
        AssertCompositeForeignKey(serial, "CompraRecepcionDetalle",
            [nameof(CompraRecepcionDetalleSerie.CompraRecepcionDetalleId),
             nameof(CompraRecepcionDetalleSerie.ProductoId),
             nameof(CompraRecepcionDetalleSerie.BodegaId),
             nameof(CompraRecepcionDetalleSerie.EmpresaId)],
            [nameof(CompraRecepcionDetalle.Id),
             nameof(CompraRecepcionDetalle.ProductoId),
             nameof(CompraRecepcionDetalle.BodegaId),
             nameof(CompraRecepcionDetalle.EmpresaId)]);
        AssertCompositeForeignKey(serial, "ProductoSerie",
            [nameof(CompraRecepcionDetalleSerie.ProductoSerieId),
             nameof(CompraRecepcionDetalleSerie.ProductoId),
             nameof(CompraRecepcionDetalleSerie.BodegaId)],
            [nameof(ProductoSerie.Id), nameof(ProductoSerie.ProductoId),
             nameof(ProductoSerie.BodegaId)]);
        AssertCompositeForeignKey(productSerial, "ProductoLote",
            [nameof(ProductoSerie.ProductoLoteId),
             nameof(ProductoSerie.ProductoId)],
            [nameof(ProductoLote.Id), nameof(ProductoLote.ProductoId)]);
    }

    private static void AssertCompositeForeignKey(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entity,
        string navigation,
        string[] foreignKeyProperties,
        string[] principalKeyProperties)
    {
        var foreignKey = entity.GetForeignKeys().Single(x =>
            x.DependentToPrincipal?.Name == navigation);

        Assert.Equal(foreignKeyProperties,
            foreignKey.Properties.Select(x => x.Name));
        Assert.Equal(principalKeyProperties,
            foreignKey.PrincipalKey.Properties.Select(x => x.Name));
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}

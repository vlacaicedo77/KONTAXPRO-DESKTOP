using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Persistence;

public sealed class StructuralSeeder
{
    // AVISO DE RELEASE:
    // Los códigos SRI incluidos son datos de referencia estructural.
    // Deben contrastarse con las fichas técnicas y catálogos vigentes del SRI
    // antes de cada release de producción. Tarifas y conceptos de retención,
    // por tener vigencia temporal, no se inventan ni se fijan aquí.
    private const string VerificarSriAntesDeProduccion =
        "VERIFICAR CONTRA CATALOGO SRI VIGENTE ANTES DE RELEASE PRODUCCION.";

    private readonly IDbContextFactory<KontaxDbContext> _dbContextFactory;

    public StructuralSeeder(
        IDbContextFactory<KontaxDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;

        await SeedTiposIdentificacionAsync(context, now, cancellationToken);
        await SeedConsumidorFinalAsync(context, now, cancellationToken);
        await SeedRegimenesAsync(context, now, cancellationToken);
        await SeedTiposComprobanteAsync(context, now, cancellationToken);
        await SeedUnidadesMedidaAsync(context, now, cancellationToken);
        await SeedImpuestosAsync(context, now, cancellationToken);
        await SeedFormasYMediosPagoAsync(context, now, cancellationToken);
        await SeedNumericosAsync(
            context,
            context.TiposAmbiente,
            [(1, "PRUEBAS"), (2, "PRODUCCION")],
            now,
            cancellationToken);
        await SeedNumericosAsync(
            context,
            context.TiposEmision,
            [(1, "NORMAL")],
            now,
            cancellationToken);

        await SeedBasicosAsync(
            context,
            context.EstadosSerie,
            ["DISPONIBLE", "RESERVADA", "VENDIDA", "DEVUELTA", "BAJA"],
            now,
            cancellationToken);

        await SeedDocumentosInternosAsync(context, now, cancellationToken);

        await SeedConNaturalezaAsync(
            context,
            context.TiposMovimientoInventario,
            [
                ("COMPRA", "ENTRADA"),
                ("VENTA", "SALIDA"),
                ("DEVOLUCION_COMPRA", "SALIDA"),
                ("DEVOLUCION_VENTA", "ENTRADA"),
                ("TRANSFERENCIA_SALIDA", "SALIDA"),
                ("TRANSFERENCIA_ENTRADA", "ENTRADA"),
                ("AJUSTE_ENTRADA", "ENTRADA"),
                ("AJUSTE_SALIDA", "SALIDA"),
                ("INVENTARIO_INICIAL", "ENTRADA")
            ],
            now,
            cancellationToken);

        await SeedBasicosAsync(
            context,
            context.TiposOrigenMovimientoInventario,
            [
                "COMPRA", "LIQUIDACION_COMPRA", "FACTURA", "NOTA_ENTREGA",
                "VENTA_XF", "DEVOLUCION_COMPRA", "DEVOLUCION_VENTA",
                "TRANSFERENCIA", "AJUSTE", "INVENTARIO_INICIAL"
            ],
            now,
            cancellationToken);

        await SeedConNaturalezaAsync(
            context,
            context.TiposMovimientoCartera,
            [
                ("ORIGEN_CREDITO", "DEBITO"),
                ("COBRO", "CREDITO"),
                ("REVERSO_COBRO", "DEBITO"),
                ("RETENCION_RECIBIDA", "CREDITO"),
                ("NOTA_CREDITO_CLIENTE", "CREDITO"),
                ("NOTA_DEBITO_CLIENTE", "DEBITO")
            ],
            now,
            cancellationToken);

        await SeedConNaturalezaAsync(
            context,
            context.TiposMovimientoCuentasPorPagar,
            [
                ("ORIGEN_DEUDA", "DEBITO"),
                ("PAGO", "CREDITO"),
                ("REVERSO_PAGO", "DEBITO"),
                ("NOTA_CREDITO_PROVEEDOR", "CREDITO"),
                ("RETENCION_EMITIDA", "CREDITO")
            ],
            now,
            cancellationToken);

        await SeedBasicosAsync(
            context,
            context.TiposConfiguracionContable,
            [
                "CUENTAS_POR_COBRAR", "CUENTAS_POR_PAGAR", "INVENTARIO",
                "COSTO_VENTAS", "VENTAS", "IVA_CREDITO_TRIBUTARIO",
                "IVA_POR_PAGAR", "RETENCIONES_RENTA_POR_PAGAR",
                "RETENCIONES_IVA_POR_PAGAR",
                "RETENCIONES_RENTA_POR_COBRAR",
                "RETENCIONES_IVA_POR_COBRAR"
            ],
            now,
            cancellationToken);

        await SeedBasicosAsync(
            context,
            context.TiposOrigenAsiento,
            [
                "FACTURA", "NOTA_ENTREGA", "VENTA_XF", "COMPRA",
                "LIQUIDACION_COMPRA", "COBRO", "PAGO", "DEVOLUCION_VENTA",
                "DEVOLUCION_COMPRA", "AJUSTE_COMPRA", "AJUSTE_INVENTARIO",
                "NOTA_CREDITO", "NOTA_DEBITO", "RETENCION_EMITIDA",
                "RETENCION_RECIBIDA", "DEPOSITO_CAJA_BANCO",
                "TRANSFERENCIA_BANCARIA", "MOVIMIENTO_INVENTARIO",
                "AJUSTE_MANUAL"
            ],
            now,
            cancellationToken);

        await SeedConNaturalezaAsync(
            context,
            context.TiposMovimientoCaja,
            [
                ("VENTA", "ENTRADA"),
                ("COBRO", "ENTRADA"),
                ("INGRESO", "ENTRADA"),
                ("PAGO", "SALIDA"),
                ("EGRESO", "SALIDA"),
                ("RETIRO", "SALIDA"),
                ("DEPOSITO_BANCO", "SALIDA"),
                ("REVERSO_INGRESO", "SALIDA"),
                ("REVERSO_EGRESO", "ENTRADA")
            ],
            now,
            cancellationToken);

        await SeedConNaturalezaAsync(
            context,
            context.TiposMovimientoBancario,
            [
                ("DEPOSITO", "ENTRADA"),
                ("RETIRO", "SALIDA"),
                ("TRANSFERENCIA_ENTRADA", "ENTRADA"),
                ("TRANSFERENCIA_SALIDA", "SALIDA"),
                ("COBRO", "ENTRADA"),
                ("PAGO", "SALIDA"),
                ("COMISION_BANCARIA", "SALIDA"),
                ("INTERES_GANADO", "ENTRADA"),
                ("INTERES_PAGADO", "SALIDA"),
                ("REVERSO_ENTRADA", "SALIDA"),
                ("REVERSO_SALIDA", "ENTRADA")
            ],
            now,
            cancellationToken);

        await SeedBasicosAsync(
            context,
            context.TiposOrigenGuiaRemision,
            [
                "FACTURA", "TRANSFERENCIA_INTERNA", "DEVOLUCION_COMPRA",
                "DEVOLUCION_VENTA", "OTRO"
            ],
            now,
            cancellationToken);
        await SeedBasicosAsync(
            context,
            context.TiposOrigenDevolucionVenta,
            ["FACTURA", "NOTA_ENTREGA", "VENTA_XF"],
            now,
            cancellationToken);
        await SeedBasicosAsync(
            context,
            context.TiposOrigenDevolucionCompra,
            ["COMPRA", "LIQUIDACION_COMPRA"],
            now,
            cancellationToken);
        await SeedBasicosAsync(
            context,
            context.TiposOrigenRetencionEmitida,
            ["COMPRA", "LIQUIDACION_COMPRA"],
            now,
            cancellationToken);
        await SeedBasicosAsync(
            context,
            context.TiposOrigenComprobanteElectronico,
            [
                "FACTURA", "LIQUIDACION_COMPRA", "NOTA_CREDITO",
                "NOTA_DEBITO", "GUIA_REMISION", "RETENCION"
            ],
            now,
            cancellationToken);
        await SeedBasicosAsync(
            context,
            context.EstadosComprobanteElectronico,
            [
                "PENDIENTE", "PROCESANDO", "GENERADO", "FIRMADO", "ENVIADO",
                "RECIBIDO", "AUTORIZADO", "NO_AUTORIZADO", "ERROR"
            ],
            now,
            cancellationToken);

        await SeedSeguridadAsync(context, now, cancellationToken);

        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(x => x.State == EntityState.Modified))
        {
            var updatedAt = entry.Metadata.FindProperty("UpdatedAt");
            if (updatedAt is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task SeedTiposIdentificacionAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        (string Sri, string Codigo, string Nombre, int Min, int Max)[] seeds =
        [
            ("04", "RUC", "RUC", 13, 13),
            ("05", "CEDULA", "CEDULA", 10, 10),
            ("06", "PASAPORTE", "PASAPORTE", 3, 20),
            ("07", "CONSUMIDOR_FINAL", "CONSUMIDOR_FINAL", 13, 13),
            ("08", "EXTERIOR", "EXTERIOR", 3, 20)
        ];
        var existentes = await context.TiposIdentificacion
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new TipoIdentificacion
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.TiposIdentificacion.Add(entity);
            }

            entity.CodigoSri = seed.Sri;
            entity.Nombre = seed.Nombre;
            entity.LongitudMinima = seed.Min;
            entity.LongitudMaxima = seed.Max;
            entity.Estado = 1;
        }
    }

    private static async Task SeedConsumidorFinalAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tipoIdentificacion = context.TiposIdentificacion.Local
            .FirstOrDefault(x => x.Codigo == "CONSUMIDOR_FINAL")
            ?? await context.TiposIdentificacion.FirstAsync(
                x => x.Codigo == "CONSUMIDOR_FINAL",
                cancellationToken);

        var tercero = await context.Terceros.FirstOrDefaultAsync(
            x => x.NumeroIdentificacion ==
                TerceroEstructural.ConsumidorFinalIdentificacion,
            cancellationToken);

        if (tercero is null)
        {
            tercero = new Tercero
            {
                NumeroIdentificacion =
                    TerceroEstructural.ConsumidorFinalIdentificacion,
                CreatedAt = now
            };
            context.Terceros.Add(tercero);
        }

        tercero.TipoIdentificacion = tipoIdentificacion;
        tercero.RazonSocial =
            TerceroEstructural.ConsumidorFinalRazonSocial;
        tercero.NombreComercial =
            TerceroEstructural.ConsumidorFinalRazonSocial;
        tercero.OrigenRegistro = "OFICIAL";
        tercero.EstadoVerificacion = "VERIFICADO";
        tercero.FuenteVerificacion = "SRI";
        tercero.VerificadoAt ??= now;
        tercero.Estado = 1;
    }

    private static async Task SeedSeguridadAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        string[] rolesSeeds =
            ["ADMINISTRADOR", "GUARDALMACEN", "VENDEDOR"];
        (string Codigo, string Modulo)[] permisosSeeds =
        [
            ("VENTAS_VER_COSTO", "VENTAS"),
            ("VENTAS_VER_UTILIDAD", "VENTAS"),
            ("VENTAS_CAMBIAR_PRECIO", "VENTAS"),
            ("VENTAS_APLICAR_DESCUENTO", "VENTAS"),
            ("VENTAS_VENDER_BAJO_COSTO", "VENTAS"),
            ("INVENTARIO_VER_COSTO", "INVENTARIO"),
            ("CARTERA_ANULAR_COBRO", "CARTERA"),
            ("CONTABILIDAD_REABRIR_PERIODO", "CONTABILIDAD"),
            ("CONTABILIDAD_CREAR_ASIENTO_MANUAL", "CONTABILIDAD"),
            ("CONTABILIDAD_ANULAR_ASIENTO", "CONTABILIDAD"),
            ("VENTAS_FACTURAR_A_TERCERO_DISTINTO", "VENTAS")
        ];

        var roles = await context.Roles
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);
        foreach (var codigo in rolesSeeds)
        {
            if (!roles.TryGetValue(codigo, out var rol))
            {
                rol = new Rol
                {
                    Codigo = codigo,
                    CreatedAt = now
                };
                context.Roles.Add(rol);
                roles.Add(codigo, rol);
            }

            rol.Nombre = codigo;
            rol.EsSistema = true;
            rol.Estado = 1;
        }

        var permisos = await context.Permisos
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);
        foreach (var seed in permisosSeeds)
        {
            if (!permisos.TryGetValue(seed.Codigo, out var permiso))
            {
                permiso = new Permiso
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.Permisos.Add(permiso);
                permisos.Add(seed.Codigo, permiso);
            }

            permiso.Nombre = seed.Codigo;
            permiso.Modulo = seed.Modulo;
            permiso.Estado = 1;
        }

        var asignacionesExistentes = await context.RolesPermisos
            .Select(x => new
            {
                Rol = x.Rol!.Codigo,
                Permiso = x.Permiso!.Codigo
            })
            .ToListAsync(cancellationToken);
        var paresExistentes = asignacionesExistentes
            .Select(x => $"{x.Rol}|{x.Permiso}")
            .ToHashSet(StringComparer.Ordinal);

        var asignaciones = permisosSeeds
            .Select(x => ("ADMINISTRADOR", x.Codigo))
            .Append(("GUARDALMACEN", "INVENTARIO_VER_COSTO"));

        foreach (var (codigoRol, codigoPermiso) in asignaciones)
        {
            var permiso = permisos[codigoPermiso];
            var par = $"{codigoRol}|{codigoPermiso}";

            if (paresExistentes.Contains(par))
            {
                continue;
            }

            context.RolesPermisos.Add(
                new RolPermiso
                {
                    Rol = roles[codigoRol],
                    Permiso = permiso,
                    CreatedAt = now
                });
            paresExistentes.Add(par);
        }
    }

    private static async Task SeedRegimenesAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        string[] seeds = ["GENERAL", "RIMPE", "RIMPE_NEGOCIO_POPULAR"];
        var existentes = await context.RegimenesTributarios
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var codigo in seeds)
        {
            if (!existentes.TryGetValue(codigo, out var entity))
            {
                entity = new RegimenTributario
                {
                    Codigo = codigo,
                    CreatedAt = now
                };
                context.RegimenesTributarios.Add(entity);
            }

            entity.Nombre = codigo;
            entity.Estado = 1;
        }
    }

    private static async Task SeedTiposComprobanteAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        (string Sri, string Codigo)[] seeds =
        [
            ("01", "FACTURA"),
            ("03", "LIQUIDACION_COMPRA"),
            ("04", "NOTA_CREDITO"),
            ("05", "NOTA_DEBITO"),
            ("06", "GUIA_REMISION"),
            ("07", "COMPROBANTE_RETENCION")
        ];
        var existentes = await context.TiposComprobante
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new TipoComprobante
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.TiposComprobante.Add(entity);
            }

            entity.CodigoSri = seed.Sri;
            entity.Nombre = seed.Codigo;
            entity.Descripcion = VerificarSriAntesDeProduccion;
            entity.Estado = 1;
        }
    }

    private static async Task SeedUnidadesMedidaAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        (string Codigo, string Nombre, string Abreviatura)[] seeds =
        [
            ("UNIDAD", "Unidad", "u")
        ];
        var existentes = await context.UnidadesMedida
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new UnidadMedida
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.UnidadesMedida.Add(entity);
            }

            entity.Nombre = seed.Nombre;
            entity.Abreviatura = seed.Abreviatura;
            entity.Estado = 1;
        }
    }

    private static async Task SeedImpuestosAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        (string Sri, string Codigo, string Nombre)[] seeds =
        [
            ("2", "IVA", "Impuesto al Valor Agregado"),
            ("3", "ICE", "Impuesto a los Consumos Especiales"),
            ("5", "IRBPNR", "Impuesto Redimible a las Botellas Plásticas")
        ];
        var existentes = await context.Impuestos
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new Impuesto
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.Impuestos.Add(entity);
            }

            entity.CodigoSri = seed.Sri;
            entity.Nombre = seed.Nombre;
            entity.Descripcion = VerificarSriAntesDeProduccion;
            entity.Estado = 1;
        }

        // No sembrar TarifasImpuesto ni ConceptosRetencion sin una fuente
        // oficial versionada: porcentajes, códigos y vigencias pueden cambiar.
    }

    private static async Task SeedFormasYMediosPagoAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        (string Sri, string Codigo, string Nombre)[] formas =
        [
            ("01", "SIN_SISTEMA_FINANCIERO",
                "Sin utilización del sistema financiero"),
            ("15", "COMPENSACION_DEUDAS", "Compensación de deudas"),
            ("16", "TARJETA_DEBITO", "Tarjeta de débito"),
            ("17", "DINERO_ELECTRONICO", "Dinero electrónico"),
            ("18", "TARJETA_PREPAGO", "Tarjeta prepago"),
            ("19", "TARJETA_CREDITO", "Tarjeta de crédito"),
            ("20", "OTROS_SISTEMA_FINANCIERO",
                "Otros con utilización del sistema financiero"),
            ("21", "ENDOSO_TITULOS", "Endoso de títulos")
        ];
        var formasExistentes = await context.FormasPago
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var seed in formas)
        {
            if (!formasExistentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new FormaPago
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.FormasPago.Add(entity);
                formasExistentes.Add(seed.Codigo, entity);
            }

            entity.CodigoSri = seed.Sri;
            entity.Nombre = seed.Nombre;
            entity.Descripcion = VerificarSriAntesDeProduccion;
            entity.Estado = 1;
        }

        (string Codigo, string Nombre, string FormaCodigo)[] medios =
        [
            ("EFECTIVO", "Efectivo", "SIN_SISTEMA_FINANCIERO"),
            ("TRANSFERENCIA", "Transferencia bancaria",
                "OTROS_SISTEMA_FINANCIERO"),
            ("CHEQUE", "Cheque", "OTROS_SISTEMA_FINANCIERO"),
            ("TARJETA_DEBITO", "Tarjeta de débito", "TARJETA_DEBITO"),
            ("TARJETA_CREDITO", "Tarjeta de crédito", "TARJETA_CREDITO")
        ];
        var mediosExistentes = await context.MediosPago
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var seed in medios)
        {
            if (!mediosExistentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new MedioPago
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.MediosPago.Add(entity);
            }

            entity.Nombre = seed.Nombre;
            entity.FormaPagoSri = formasExistentes[seed.FormaCodigo];
            entity.Descripcion =
                $"Mapeo estructural. {VerificarSriAntesDeProduccion}";
            entity.Estado = 1;
        }
    }

    private static async Task SeedDocumentosInternosAsync(
        KontaxDbContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        (string Codigo, string Prefijo)[] seeds =
        [
            ("VENTA_XF", "XF"),
            ("NOTA_ENTREGA", "NE"),
            ("PROFORMA", "PRO"),
            ("MOVIMIENTO_INVENTARIO", "MOV"),
            ("COBRO", "COB"),
            ("PAGO", "PAG"),
            ("DEVOLUCION_COMPRA", "DC"),
            ("DEVOLUCION_VENTA", "DV"),
            ("DEPOSITO_CAJA_BANCO", "DEP"),
            ("TRANSFERENCIA_BANCARIA", "TB")
        ];
        var existentes = await context.TiposDocumentoInterno
            .ToDictionaryAsync(x => x.Codigo, cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new TipoDocumentoInterno
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                context.TiposDocumentoInterno.Add(entity);
            }

            entity.Nombre = seed.Codigo;
            entity.PrefijoDefault = seed.Prefijo;
            entity.Estado = 1;
        }
    }

    private static async Task SeedBasicosAsync<TEntity>(
        KontaxDbContext context,
        DbSet<TEntity> set,
        IReadOnlyCollection<string> seeds,
        DateTime now,
        CancellationToken cancellationToken)
        where TEntity : class, ICatalogoBasico, new()
    {
        var existentes = await set.ToDictionaryAsync(
            x => x.Codigo,
            cancellationToken);

        foreach (var codigo in seeds)
        {
            if (!existentes.TryGetValue(codigo, out var entity))
            {
                entity = new TEntity { Codigo = codigo, CreatedAt = now };
                set.Add(entity);
            }

            entity.Nombre = codigo;
            entity.Estado = 1;
        }
    }

    private static async Task SeedConNaturalezaAsync<TEntity>(
        KontaxDbContext context,
        DbSet<TEntity> set,
        IReadOnlyCollection<(string Codigo, string Naturaleza)> seeds,
        DateTime now,
        CancellationToken cancellationToken)
        where TEntity : class, ICatalogoConNaturaleza, new()
    {
        var existentes = await set.ToDictionaryAsync(
            x => x.Codigo,
            cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new TEntity
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                set.Add(entity);
            }

            entity.Nombre = seed.Codigo;
            entity.Naturaleza = seed.Naturaleza;
            entity.Estado = 1;
        }
    }

    private static async Task SeedNumericosAsync<TEntity>(
        KontaxDbContext context,
        DbSet<TEntity> set,
        IReadOnlyCollection<(int Codigo, string Nombre)> seeds,
        DateTime now,
        CancellationToken cancellationToken)
        where TEntity : class, ICatalogoNumerico, new()
    {
        var existentes = await set.ToDictionaryAsync(
            x => x.Codigo,
            cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existentes.TryGetValue(seed.Codigo, out var entity))
            {
                entity = new TEntity
                {
                    Codigo = seed.Codigo,
                    CreatedAt = now
                };
                set.Add(entity);
            }

            entity.Nombre = seed.Nombre;
            entity.Estado = 1;
        }
    }
}

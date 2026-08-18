using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Domain.Entities.Tesoreria;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Persistence;

public sealed class DemoSeeder
{
    public const string DemoUserIdentification = "1724853377";
    public const string DemoPassword = "Kontax123";

    private readonly IDbContextFactory<KontaxDbContext> _contextFactory;
    private readonly IPasswordHasher _passwordHasher;

    public DemoSeeder(
        IDbContextFactory<KontaxDbContext> contextFactory,
        IPasswordHasher passwordHasher)
    {
        _contextFactory = contextFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(
        string? environmentName,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(
                environmentName,
                "Development",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var regimenGeneral = await db.RegimenesTributarios
            .SingleAsync(x => x.Codigo == "GENERAL", cancellationToken);
        var rolAdministrador = await db.Roles
            .SingleAsync(x => x.Codigo == "ADMINISTRADOR", cancellationToken);
        var tiposAmbiente = await db.TiposAmbiente
            .Where(x => x.Estado == 1)
            .ToListAsync(cancellationToken);
        var ambientePruebas = tiposAmbiente.Single(x => x.Codigo == 1);
        var emisionNormal = await db.TiposEmision
            .SingleAsync(x => x.Codigo == 1, cancellationToken);
        var tiposComprobante = await db.TiposComprobante
            .Where(x => x.Estado == 1)
            .ToListAsync(cancellationToken);
        var tiposDocumentoInterno = await db.TiposDocumentoInterno
            .Where(x => x.Estado == 1)
            .ToListAsync(cancellationToken);
        var consumidorFinal = await db.Terceros
            .SingleAsync(x => x.NumeroIdentificacion == "9999999999999", cancellationToken);

        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(
                x => x.NumeroIdentificacion == DemoUserIdentification,
                cancellationToken);

        if (usuario is null)
        {
            usuario = new Usuario
            {
                NumeroIdentificacion = DemoUserIdentification,
                NombreCompleto = "CAICEDO YELA JUAN VLADIMIR",
                Correo = "demo@kontaxpro.local",
                PasswordHash = _passwordHasher.Hash(DemoPassword),
                RequiereCambioClave = false,
                Estado = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Usuarios.Add(usuario);
        }
        else
        {
            usuario.NombreCompleto = "CAICEDO YELA JUAN VLADIMIR";
            usuario.RequiereCambioClave = false;
            usuario.Estado = 1;
            usuario.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        var companies = new[]
        {
            new DemoCompany(
                "1724204787001",
                "ZAMBRANO PANTOJA LEIDY ALEXANDRA",
                "MULTISERVICIOS JR",
                false,
                "vlacaicedo77@gmail.com",
                "0989213593",
                [
                    new DemoEstablishment("001", "MATRIZ",
                        "MULTISERVICIOS JR",
                        "VIA LAS MERCEDES A LOS BANCOS S/N Y MARGEN DERECHO",
                        true),
                    new DemoEstablishment("002", "ESTABLECIMIENTO 002",
                        "AGROVETERINARIA JR",
                        "VIA LAS MERCEDES A LOS BANCOS S/N Y MARGEN DERECHO",
                        false)
                ]),
            new DemoCompany(
                "1716017940001",
                "REQUELME MORENO JOSE MIGUEL",
                "AGROVETERINARIA JR",
                false,
                "vlacaicedo77@gmail.com",
                "0989213593",
                [
                    new DemoEstablishment("001", "MATRIZ",
                        "AGROVETERINARIA JR",
                        "VIA A MAR DE LA TRANQUILIDAD S/N Y MARGEN IZQUIERDO",
                        true),
                    new DemoEstablishment("002", "ESTABLECIMIENTO 002",
                        "BALANCEADOS JR",
                        "VIA LAS MERCEDES A LOS BANCOS S/N Y MARGEN DERECHO",
                        false)
                ])
        };

        foreach (var definition in companies)
        {
            var empresa = await db.Empresas.SingleOrDefaultAsync(
                x => x.NumeroIdentificacion == definition.Identification,
                cancellationToken);

            if (empresa is null)
            {
                empresa = new Empresa
                {
                    RegimenTributarioId = regimenGeneral.Id,
                    NumeroIdentificacion = definition.Identification,
                    RazonSocial = definition.LegalName,
                    NombreComercial = definition.TradeName,
                    ObligadoContabilidad = definition.RequiredAccounting,
                    Correo = definition.Email,
                    Telefono = definition.Phone,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Empresas.Add(empresa);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                empresa.RegimenTributarioId = regimenGeneral.Id;
                empresa.RazonSocial = definition.LegalName;
                empresa.NombreComercial = definition.TradeName;
                empresa.ObligadoContabilidad = definition.RequiredAccounting;
                empresa.Correo = definition.Email;
                empresa.Telefono = definition.Phone;
                empresa.Estado = 1;
                empresa.UpdatedAt = now;
            }

            var primaryDefinition = definition.Establishments
                .Single(x => x.IsHeadOffice);
            var establecimiento = await db.Establecimientos.SingleOrDefaultAsync(
                x => x.EmpresaId == empresa.Id &&
                     x.Codigo == primaryDefinition.Code,
                cancellationToken);
            if (establecimiento is null)
            {
                establecimiento = new Establecimiento
                {
                    EmpresaId = empresa.Id,
                    Codigo = primaryDefinition.Code,
                    Prefijo = primaryDefinition.Code,
                    Estado = 1,
                    CreatedAt = now,
                };
                db.Establecimientos.Add(establecimiento);
            }
            establecimiento.Nombre = primaryDefinition.Name;
            establecimiento.NombreComercial = primaryDefinition.TradeName;
            establecimiento.Direccion = primaryDefinition.Address;
            establecimiento.EsMatriz = true;
            establecimiento.Estado = 1;
            establecimiento.UpdatedAt = now;

            var listasDefinidas = new[]
            {
                new { Codigo = "A", Nombre = "LISTA A - PRECIO NORMAL",
                    EsBase = true, Descuento = (decimal?)null, Orden = 1 },
                new { Codigo = "B", Nombre = "LISTA B - DISTRIBUIDOR",
                    EsBase = false, Descuento = (decimal?)5m, Orden = 2 },
                new { Codigo = "C", Nombre = "LISTA C - MAYORISTA",
                    EsBase = false, Descuento = (decimal?)10m, Orden = 3 }
            };
            ListaPrecio? listaBase = null;
            foreach (var definicionLista in listasDefinidas)
            {
                var lista = await db.ListasPrecio.SingleOrDefaultAsync(
                    x => x.EmpresaId == empresa.Id &&
                         (x.Codigo == definicionLista.Codigo ||
                          (definicionLista.EsBase && x.EsListaBase)),
                    cancellationToken);
                if (lista is null)
                {
                    lista = new ListaPrecio
                    {
                        EmpresaId = empresa.Id,
                        CreatedAt = now
                    };
                    db.ListasPrecio.Add(lista);
                }

                lista.Codigo = definicionLista.Codigo;
                lista.Nombre = definicionLista.Nombre;
                lista.EsListaBase = definicionLista.EsBase;
                lista.PorcentajeDescuentoPredeterminado =
                    definicionLista.Descuento;
                lista.Orden = definicionLista.Orden;
                lista.Estado = 1;
                lista.UpdatedAt = now;
                if (definicionLista.EsBase)
                    listaBase = lista;
            }

            var accountDefinitions = new[]
            {
                new { Code = "1.1.01", Name = "CAJA GENERAL",
                    Nature = "DEUDORA" },
                new { Code = "1.1.02", Name = "INVENTARIO DE MERCADERÍAS",
                    Nature = "DEUDORA" },
                new { Code = "1.1.03", Name = "IVA CRÉDITO TRIBUTARIO",
                    Nature = "DEUDORA" },
                new { Code = "1.2.01", Name = "ACTIVOS PARA OPERACIÓN",
                    Nature = "DEUDORA" },
                new { Code = "2.1.01", Name = "CUENTAS POR PAGAR PROVEEDORES",
                    Nature = "ACREEDORA" },
                new { Code = "5.1.01", Name = "GASTOS GENERALES",
                    Nature = "DEUDORA" },
                new { Code = "5.9.01", Name = "OTROS COSTOS Y GASTOS",
                    Nature = "DEUDORA" }
            };
            var demoAccounts = new Dictionary<string, PlanCuenta>();
            foreach (var definitionAccount in accountDefinitions)
            {
                var account = await db.PlanCuentas.SingleOrDefaultAsync(
                    x => x.EmpresaId == empresa.Id &&
                         x.Codigo == definitionAccount.Code,
                    cancellationToken);
                if (account is null)
                {
                    account = new PlanCuenta
                    {
                        EmpresaId = empresa.Id,
                        Codigo = definitionAccount.Code,
                        CreatedAt = now
                    };
                    db.PlanCuentas.Add(account);
                }
                account.Nombre = definitionAccount.Name;
                account.Naturaleza = definitionAccount.Nature;
                account.AceptaMovimientos = true;
                account.Estado = 1;
                account.UpdatedAt = now;
                demoAccounts[definitionAccount.Code] = account;
            }

            await db.SaveChangesAsync(cancellationToken);
            var cuentaCaja = demoAccounts["1.1.01"];
            var accountingConfigurations = new[]
            {
                new { Type = "INVENTARIO", Account = "1.1.02" },
                new { Type = "IVA_CREDITO_TRIBUTARIO", Account = "1.1.03" },
                new { Type = "CUENTAS_POR_PAGAR", Account = "2.1.01" },
                new { Type = "GASTOS_NO_DEDUCIBLES", Account = "5.9.01" }
            };
            foreach (var definitionConfiguration in accountingConfigurations)
            {
                var configurationType = await db.TiposConfiguracionContable
                    .SingleAsync(x => x.Codigo == definitionConfiguration.Type,
                        cancellationToken);
                var configuration = await db.ConfiguracionCuentas
                    .SingleOrDefaultAsync(x => x.EmpresaId == empresa.Id &&
                        x.TipoConfiguracionContableId == configurationType.Id,
                        cancellationToken);
                if (configuration is null)
                {
                    configuration = new ConfiguracionCuenta
                    {
                        EmpresaId = empresa.Id,
                        TipoConfiguracionContableId = configurationType.Id,
                        CreatedAt = now
                    };
                    db.ConfiguracionCuentas.Add(configuration);
                }
                configuration.CuentaContableId =
                    demoAccounts[definitionConfiguration.Account].Id;
                configuration.Estado = 1;
                configuration.UpdatedAt = now;
            }

            for (var month = 1; month <= 12; month++)
            {
                var period = await db.PeriodosContables.SingleOrDefaultAsync(
                    x => x.EmpresaId == empresa.Id && x.Anio == now.Year &&
                         x.Mes == month, cancellationToken);
                if (period is not null) continue;
                var firstDay = new DateOnly(now.Year, month, 1);
                db.PeriodosContables.Add(new PeriodoContable
                {
                    EmpresaId = empresa.Id,
                    Anio = now.Year,
                    Mes = month,
                    FechaInicio = firstDay,
                    FechaFin = firstDay.AddMonths(1).AddDays(-1),
                    Estado = "ABIERTO",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            var puntoEmision = await db.PuntosEmision.SingleOrDefaultAsync(
                x => x.EstablecimientoId == establecimiento.Id && x.Codigo == "001",
                cancellationToken);
            if (puntoEmision is null)
            {
                puntoEmision = new PuntoEmision
                {
                    EstablecimientoId = establecimiento.Id,
                    Codigo = "001",
                    Nombre = "PUNTO PRINCIPAL",
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.PuntosEmision.Add(puntoEmision);
            }

            var bodega = await db.Bodegas.SingleOrDefaultAsync(
                x => x.EstablecimientoId == establecimiento.Id && x.Codigo == "FAC",
                cancellationToken);
            if (bodega is null)
            {
                bodega = new Bodega
                {
                    EstablecimientoId = establecimiento.Id,
                    Codigo = "FAC",
                    Nombre = "PRODUCTOS CON FACTURA",
                    PermiteTransferenciasInternas = true,
                    PermiteVentaFacturada = true,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Bodegas.Add(bodega);
            }

            bodega.Nombre = "PRODUCTOS CON FACTURA";
            bodega.PermiteTransferenciasInternas = true;
            bodega.PermiteVentaFacturada = true;
            bodega.Estado = 1;
            bodega.UpdatedAt = now;

            var bodegaSinFactura = await db.Bodegas.SingleOrDefaultAsync(
                x => x.EstablecimientoId == establecimiento.Id && x.Codigo == "SFA",
                cancellationToken);
            if (bodegaSinFactura is null)
            {
                bodegaSinFactura = new Bodega
                {
                    EstablecimientoId = establecimiento.Id,
                    Codigo = "SFA",
                    CreatedAt = now
                };
                db.Bodegas.Add(bodegaSinFactura);
            }
            bodegaSinFactura.Nombre = "PRODUCTOS SIN FACTURA";
            bodegaSinFactura.PermiteTransferenciasInternas = true;
            bodegaSinFactura.PermiteVentaFacturada = false;
            bodegaSinFactura.Estado = 1;
            bodegaSinFactura.UpdatedAt = now;

            var caja = await db.Cajas.SingleOrDefaultAsync(
                x => x.EmpresaId == empresa.Id && x.Codigo == "PRINCIPAL",
                cancellationToken);
            if (caja is null)
            {
                caja = new Caja
                {
                    EmpresaId = empresa.Id,
                    EstablecimientoId = establecimiento.Id,
                    Codigo = "PRINCIPAL",
                    Nombre = "CAJA PRINCIPAL",
                    CuentaContableId = cuentaCaja.Id,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Cajas.Add(caja);
            }

            var configuracionElectronica = await db.FacturacionesElectronicas
                .SingleOrDefaultAsync(x => x.EmpresaId == empresa.Id, cancellationToken);
            if (configuracionElectronica is null)
            {
                configuracionElectronica =
                    new global::KONTAXPRO.Domain.Entities.Configuracion.FacturacionElectronica
                {
                    EmpresaId = empresa.Id,
                    TipoAmbienteId = ambientePruebas.Id,
                    TipoEmisionId = emisionNormal.Id,
                    Habilitada = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.FacturacionesElectronicas.Add(configuracionElectronica);
            }

            var terceroEmpresa = await db.EmpresasTerceros.SingleOrDefaultAsync(
                x => x.EmpresaId == empresa.Id && x.TerceroId == consumidorFinal.Id,
                cancellationToken);
            if (terceroEmpresa is null)
            {
                terceroEmpresa = new EmpresaTercero
                {
                    EmpresaId = empresa.Id,
                    TerceroId = consumidorFinal.Id,
                    ListaPrecioId = listaBase!.Id,
                    CreditoHabilitado = false,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.EmpresasTerceros.Add(terceroEmpresa);
            }

            await db.SaveChangesAsync(cancellationToken);

            foreach (var tipoComprobante in tiposComprobante)
            {
                foreach (var tipoAmbiente in tiposAmbiente)
                {
                    if (await db.SecuencialesComprobantes.AnyAsync(
                            x => x.PuntoEmisionId == puntoEmision.Id
                                 && x.TipoComprobanteId == tipoComprobante.Id
                                 && x.TipoAmbienteId == tipoAmbiente.Id,
                            cancellationToken))
                        continue;

                    db.SecuencialesComprobantes.Add(
                        new SecuencialComprobante
                    {
                        PuntoEmisionId = puntoEmision.Id,
                        TipoComprobanteId = tipoComprobante.Id,
                        TipoAmbienteId = tipoAmbiente.Id,
                        UltimoSecuencial = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            foreach (var tipoDocumento in tiposDocumentoInterno)
            {
                if (!await db.SecuencialesInternos.AnyAsync(
                        x => x.EmpresaId == empresa.Id
                             && x.EstablecimientoId == establecimiento.Id
                             && x.TipoDocumentoInternoId == tipoDocumento.Id,
                        cancellationToken))
                {
                    db.SecuencialesInternos.Add(new SecuencialInterno
                    {
                        EmpresaId = empresa.Id,
                        EstablecimientoId = establecimiento.Id,
                        TipoDocumentoInternoId = tipoDocumento.Id,
                        UltimoSecuencial = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            if (!await db.SecuencialesAsientos.AnyAsync(
                    x => x.EmpresaId == empresa.Id && x.Anio == now.Year,
                    cancellationToken))
            {
                db.SecuencialesAsientos.Add(new SecuencialAsiento
                {
                    EmpresaId = empresa.Id,
                    Anio = now.Year,
                    UltimoSecuencial = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            var usuarioEmpresa = await db.UsuariosEmpresas.SingleOrDefaultAsync(
                x => x.UsuarioId == usuario.Id && x.EmpresaId == empresa.Id,
                cancellationToken);
            if (usuarioEmpresa is null)
            {
                usuarioEmpresa = new UsuarioEmpresa
                {
                    UsuarioId = usuario.Id,
                    EmpresaId = empresa.Id,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.UsuariosEmpresas.Add(usuarioEmpresa);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                usuarioEmpresa.Estado = 1;
                usuarioEmpresa.UpdatedAt = now;
            }

            if (!await db.UsuariosEmpresasRoles.AnyAsync(
                    x => x.UsuarioEmpresaId == usuarioEmpresa.Id
                         && x.RolId == rolAdministrador.Id,
                    cancellationToken))
            {
                db.UsuariosEmpresasRoles.Add(new UsuarioEmpresaRol
                {
                    UsuarioEmpresaId = usuarioEmpresa.Id,
                    RolId = rolAdministrador.Id,
                    CreatedAt = now
                });
            }

            if (!await db.UsuariosEmpresasEstablecimientos.AnyAsync(
                    x => x.UsuarioEmpresaId == usuarioEmpresa.Id
                         && x.EstablecimientoId == establecimiento.Id,
                    cancellationToken))
            {
                db.UsuariosEmpresasEstablecimientos.Add(
                    new UsuarioEmpresaEstablecimiento
                    {
                        UsuarioEmpresaId = usuarioEmpresa.Id,
                        EstablecimientoId = establecimiento.Id,
                        CreatedAt = now
                    });
            }

            var preferencias = await db.UsuariosConfiguracionesEmpresa
                .SingleOrDefaultAsync(
                    x => x.UsuarioId == usuario.Id && x.EmpresaId == empresa.Id,
                    cancellationToken);
            if (preferencias is null)
            {
                db.UsuariosConfiguracionesEmpresa.Add(new UsuarioConfiguracionEmpresa
                {
                    UsuarioId = usuario.Id,
                    EmpresaId = empresa.Id,
                    EstablecimientoId = establecimiento.Id,
                    PuntoEmisionId = puntoEmision.Id,
                    BodegaId = bodega.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                preferencias.EstablecimientoId = establecimiento.Id;
                preferencias.PuntoEmisionId = puntoEmision.Id;
                preferencias.BodegaId = bodega.Id;
                preferencias.UpdatedAt = now;
            }

            await db.SaveChangesAsync(cancellationToken);

            foreach (var additional in definition.Establishments
                         .Where(x => !x.IsHeadOffice))
            {
                await SeedAdditionalEstablishmentAsync(
                    db,
                    empresa,
                    usuarioEmpresa,
                    cuentaCaja,
                    additional,
                    tiposComprobante,
                    tiposAmbiente,
                    tiposDocumentoInterno,
                    now,
                    cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task SeedAdditionalEstablishmentAsync(
        KontaxDbContext db,
        Empresa company,
        UsuarioEmpresa userCompany,
        PlanCuenta cashAccount,
        DemoEstablishment definition,
        IReadOnlyCollection<TipoComprobante> documentTypes,
        IReadOnlyCollection<TipoAmbiente> environmentTypes,
        IReadOnlyCollection<TipoDocumentoInterno> internalDocumentTypes,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var establishment = await db.Establecimientos.SingleOrDefaultAsync(
            x => x.EmpresaId == company.Id && x.Codigo == definition.Code,
            cancellationToken);
        if (establishment is null)
        {
            establishment = new Establecimiento
            {
                EmpresaId = company.Id,
                Codigo = definition.Code,
                Prefijo = definition.Code,
                CreatedAt = now
            };
            db.Establecimientos.Add(establishment);
        }

        establishment.Nombre = definition.Name;
        establishment.NombreComercial = definition.TradeName;
        establishment.Direccion = definition.Address;
        establishment.EsMatriz = false;
        establishment.Estado = 1;
        establishment.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        var issuePoint = await db.PuntosEmision.SingleOrDefaultAsync(
            x => x.EstablecimientoId == establishment.Id && x.Codigo == "001",
            cancellationToken);
        if (issuePoint is null)
        {
            issuePoint = new PuntoEmision
            {
                EstablecimientoId = establishment.Id,
                Codigo = "001",
                Nombre = "PUNTO PRINCIPAL",
                Estado = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.PuntosEmision.Add(issuePoint);
        }

        await EnsureWarehouseAsync(
            db, establishment.Id, "FAC", "PRODUCTOS CON FACTURA", true,
            now, cancellationToken);
        await EnsureWarehouseAsync(
            db, establishment.Id, "SFA", "PRODUCTOS SIN FACTURA", false,
            now, cancellationToken);

        var cashCode = $"EST{definition.Code}";
        var cash = await db.Cajas.SingleOrDefaultAsync(
            x => x.EmpresaId == company.Id && x.Codigo == cashCode,
            cancellationToken);
        if (cash is null)
        {
            cash = new Caja
            {
                EmpresaId = company.Id,
                Codigo = cashCode,
                CreatedAt = now
            };
            db.Cajas.Add(cash);
        }
        cash.EstablecimientoId = establishment.Id;
        cash.Nombre = $"CAJA {definition.TradeName}";
        cash.CuentaContableId = cashAccount.Id;
        cash.Estado = 1;
        cash.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        foreach (var documentType in documentTypes)
        foreach (var environmentType in environmentTypes)
        {
            if (await db.SecuencialesComprobantes.AnyAsync(x =>
                    x.PuntoEmisionId == issuePoint.Id &&
                    x.TipoComprobanteId == documentType.Id &&
                    x.TipoAmbienteId == environmentType.Id,
                    cancellationToken))
                continue;
            db.SecuencialesComprobantes.Add(new SecuencialComprobante
            {
                PuntoEmisionId = issuePoint.Id,
                TipoComprobanteId = documentType.Id,
                TipoAmbienteId = environmentType.Id,
                UltimoSecuencial = 0,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        foreach (var internalDocumentType in internalDocumentTypes)
        {
            if (await db.SecuencialesInternos.AnyAsync(x =>
                    x.EmpresaId == company.Id &&
                    x.EstablecimientoId == establishment.Id &&
                    x.TipoDocumentoInternoId == internalDocumentType.Id,
                    cancellationToken))
                continue;
            db.SecuencialesInternos.Add(new SecuencialInterno
            {
                EmpresaId = company.Id,
                EstablecimientoId = establishment.Id,
                TipoDocumentoInternoId = internalDocumentType.Id,
                UltimoSecuencial = 0,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (!await db.UsuariosEmpresasEstablecimientos.AnyAsync(x =>
                x.UsuarioEmpresaId == userCompany.Id &&
                x.EstablecimientoId == establishment.Id,
                cancellationToken))
        {
            db.UsuariosEmpresasEstablecimientos.Add(
                new UsuarioEmpresaEstablecimiento
                {
                    UsuarioEmpresaId = userCompany.Id,
                    EstablecimientoId = establishment.Id,
                    CreatedAt = now
                });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Bodega> EnsureWarehouseAsync(
        KontaxDbContext db,
        long establishmentId,
        string code,
        string name,
        bool allowsInvoicedSale,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var warehouse = await db.Bodegas.SingleOrDefaultAsync(
            x => x.EstablecimientoId == establishmentId && x.Codigo == code,
            cancellationToken);
        if (warehouse is null)
        {
            warehouse = new Bodega
            {
                EstablecimientoId = establishmentId,
                Codigo = code,
                CreatedAt = now
            };
            db.Bodegas.Add(warehouse);
        }
        warehouse.Nombre = name;
        warehouse.PermiteTransferenciasInternas = true;
        warehouse.PermiteVentaFacturada = allowsInvoicedSale;
        warehouse.Estado = 1;
        warehouse.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return warehouse;
    }

    private sealed record DemoCompany(
        string Identification,
        string LegalName,
        string TradeName,
        bool RequiredAccounting,
        string Email,
        string Phone,
        IReadOnlyCollection<DemoEstablishment> Establishments);

    private sealed record DemoEstablishment(
        string Code,
        string Name,
        string TradeName,
        string Address,
        bool IsHeadOffice);
}

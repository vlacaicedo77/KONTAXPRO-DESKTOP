using KONTAXPRO.Application.Interfaces;
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
    public const string DemoUserIdentification = "0999999999";
    public const string DemoPassword = "KontaxDemo2026!";

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
        var ambientePruebas = await db.TiposAmbiente
            .SingleAsync(x => x.Codigo == 1, cancellationToken);
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
                NombreCompleto = "USUARIO DEMO KONTAXPRO",
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
            usuario.Estado = 1;
            usuario.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        var companies = new[]
        {
            new DemoCompany(
                "1799999999001",
                "KONTAXPRO DEMO UNO S.A.S.",
                "KONTAXPRO DEMO UNO",
                "Quito - dirección exclusiva para desarrollo"),
            new DemoCompany(
                "1799999999002",
                "KONTAXPRO DEMO DOS S.A.S.",
                "KONTAXPRO DEMO DOS",
                "Guayaquil - dirección exclusiva para desarrollo")
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
                    ObligadoContabilidad = true,
                    Correo = "demo@kontaxpro.local",
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Empresas.Add(empresa);
                await db.SaveChangesAsync(cancellationToken);
            }

            var establecimiento = await db.Establecimientos.SingleOrDefaultAsync(
                x => x.EmpresaId == empresa.Id && x.Codigo == "001",
                cancellationToken);
            if (establecimiento is null)
            {
                establecimiento = new Establecimiento
                {
                    EmpresaId = empresa.Id,
                    Codigo = "001",
                    Prefijo = "001",
                    Nombre = "MATRIZ",
                    Direccion = definition.Address,
                    EsMatriz = true,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Establecimientos.Add(establecimiento);
            }

            var listaBase = await db.ListasPrecio.SingleOrDefaultAsync(
                x => x.EmpresaId == empresa.Id && x.Codigo == "BASE",
                cancellationToken);
            if (listaBase is null)
            {
                listaBase = new ListaPrecio
                {
                    EmpresaId = empresa.Id,
                    Codigo = "BASE",
                    Nombre = "LISTA BASE",
                    EsListaBase = true,
                    Orden = 1,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.ListasPrecio.Add(listaBase);
            }

            var cuentaCaja = await db.PlanCuentas.SingleOrDefaultAsync(
                x => x.EmpresaId == empresa.Id && x.Codigo == "1.1.01",
                cancellationToken);
            if (cuentaCaja is null)
            {
                cuentaCaja = new PlanCuenta
                {
                    EmpresaId = empresa.Id,
                    Codigo = "1.1.01",
                    Nombre = "CAJA GENERAL",
                    Naturaleza = "DEUDORA",
                    AceptaMovimientos = true,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.PlanCuentas.Add(cuentaCaja);
            }

            await db.SaveChangesAsync(cancellationToken);

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
                x => x.EstablecimientoId == establecimiento.Id && x.Codigo == "PRINCIPAL",
                cancellationToken);
            if (bodega is null)
            {
                bodega = new Bodega
                {
                    EstablecimientoId = establecimiento.Id,
                    Codigo = "PRINCIPAL",
                    Nombre = "BODEGA PRINCIPAL",
                    PermiteTransferenciasInternas = true,
                    PermiteVentaFacturada = true,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Bodegas.Add(bodega);
            }

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
                    EsCliente = true,
                    EsProveedor = false,
                    ListaPrecioId = listaBase.Id,
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
                if (!await db.SecuencialesComprobantes.AnyAsync(
                        x => x.PuntoEmisionId == puntoEmision.Id
                             && x.TipoComprobanteId == tipoComprobante.Id
                             && x.TipoAmbienteId == ambientePruebas.Id,
                        cancellationToken))
                {
                    db.SecuencialesComprobantes.Add(new SecuencialComprobante
                    {
                        PuntoEmisionId = puntoEmision.Id,
                        TipoComprobanteId = tipoComprobante.Id,
                        TipoAmbienteId = ambientePruebas.Id,
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
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private sealed record DemoCompany(
        string Identification,
        string LegalName,
        string TradeName,
        string Address);
}

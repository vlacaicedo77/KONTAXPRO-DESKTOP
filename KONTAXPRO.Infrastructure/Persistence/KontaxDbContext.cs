using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using Microsoft.EntityFrameworkCore;
using KONTAXPRO.Domain.Entities.Inventario;

namespace KONTAXPRO.Infrastructure.Persistence;

public class KontaxDbContext : DbContext
{
    public KontaxDbContext(DbContextOptions<KontaxDbContext> options)
        : base(options)
    {
    }

    public DbSet<TipoIdentificacion> TiposIdentificacion
        => Set<TipoIdentificacion>();

    public DbSet<RegimenTributario> RegimenesTributarios
        => Set<RegimenTributario>();

    public DbSet<Usuario> Usuarios
        => Set<Usuario>();

    public DbSet<Rol> Roles
        => Set<Rol>();

    public DbSet<Permiso> Permisos
        => Set<Permiso>();

    public DbSet<RolPermiso> RolesPermisos
        => Set<RolPermiso>();

    public DbSet<UsuarioEmpresa> UsuariosEmpresas
        => Set<UsuarioEmpresa>();

    public DbSet<Empresa> Empresas
        => Set<Empresa>();

    public DbSet<Establecimiento> Establecimientos
        => Set<Establecimiento>();

    public DbSet<PuntoEmision> PuntosEmision
        => Set<PuntoEmision>();

    public DbSet<UsuarioEmpresaRol> UsuariosEmpresasRoles
    => Set<UsuarioEmpresaRol>();

    public DbSet<UsuarioEmpresaEstablecimiento>
        UsuariosEmpresasEstablecimientos
        => Set<UsuarioEmpresaEstablecimiento>();

    public DbSet<Auditoria> Auditorias
        => Set<Auditoria>();

    public DbSet<UsuarioConfiguracionEmpresa>
        UsuariosConfiguracionesEmpresa
        => Set<UsuarioConfiguracionEmpresa>();

    public DbSet<SecuencialComprobante> SecuencialesComprobantes
        => Set<SecuencialComprobante>();

    public DbSet<SecuencialInterno> SecuencialesInternos
        => Set<SecuencialInterno>();

    public DbSet<FacturacionElectronica> FacturacionesElectronicas
        => Set<FacturacionElectronica>();

    public DbSet<TarifaImpuesto> TarifasImpuesto
    => Set<TarifaImpuesto>();

    public DbSet<Impuesto> Impuestos
        => Set<Impuesto>();

    public DbSet<UnidadMedida> UnidadesMedida
        => Set<UnidadMedida>();

    public DbSet<Marca> Marcas
        => Set<Marca>();

    public DbSet<CategoriaProducto> CategoriasProducto
        => Set<CategoriaProducto>();

    public DbSet<TipoComprobante> TiposComprobante
        => Set<TipoComprobante>();

    public DbSet<FormaPago> FormasPago
        => Set<FormaPago>();

    public DbSet<MedioPago> MediosPago
        => Set<MedioPago>();

    public DbSet<TipoAmbiente> TiposAmbiente
        => Set<TipoAmbiente>();

    public DbSet<TipoEmision> TiposEmision
        => Set<TipoEmision>();

    public DbSet<EstadoSerie> EstadosSerie
        => Set<EstadoSerie>();

    public DbSet<TipoDocumentoInterno> TiposDocumentoInterno
        => Set<TipoDocumentoInterno>();

    public DbSet<TipoMovimientoInventario> TiposMovimientoInventario
        => Set<TipoMovimientoInventario>();

    public DbSet<TipoOrigenMovimientoInventario>
        TiposOrigenMovimientoInventario
        => Set<TipoOrigenMovimientoInventario>();

    public DbSet<TipoMovimientoCartera> TiposMovimientoCartera
        => Set<TipoMovimientoCartera>();

    public DbSet<TipoMovimientoCuentaPorPagar>
        TiposMovimientoCuentasPorPagar
        => Set<TipoMovimientoCuentaPorPagar>();

    public DbSet<TipoConfiguracionContable> TiposConfiguracionContable
        => Set<TipoConfiguracionContable>();

    public DbSet<TipoOrigenAsiento> TiposOrigenAsiento
        => Set<TipoOrigenAsiento>();

    public DbSet<TipoMovimientoCaja> TiposMovimientoCaja
        => Set<TipoMovimientoCaja>();

    public DbSet<TipoMovimientoBancario> TiposMovimientoBancario
        => Set<TipoMovimientoBancario>();

    public DbSet<TipoOrigenGuiaRemision> TiposOrigenGuiaRemision
        => Set<TipoOrigenGuiaRemision>();

    public DbSet<TipoOrigenDevolucionVenta>
        TiposOrigenDevolucionVenta
        => Set<TipoOrigenDevolucionVenta>();

    public DbSet<TipoOrigenDevolucionCompra>
        TiposOrigenDevolucionCompra
        => Set<TipoOrigenDevolucionCompra>();

    public DbSet<TipoOrigenRetencionEmitida>
        TiposOrigenRetencionEmitida
        => Set<TipoOrigenRetencionEmitida>();

    public DbSet<TipoOrigenComprobanteElectronico>
        TiposOrigenComprobanteElectronico
        => Set<TipoOrigenComprobanteElectronico>();

    public DbSet<EstadoComprobanteElectronico>
        EstadosComprobanteElectronico
        => Set<EstadoComprobanteElectronico>();

    public DbSet<ConceptoRetencion> ConceptosRetencion
        => Set<ConceptoRetencion>();

    public DbSet<Tercero> Terceros
        => Set<Tercero>();

    public DbSet<EmpresaTercero> EmpresasTerceros
        => Set<EmpresaTercero>();

    public DbSet<ListaPrecio> ListasPrecio
        => Set<ListaPrecio>();

    public DbSet<ConfiguracionInventario> ConfiguracionesInventario
        => Set<ConfiguracionInventario>();

    public DbSet<Bodega> Bodegas
    => Set<Bodega>();

    public DbSet<Producto> Productos
    => Set<Producto>();

    public DbSet<ProductoPresentacion> ProductosPresentaciones
        => Set<ProductoPresentacion>();

    public DbSet<ProductoPresentacionPrecio> ProductosPresentacionesPrecios
        => Set<ProductoPresentacionPrecio>();

    public DbSet<ProductoExistencia> ProductosExistencias
    => Set<ProductoExistencia>();

    public DbSet<ProductoLote> ProductosLotes
        => Set<ProductoLote>();

    public DbSet<ProductoLoteExistencia> ProductosLotesExistencias
        => Set<ProductoLoteExistencia>();

    public DbSet<ProductoSerie> ProductosSeries
        => Set<ProductoSerie>();

    public DbSet<MovimientoInventario> MovimientosInventario
    => Set<MovimientoInventario>();

    public DbSet<MovimientoInventarioDetalle> MovimientosInventarioDetalles
        => Set<MovimientoInventarioDetalle>();

    public DbSet<ProductoCosto> ProductosCostos
        => Set<ProductoCosto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(KontaxDbContext).Assembly);
    }
}

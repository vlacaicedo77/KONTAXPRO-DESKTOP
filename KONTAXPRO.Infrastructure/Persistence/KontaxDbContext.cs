using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using Microsoft.EntityFrameworkCore;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Ventas;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Domain.Entities.Cartera;
using KONTAXPRO.Domain.Entities.Tesoreria;
using KONTAXPRO.Domain.Entities.Bancos;
using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Domain.Entities.Tributacion;
using KONTAXPRO.Domain.Entities.FacturacionElectronica;
using ConfiguracionFacturacionElectronica =
    KONTAXPRO.Domain.Entities.Configuracion.FacturacionElectronica;

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

    public DbSet<ConfiguracionFacturacionElectronica> FacturacionesElectronicas
        => Set<ConfiguracionFacturacionElectronica>();

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

    public DbSet<MotivoOperacionInventario> MotivosOperacionInventario
        => Set<MotivoOperacionInventario>();

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

    public DbSet<Bodega> Bodegas
    => Set<Bodega>();

    public DbSet<Producto> Productos
    => Set<Producto>();

    public DbSet<ProductoPresentacion> ProductosPresentaciones
        => Set<ProductoPresentacion>();

    public DbSet<ProductoPresentacionPrecio> ProductosPresentacionesPrecios
        => Set<ProductoPresentacionPrecio>();

    public DbSet<ProductoImpuesto> ProductosImpuestos
        => Set<ProductoImpuesto>();

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

    public DbSet<MovimientoInventarioDetalleLote>
        MovimientosInventarioDetallesLotes
        => Set<MovimientoInventarioDetalleLote>();

    public DbSet<MovimientoInventarioDetalleSerie>
        MovimientosInventarioDetallesSeries
        => Set<MovimientoInventarioDetalleSerie>();

    public DbSet<TransferenciaInventario> TransferenciasInventario
        => Set<TransferenciaInventario>();

    public DbSet<TransferenciaInventarioDetalle>
        TransferenciasInventarioDetalles
        => Set<TransferenciaInventarioDetalle>();

    public DbSet<AjusteInventario> AjustesInventario
        => Set<AjusteInventario>();

    public DbSet<AjusteInventarioDetalle> AjustesInventarioDetalles
        => Set<AjusteInventarioDetalle>();

    public DbSet<ConversionControlInventario> ConversionesControlInventario
        => Set<ConversionControlInventario>();

    public DbSet<ConversionControlInventarioDetalle>
        ConversionesControlInventarioDetalles
        => Set<ConversionControlInventarioDetalle>();

    public DbSet<ConversionControlInventarioSerie>
        ConversionesControlInventarioSeries
        => Set<ConversionControlInventarioSerie>();

    public DbSet<CorreccionDatoInventario> CorreccionesDatosInventario
        => Set<CorreccionDatoInventario>();

    public DbSet<ProductoCosto> ProductosCostos
        => Set<ProductoCosto>();

    public DbSet<VentaXf> VentasXf => Set<VentaXf>();
    public DbSet<VentaXfDetalle> VentasXfDetalles => Set<VentaXfDetalle>();
    public DbSet<NotaEntrega> NotasEntrega => Set<NotaEntrega>();
    public DbSet<NotaEntregaDetalle> NotasEntregaDetalles
        => Set<NotaEntregaDetalle>();
    public DbSet<NotaEntregaXfDetalle> NotasEntregaXfDetalles
        => Set<NotaEntregaXfDetalle>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturasDetalles => Set<FacturaDetalle>();
    public DbSet<FacturaDetalleImpuesto> FacturasDetallesImpuestos
        => Set<FacturaDetalleImpuesto>();
    public DbSet<FacturaNotaEntregaDetalle> FacturasNotasEntregaDetalles
        => Set<FacturaNotaEntregaDetalle>();
    public DbSet<FacturaXfDetalle> FacturasXfDetalles
        => Set<FacturaXfDetalle>();
    public DbSet<FacturaFormaPago> FacturasFormasPago
        => Set<FacturaFormaPago>();
    public DbSet<Proforma> Proformas => Set<Proforma>();
    public DbSet<ProformaDetalle> ProformasDetalles => Set<ProformaDetalle>();
    public DbSet<ProformaDetalleImpuesto> ProformasDetallesImpuestos
        => Set<ProformaDetalleImpuesto>();
    public DbSet<DevolucionVenta> DevolucionesVentas
        => Set<DevolucionVenta>();
    public DbSet<DevolucionVentaDetalle> DevolucionesVentasDetalles
        => Set<DevolucionVentaDetalle>();
    public DbSet<NotaCredito> NotasCredito => Set<NotaCredito>();
    public DbSet<NotaCreditoDetalle> NotasCreditoDetalles
        => Set<NotaCreditoDetalle>();
    public DbSet<NotaCreditoDetalleImpuesto> NotasCreditoDetallesImpuestos
        => Set<NotaCreditoDetalleImpuesto>();
    public DbSet<NotaDebito> NotasDebito => Set<NotaDebito>();
    public DbSet<NotaDebitoDetalle> NotasDebitoDetalles
        => Set<NotaDebitoDetalle>();
    public DbSet<NotaDebitoDetalleImpuesto> NotasDebitoDetallesImpuestos
        => Set<NotaDebitoDetalleImpuesto>();
    public DbSet<GuiaRemision> GuiasRemision => Set<GuiaRemision>();
    public DbSet<GuiaRemisionDetalle> GuiasRemisionDetalles
        => Set<GuiaRemisionDetalle>();

    public DbSet<DocumentoRecibidoSri> DocumentosRecibidosSri
        => Set<DocumentoRecibidoSri>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> ComprasDetalles => Set<CompraDetalle>();
    public DbSet<CompraDetalleImpuesto> ComprasDetallesImpuestos
        => Set<CompraDetalleImpuesto>();
    public DbSet<LiquidacionCompra> LiquidacionesCompra
        => Set<LiquidacionCompra>();
    public DbSet<LiquidacionCompraDetalle> LiquidacionesCompraDetalles
        => Set<LiquidacionCompraDetalle>();
    public DbSet<LiquidacionCompraDetalleImpuesto>
        LiquidacionesCompraDetallesImpuestos
        => Set<LiquidacionCompraDetalleImpuesto>();
    public DbSet<DevolucionCompra> DevolucionesCompras
        => Set<DevolucionCompra>();
    public DbSet<DevolucionCompraDetalle> DevolucionesComprasDetalles
        => Set<DevolucionCompraDetalle>();
    public DbSet<AjusteCompra> AjustesCompras => Set<AjusteCompra>();
    public DbSet<AjusteCompraDetalle> AjustesComprasDetalles
        => Set<AjusteCompraDetalle>();
    public DbSet<AjusteCompraDetalleImpuesto> AjustesComprasDetallesImpuestos
        => Set<AjusteCompraDetalleImpuesto>();

    public DbSet<CuentaPorCobrar> CuentasPorCobrar
        => Set<CuentaPorCobrar>();
    public DbSet<Cobro> Cobros => Set<Cobro>();
    public DbSet<CobroMedio> CobrosMedios => Set<CobroMedio>();
    public DbSet<CobroAplicacion> CobrosAplicaciones
        => Set<CobroAplicacion>();
    public DbSet<CobroAplicacionReverso> CobrosAplicacionesReversos
        => Set<CobroAplicacionReverso>();
    public DbSet<CuentaPorCobrarMovimiento> CuentasPorCobrarMovimientos
        => Set<CuentaPorCobrarMovimiento>();
    public DbSet<CuentaPorPagar> CuentasPorPagar
        => Set<CuentaPorPagar>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PagoMedio> PagosMedios => Set<PagoMedio>();
    public DbSet<PagoAplicacion> PagosAplicaciones
        => Set<PagoAplicacion>();
    public DbSet<PagoAplicacionReverso> PagosAplicacionesReversos
        => Set<PagoAplicacionReverso>();
    public DbSet<CuentaPorPagarMovimiento> CuentasPorPagarMovimientos
        => Set<CuentaPorPagarMovimiento>();

    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<CajaSesion> CajasSesiones => Set<CajaSesion>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<DepositoCajaBanco> DepositosCajaBanco
        => Set<DepositoCajaBanco>();
    public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();
    public DbSet<MovimientoBancario> MovimientosBancarios
        => Set<MovimientoBancario>();
    public DbSet<TransferenciaBancaria> TransferenciasBancarias
        => Set<TransferenciaBancaria>();

    public DbSet<PlanCuenta> PlanCuentas => Set<PlanCuenta>();
    public DbSet<ConfiguracionCuenta> ConfiguracionCuentas
        => Set<ConfiguracionCuenta>();
    public DbSet<PeriodoContable> PeriodosContables
        => Set<PeriodoContable>();
    public DbSet<SecuencialAsiento> SecuencialesAsientos
        => Set<SecuencialAsiento>();
    public DbSet<Asiento> Asientos => Set<Asiento>();
    public DbSet<AsientoDetalle> AsientosDetalles => Set<AsientoDetalle>();

    public DbSet<RetencionEmitida> RetencionesEmitidas
        => Set<RetencionEmitida>();
    public DbSet<RetencionEmitidaDetalle> RetencionesEmitidasDetalles
        => Set<RetencionEmitidaDetalle>();
    public DbSet<RetencionRecibida> RetencionesRecibidas
        => Set<RetencionRecibida>();
    public DbSet<RetencionRecibidaDocumento> RetencionesRecibidasDocumentos
        => Set<RetencionRecibidaDocumento>();
    public DbSet<RetencionRecibidaDetalle> RetencionesRecibidasDetalles
        => Set<RetencionRecibidaDetalle>();

    public DbSet<ComprobanteElectronico> ComprobantesElectronicos
        => Set<ComprobanteElectronico>();
    public DbSet<ComprobanteElectronicoEvento> ComprobantesElectronicosEventos
        => Set<ComprobanteElectronicoEvento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(KontaxDbContext).Assembly);
    }
}

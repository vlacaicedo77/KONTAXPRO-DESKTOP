using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;

namespace KONTAXPRO.Desktop.ViewModels.FacturacionElectronica;

public sealed record EstablecimientoNumeracionSriOption(
    long Id,
    string Codigo,
    string Nombre)
{
    public string Descripcion => $"{Codigo} · {Nombre}";
}

public partial class InicializacionSecuencialPuntoItem : ObservableObject
{
    public InicializacionSecuencialPuntoItem(
        long tipoComprobanteId,
        string codigoSri,
        string comprobante,
        long? ambientePruebasId,
        long? ambienteProduccionId)
    {
        TipoComprobanteId = tipoComprobanteId;
        CodigoSri = codigoSri;
        Comprobante = comprobante;
        AmbientePruebasId = ambientePruebasId;
        AmbienteProduccionId = ambienteProduccionId;
    }

    public long TipoComprobanteId { get; }
    public string CodigoSri { get; }
    public string Comprobante { get; }
    public long? AmbientePruebasId { get; }
    public long? AmbienteProduccionId { get; }
    [ObservableProperty] private bool tieneEmisionesPruebas;
    [ObservableProperty] private int ultimoSecuencialPruebas;
    [ObservableProperty] private bool tieneEmisionesProduccion;
    [ObservableProperty] private int ultimoSecuencialProduccion;
}

public partial class FacturacionElectronicaConfiguracionViewModel :
    ObservableObject, IAsyncNavigationTarget, IDisposable
{
    private readonly IConfiguracionFacturacionElectronicaService service;
    private readonly IDiagnosticoComunicacionSri diagnosticoComunicacion;
    private readonly IContextoInstalacion contextoInstalacion;
    private readonly CurrentSession session;
    private readonly IMessageDialogService dialogs;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? currentOperation;
    private bool disposed;
    private bool produccionOriginal;
    private bool habilitadaOriginal;
    private bool diagnosticoBaseListo;
    private bool suppressEnvironmentReload;
    private uint version;
    private IReadOnlyList<SecuencialComprobanteSriDto> numeracionesCargadas =
        [];
    private IReadOnlyList<PuntoEmisionSriDto> puntosEmisionCargados = [];
    private IReadOnlyList<PlantillaSecuencialPuntoEmisionSriDto>
        plantillasSecuencialesPunto = [];

    public FacturacionElectronicaConfiguracionViewModel(
        IConfiguracionFacturacionElectronicaService service,
        IDiagnosticoComunicacionSri diagnosticoComunicacion,
        IContextoInstalacion contextoInstalacion,
        CurrentSession session,
        IMessageDialogService dialogs)
    {
        this.service = service;
        this.diagnosticoComunicacion = diagnosticoComunicacion;
        this.contextoInstalacion = contextoInstalacion;
        this.session = session;
        this.dialogs = dialogs;
        session.EmpresaActivaChanged += OnEmpresaActivaChanged;
        session.EstablecimientoActivoChanged +=
            OnEstablecimientoActivoChanged;
    }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool habilitada;
    [ObservableProperty] private bool esProduccion;
    [ObservableProperty] private string ambiente = "PRUEBAS";
    [ObservableProperty] private string certificadoNombre = "Sin certificado";
    [ObservableProperty] private string certificadoTitular = "No configurado";
    [ObservableProperty] private string certificadoEmisor = "No configurado";
    [ObservableProperty] private string certificadoSerie = "—";
    [ObservableProperty] private string certificadoVigencia = "—";
    [ObservableProperty] private string certificadoRevocacion = "NO COMPROBADA";
    [ObservableProperty] private bool listoParaFacturar;
    [ObservableProperty] private bool tieneAdvertencias;
    [ObservableProperty] private string estadoGeneral =
        "CONFIGURACIÓN PENDIENTE";
    [ObservableProperty]
    private EstablecimientoNumeracionSriOption?
        establecimientoNumeracionSeleccionado;
    [ObservableProperty]
    private SecuencialComprobanteSriDto? numeracionSeleccionada;
    [ObservableProperty] private int ultimoSecuencialEditado;
    [ObservableProperty] private string proximoNumeroCompleto =
        "SELECCIONA UNA NUMERACIÓN";
    [ObservableProperty] private string estadoEdicionNumeracion =
        "Selecciona una fila para configurar su valor inicial.";
    [ObservableProperty]
    private PuntoEmisionSriDto? puntoEmisionSeleccionado;
    [ObservableProperty] private string codigoNuevoPunto = "100";
    [ObservableProperty]
    private string nombreNuevoPunto = "PUNTO KONTAXPRO";
    [ObservableProperty] private bool nuevoPuntoPredeterminado = true;
    [ObservableProperty] private bool continuarNumeracionExistente;
    [ObservableProperty]
    private string nombrePuntoSeleccionadoEditado = string.Empty;
    [ObservableProperty] private int modoPanelNumeracion;

    public ObservableCollection<ItemDiagnosticoFacturacionElectronica>
        Diagnostico { get; } = [];
    public ObservableCollection<EstablecimientoNumeracionSriOption>
        EstablecimientosNumeracion { get; } = [];
    public ObservableCollection<SecuencialComprobanteSriDto>
        NumeracionesVisibles { get; } = [];
    public ObservableCollection<PuntoEmisionSriDto>
        PuntosEmisionVisibles { get; } = [];
    public ObservableCollection<InicializacionSecuencialPuntoItem>
        InicializacionSecuenciales { get; } = [];

    public bool EsServidor => contextoInstalacion.EsServidor;
    public bool PuedeConfigurar =>
        session.HasPermission("SRI_CONFIGURAR_FACTURACION");
    public bool PuedeEditarConfiguracion => PuedeConfigurar && !IsBusy;
    public bool PuedeCambiarAmbienteConfiguracion => PuedeConfigurar &&
        session.HasPermission("SRI_CAMBIAR_AMBIENTE") && !IsBusy;
    public bool PuedeCambiarCertificado => EsServidor && !IsBusy &&
        session.HasPermission("SRI_CAMBIAR_CERTIFICADO");
    public bool PuedeDiagnosticar =>
        session.HasPermission("SRI_EJECUTAR_DIAGNOSTICO");
    public bool PuedeDiagnosticarContexto => PuedeDiagnosticar &&
        !HayCambiosSinGuardar && !IsBusy;
    public bool PuedeAdministrarNumeracion =>
        session.HasPermission("SRI_ADMINISTRAR_SECUENCIALES");
    public bool HayCambiosSinGuardar =>
        EsProduccion != produccionOriginal || Habilitada != habilitadaOriginal;
    public string AlcanceDiagnostico => session.EstablecimientoId.HasValue &&
        session.PuntoEmisionId.HasValue
            ? $"Est. {session.EstablecimientoCodigo ?? "—"} · Punto {session.PuntoEmisionCodigo ?? "—"} · ambiente guardado"
            : "Establecimiento y punto activos · ambiente guardado";
    public bool TieneNumeraciones => NumeracionesVisibles.Count > 0;
    public bool TienePuntosEmision => PuntosEmisionVisibles.Count > 0;
    public bool TieneNumeracionSeleccionada =>
        NumeracionSeleccionada is not null;
    public bool NumeracionBloqueada =>
        NumeracionSeleccionada?.TieneComprobantesKontax == true;
    public bool PuedeGuardarNumeracion =>
        PuedeAdministrarNumeracion && !IsBusy &&
        NumeracionSeleccionada is
            { TieneComprobantesKontax: false } selected &&
        UltimoSecuencialEditado is >= 0 and <= 999_999_998 &&
        UltimoSecuencialEditado != selected.UltimoSecuencial;
    public bool PuedeCrearPuntoEmision =>
        PuedeAdministrarNumeracion && !IsBusy &&
        EstablecimientoNumeracionSeleccionado is not null &&
        EsCodigoPuntoValido(CodigoNuevoPunto);
    public bool PuedeRenombrarPuntoEmision =>
        PuedeAdministrarNumeracion && !IsBusy &&
        PuntoEmisionSeleccionado is not null &&
        !string.IsNullOrWhiteSpace(NombrePuntoSeleccionadoEditado) &&
        !string.Equals(PuntoEmisionSeleccionado.Nombre,
            NombrePuntoSeleccionadoEditado.Trim(),
            StringComparison.OrdinalIgnoreCase);
    public bool PuedeEstablecerPuntoPredeterminado =>
        PuedeAdministrarNumeracion && !IsBusy &&
        PuntoEmisionSeleccionado is { Activo: true,
            EsPredeterminado: false } selected &&
        selected.EstablecimientoId == session.EstablecimientoId;
    public bool PuedeCambiarEstadoPunto =>
        PuedeAdministrarNumeracion && !IsBusy &&
        PuntoEmisionSeleccionado is not null &&
        (!PuntoEmisionSeleccionado.Activo ||
         PuntosEmisionVisibles.Count(x => x.Activo) > 1);
    public bool PuedePredeterminarPuntoNuevo =>
        !IsBusy && EstablecimientoNumeracionSeleccionado?.Id ==
        session.EstablecimientoId;
    public bool MostrarEditorSecuencial => ModoPanelNumeracion == 0;
    public bool MostrarAdministracionPunto => ModoPanelNumeracion == 1;
    public bool MostrarCreacionPunto => ModoPanelNumeracion == 2;
    public string TextoEstadoPunto => PuntoEmisionSeleccionado?.Activo == true
        ? "Inactivar"
        : "Activar";
    public string DescripcionNodo => EsServidor
        ? "Certificado protegido y procesamiento automático en este servidor."
        : "Modo cliente: el certificado se administra únicamente en el servidor.";

    public Task InitializeAsync(
        CancellationToken cancellationToken = default) =>
        EjecutarCargaAsync(cancellationToken);

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!PuedeConfigurar || !session.EmpresaId.HasValue) return;
        var ambienteCambio = EsProduccion != produccionOriginal;
        if (ambienteCambio &&
            !session.HasPermission("SRI_CAMBIAR_AMBIENTE"))
        {
            await dialogs.ShowWarningAsync("Permiso requerido",
                "No tienes permiso para cambiar el ambiente SRI.");
            return;
        }
        if (ambienteCambio && EsProduccion)
        {
            var confirmed = await dialogs.ConfirmWarningAsync(
                "Activar ambiente de producción",
                "Los comprobantes se enviarán al ambiente oficial del SRI. Confirma que el certificado, punto de emisión y secuencial son correctos.",
                "Activar producción", "Cancelar");
            if (!confirmed) return;
        }

        await EjecutarAsync(async token =>
        {
            var companyId = session.EmpresaId!.Value;
            try
            {
                await service.GuardarAmbienteAsync(companyId,
                    session.UsuarioId, session.EstablecimientoId,
                    session.PuntoEmisionId,
                    EsProduccion ? "PRODUCCION" : "PRUEBAS",
                    Habilitada, version, token);
            }
            catch (InvalidOperationException exception) when (
                EsConflicto(exception))
            {
                await CargarAsync(companyId, session.EstablecimientoId,
                    session.PuntoEmisionId, token);
                throw;
            }
            await CargarAsync(companyId, session.EstablecimientoId,
                session.PuntoEmisionId, token);
            await dialogs.ShowSuccessAsync("Configuración guardada",
                "La configuración electrónica de la empresa fue actualizada.");
        });
    }

    [RelayCommand]
    private async Task DiagnosticarAsync()
    {
        if (!PuedeDiagnosticarContexto || !session.EmpresaId.HasValue) return;
        await EjecutarAsync(async token =>
        {
            try
            {
                var result = await service.DiagnosticarAsync(
                    session.EmpresaId.Value, session.UsuarioId,
                    session.EstablecimientoId, session.PuntoEmisionId, token);
                AplicarDiagnostico(result);
            }
            catch
            {
                InvalidarDiagnostico("No fue posible completar el diagnóstico de forma segura");
                throw;
            }
        });
    }

    [RelayCommand]
    private async Task ProbarComunicacionAsync()
    {
        if (!PuedeDiagnosticarContexto) return;
        await EjecutarAsync(async token =>
        {
            var result = await diagnosticoComunicacion.ProbarAsync(
                EsProduccion ? 2 : 1, token);
            for (var index = Diagnostico.Count - 1; index >= 0; index--)
                if (Diagnostico[index].Codigo.StartsWith("COMUNICACION_",
                        StringComparison.Ordinal))
                    Diagnostico.RemoveAt(index);
            foreach (var item in result.Items) Diagnostico.Add(item);
            ActualizarEstadoDesdeItems();
            if (result.Disponible)
                await dialogs.ShowSuccessAsync("Comunicación disponible",
                    $"Los servicios SRI del ambiente {Ambiente} respondieron con un WSDL válido.");
            else
                await dialogs.ShowWarningAsync("Comunicación no disponible",
                    "Uno o más servicios del SRI no devolvieron una respuesta válida. No se emitió ningún comprobante.");
        });
    }

    [RelayCommand(CanExecute = nameof(PuedeGuardarNumeracion))]
    private async Task GuardarNumeracionAsync()
    {
        if (!PuedeGuardarNumeracion || !session.EmpresaId.HasValue ||
            NumeracionSeleccionada is not { } selected)
            return;

        var next = UltimoSecuencialEditado + 1;
        var nextDocument =
            $"{selected.EstablecimientoCodigo}-{selected.PuntoEmisionCodigo}-{next:D9}";
        var confirmed = await dialogs.ConfirmWarningAsync(
            "Confirmar numeración inicial",
            $"Se registrará {UltimoSecuencialEditado:D9} como el último " +
            $"{selected.TipoComprobanteNombre.ToLowerInvariant()} emitido en " +
            $"{selected.Ambiente}. El próximo comprobante será " +
            $"{nextDocument}. Revisa este valor antes de continuar.",
            "Guardar numeración", "Cancelar");
        if (!confirmed) return;

        await EjecutarAsync(async token =>
        {
            await service.ActualizarUltimoSecuencialAsync(
                session.EmpresaId.Value, session.UsuarioId, selected.Id,
                selected.UltimoSecuencial, UltimoSecuencialEditado, token);
            await CargarNumeracionesAsync(session.EmpresaId.Value, token,
                selected.Id);
            await dialogs.ShowSuccessAsync("Numeración actualizada",
                $"El próximo comprobante será {nextDocument}.");
        });
    }

    [RelayCommand(CanExecute = nameof(PuedeCrearPuntoEmision))]
    private async Task CrearPuntoEmisionAsync()
    {
        if (!PuedeCrearPuntoEmision || !session.EmpresaId.HasValue ||
            EstablecimientoNumeracionSeleccionado is not { } establishment)
            return;

        var codigo = FormatearCodigoPunto(CodigoNuevoPunto);
        var iniciales = CrearSecuencialesIniciales();
        if (ContinuarNumeracionExistente &&
            iniciales.Any(x => x.UltimoSecuencial is < 0 or > 999_999_998))
        {
            await dialogs.ShowWarningAsync("Numeración no válida",
                "Los últimos secuenciales deben estar entre 0 y 999999998.");
            return;
        }
        var confirmed = await dialogs.ConfirmWarningAsync(
            ContinuarNumeracionExistente
                ? "Continuar numeración existente"
                : "Crear punto de emisión",
            ContinuarNumeracionExistente
                ? $"Se incorporará el punto {establishment.Codigo}-{codigo} con los últimos números declarados. KONTAXPRO emitirá cada comprobante desde el número siguiente. Confirma cuidadosamente los valores de pruebas y producción."
                : $"Se creará el punto {establishment.Codigo}-{codigo} con todas sus numeraciones iniciadas en cero. Confirma que este código nunca fue utilizado por otro sistema; KONTAXPRO no puede comprobar emisiones externas.",
            "Crear punto", "Cancelar");
        if (!confirmed) return;

        await EjecutarAsync(async token =>
        {
            var establecerPredeterminado = NuevoPuntoPredeterminado &&
                PuedePredeterminarPuntoNuevo;
            await service.CrearPuntoEmisionAsync(session.EmpresaId.Value,
                session.UsuarioId, new CrearPuntoEmisionSriDto(
                    establishment.Id, codigo, NombreNuevoPunto,
                    establecerPredeterminado,
                    ContinuarNumeracionExistente,
                    ContinuarNumeracionExistente ? iniciales : []), token);
            await CargarNumeracionesAsync(session.EmpresaId.Value, token,
                seleccionarPuntoCodigo: codigo);
            SincronizarPuntoSesion();
            await dialogs.ShowSuccessAsync("Punto de emisión creado",
                ContinuarNumeracionExistente
                    ? "El punto quedó listo para continuar desde los números declarados."
                    : $"El próximo comprobante del nuevo punto será {establishment.Codigo}-{codigo}-000000001.");
        });
    }

    [RelayCommand]
    private void AbrirEditorSecuencial()
    {
        ModoPanelNumeracion = 0;
    }

    [RelayCommand]
    private void MostrarAdministracionPuntoSeleccionado()
    {
        if (PuntoEmisionSeleccionado is not null)
            ModoPanelNumeracion = 1;
    }

    [RelayCommand]
    private void MostrarCreacionPuntoNuevo()
    {
        SugerirCodigoPunto();
        ModoPanelNumeracion = 2;
    }

    [RelayCommand(CanExecute = nameof(PuedeRenombrarPuntoEmision))]
    private async Task RenombrarPuntoEmisionAsync()
    {
        if (!PuedeRenombrarPuntoEmision || !session.EmpresaId.HasValue ||
            PuntoEmisionSeleccionado is not { } punto)
            return;

        await EjecutarAsync(async token =>
        {
            await service.RenombrarPuntoEmisionAsync(
                session.EmpresaId.Value, session.UsuarioId, punto.Id,
                NombrePuntoSeleccionadoEditado, token);
            await CargarNumeracionesAsync(session.EmpresaId.Value, token,
                seleccionarPuntoId: punto.Id);
            SincronizarPuntoSesion();
            await dialogs.ShowSuccessAsync("Nombre actualizado",
                "El nombre interno del punto de emisión fue actualizado.");
        });
    }

    [RelayCommand(CanExecute = nameof(PuedeEstablecerPuntoPredeterminado))]
    private async Task EstablecerPuntoPredeterminadoAsync()
    {
        if (!PuedeEstablecerPuntoPredeterminado ||
            !session.EmpresaId.HasValue ||
            PuntoEmisionSeleccionado is not { } punto)
            return;

        await EjecutarAsync(async token =>
        {
            await service.EstablecerPuntoEmisionPredeterminadoAsync(
                session.EmpresaId.Value, session.UsuarioId, punto.Id, token);
            await CargarNumeracionesAsync(session.EmpresaId.Value, token,
                seleccionarPuntoId: punto.Id);
            SincronizarPuntoSesion();
            await dialogs.ShowSuccessAsync("Punto preferido actualizado",
                $"Las nuevas operaciones usarán el punto {punto.Codigo}.");
        });
    }

    [RelayCommand(CanExecute = nameof(PuedeCambiarEstadoPunto))]
    private async Task CambiarEstadoPuntoAsync()
    {
        if (!PuedeCambiarEstadoPunto || !session.EmpresaId.HasValue ||
            PuntoEmisionSeleccionado is not { } punto)
            return;
        var activar = !punto.Activo;
        var confirmed = await dialogs.ConfirmWarningAsync(
            activar ? "Activar punto de emisión" :
                "Inactivar punto de emisión",
            activar
                ? $"El punto {punto.Codigo} volverá a estar disponible para nuevas emisiones."
                : $"El punto {punto.Codigo} dejará de estar disponible para nuevas emisiones. Su historial y numeraciones se conservarán.",
            activar ? "Activar" : "Inactivar", "Cancelar");
        if (!confirmed) return;

        await EjecutarAsync(async token =>
        {
            await service.CambiarEstadoPuntoEmisionAsync(
                session.EmpresaId.Value, session.UsuarioId, punto.Id,
                activar, token);
            await CargarNumeracionesAsync(session.EmpresaId.Value, token,
                seleccionarPuntoId: punto.Id);
            SincronizarPuntoSesion();
            await dialogs.ShowSuccessAsync(
                activar ? "Punto activado" : "Punto inactivado",
                activar
                    ? $"El punto {punto.Codigo} está disponible nuevamente."
                    : $"El punto {punto.Codigo} fue inactivado sin eliminar su historial.");
        });
    }

    public async Task ImportarCertificadoAsync(
        string fileName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!PuedeCambiarCertificado || !session.EmpresaId.HasValue) return;
        var file = new FileInfo(fileName);
        if (!file.Exists || file.Length is <= 0 or > 5 * 1024 * 1024)
        {
            await dialogs.ShowWarningAsync("Certificado no válido",
                "El archivo debe existir, contener información y no superar 5 MB.");
            return;
        }

        await EjecutarAsync(async token =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                token, cancellationToken);
            var bytes = await File.ReadAllBytesAsync(fileName, linked.Token);
            try
            {
                var companyId = session.EmpresaId!.Value;
                try
                {
                    await service.ImportarCertificadoAsync(companyId,
                        session.UsuarioId, Path.GetFileName(fileName), bytes,
                        password, version, linked.Token);
                }
                catch (InvalidOperationException exception) when (
                    EsConflicto(exception))
                {
                    await CargarAsync(companyId, session.EstablecimientoId,
                        session.PuntoEmisionId, linked.Token);
                    throw;
                }
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations
                    .ZeroMemory(bytes);
            }
            await CargarAsync(session.EmpresaId.Value,
                session.EstablecimientoId, session.PuntoEmisionId,
                linked.Token);
            await dialogs.ShowSuccessAsync("Certificado configurado",
                "El certificado fue validado y almacenado de forma protegida en el servidor.");
        }, cancellationToken);
    }

    partial void OnEsProduccionChanged(bool value)
    {
        Ambiente = value ? "PRODUCCIÓN" : "PRUEBAS";
        NotificarEstadoConfiguracion();
        if (!suppressEnvironmentReload && !IsBusy && !disposed &&
            PuedeAdministrarNumeracion &&
            session.EmpresaId.HasValue)
            _ = RecargarNumeracionesAmbienteAsync();
    }

    partial void OnHabilitadaChanged(bool value) =>
        NotificarEstadoConfiguracion();

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(PuedeEditarConfiguracion));
        OnPropertyChanged(nameof(PuedeCambiarAmbienteConfiguracion));
        OnPropertyChanged(nameof(PuedeCambiarCertificado));
        OnPropertyChanged(nameof(PuedeDiagnosticarContexto));
        OnPropertyChanged(nameof(PuedeGuardarNumeracion));
        OnPropertyChanged(nameof(PuedeCrearPuntoEmision));
        OnPropertyChanged(nameof(PuedeRenombrarPuntoEmision));
        OnPropertyChanged(nameof(PuedeEstablecerPuntoPredeterminado));
        OnPropertyChanged(nameof(PuedeCambiarEstadoPunto));
        OnPropertyChanged(nameof(PuedePredeterminarPuntoNuevo));
        GuardarNumeracionCommand.NotifyCanExecuteChanged();
        CrearPuntoEmisionCommand.NotifyCanExecuteChanged();
        RenombrarPuntoEmisionCommand.NotifyCanExecuteChanged();
        EstablecerPuntoPredeterminadoCommand.NotifyCanExecuteChanged();
        CambiarEstadoPuntoCommand.NotifyCanExecuteChanged();
    }

    private void NotificarEstadoConfiguracion()
    {
        OnPropertyChanged(nameof(HayCambiosSinGuardar));
        OnPropertyChanged(nameof(PuedeDiagnosticarContexto));
        ActualizarEstadoDerivado();
    }

    partial void OnEstablecimientoNumeracionSeleccionadoChanged(
        EstablecimientoNumeracionSriOption? value)
    {
        AplicarFiltroPuntosEmision();
        AplicarFiltroNumeracion();
        SugerirCodigoPunto();
        OnPropertyChanged(nameof(PuedeCrearPuntoEmision));
        OnPropertyChanged(nameof(PuedePredeterminarPuntoNuevo));
        CrearPuntoEmisionCommand.NotifyCanExecuteChanged();
    }

    partial void OnNumeracionSeleccionadaChanged(
        SecuencialComprobanteSriDto? value)
    {
        if (value is not null)
            ModoPanelNumeracion = 0;
        UltimoSecuencialEditado = value?.UltimoSecuencial ?? 0;
        EstadoEdicionNumeracion = value switch
        {
            null => "Selecciona una fila para configurar su valor inicial.",
            { TieneComprobantesKontax: true } =>
                "Bloqueada: KONTAXPRO ya generó comprobantes con esta numeración.",
            _ => "Configuración inicial disponible. Verifica el último número realmente emitido."
        };
        ActualizarVistaPreviaNumeracion();
    }

    partial void OnUltimoSecuencialEditadoChanged(int value) =>
        ActualizarVistaPreviaNumeracion();

    partial void OnPuntoEmisionSeleccionadoChanged(
        PuntoEmisionSriDto? value)
    {
        NombrePuntoSeleccionadoEditado = value?.Nombre ?? string.Empty;
        AplicarFiltroNumeracion();
        OnPropertyChanged(nameof(PuedeEstablecerPuntoPredeterminado));
        OnPropertyChanged(nameof(PuedeCambiarEstadoPunto));
        OnPropertyChanged(nameof(TextoEstadoPunto));
        EstablecerPuntoPredeterminadoCommand.NotifyCanExecuteChanged();
        CambiarEstadoPuntoCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PuedeRenombrarPuntoEmision));
        RenombrarPuntoEmisionCommand.NotifyCanExecuteChanged();
    }

    partial void OnNombrePuntoSeleccionadoEditadoChanged(string value)
    {
        OnPropertyChanged(nameof(PuedeRenombrarPuntoEmision));
        RenombrarPuntoEmisionCommand.NotifyCanExecuteChanged();
    }

    partial void OnContinuarNumeracionExistenteChanged(bool value) =>
        ReiniciarSecuencialesIniciales();

    partial void OnModoPanelNumeracionChanged(int value)
    {
        OnPropertyChanged(nameof(MostrarEditorSecuencial));
        OnPropertyChanged(nameof(MostrarAdministracionPunto));
        OnPropertyChanged(nameof(MostrarCreacionPunto));
    }

    partial void OnCodigoNuevoPuntoChanged(string value)
    {
        OnPropertyChanged(nameof(PuedeCrearPuntoEmision));
        CrearPuntoEmisionCommand.NotifyCanExecuteChanged();
    }

    private async Task EjecutarCargaAsync(CancellationToken cancellationToken)
    {
        if (!session.EmpresaId.HasValue)
            throw new InvalidOperationException(
                "Seleccione una empresa antes de configurar el SRI.");
        await EjecutarAsync(token => CargarAsync(session.EmpresaId.Value,
            session.EstablecimientoId, session.PuntoEmisionId, token),
            cancellationToken, showErrors: false);
    }

    private async Task CargarAsync(
        long empresaId,
        long? establecimientoId,
        long? puntoEmisionId,
        CancellationToken cancellationToken)
    {
        var config = await service.ObtenerAsync(empresaId, session.UsuarioId,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        version = config.Version;
        produccionOriginal = string.Equals(config.AmbienteCodigo,
            "PRODUCCION", StringComparison.OrdinalIgnoreCase);
        habilitadaOriginal = config.Habilitada;
        Habilitada = habilitadaOriginal;
        EsProduccion = produccionOriginal;
        Ambiente = EsProduccion ? "PRODUCCIÓN" : "PRUEBAS";
        CertificadoNombre = config.CertificadoNombre ?? "Sin certificado";
        CertificadoTitular = config.CertificadoTitular ?? "No configurado";
        CertificadoEmisor = config.CertificadoEmisor ?? "No configurado";
        CertificadoSerie = config.CertificadoNumeroSerie ?? "—";
        CertificadoVigencia = config.CertificadoFechaInicio.HasValue &&
                              config.CertificadoFechaCaducidad.HasValue
            ? $"{config.CertificadoFechaInicio:dd/MM/yyyy} — {config.CertificadoFechaCaducidad:dd/MM/yyyy}"
            : "—";

        await CargarNumeracionesAsync(empresaId, cancellationToken);

        if (!PuedeDiagnosticar)
        {
            diagnosticoBaseListo = false;
            Diagnostico.Clear();
            Diagnostico.Add(new ItemDiagnosticoFacturacionElectronica(
                "PERMISO", "Diagnóstico de configuración", false,
                "No tienes permiso para ejecutar el diagnóstico SRI"));
            ListoParaFacturar = false;
            TieneAdvertencias = false;
            CertificadoRevocacion = "NO COMPROBADA";
            EstadoGeneral = "DIAGNÓSTICO RESTRINGIDO";
            return;
        }

        try
        {
            AplicarDiagnostico(await service.DiagnosticarAsync(empresaId,
                session.UsuarioId, establecimientoId, puntoEmisionId,
                cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            diagnosticoBaseListo = false;
            Diagnostico.Clear();
            Diagnostico.Add(new ItemDiagnosticoFacturacionElectronica(
                "DIAGNOSTICO", "Diagnóstico de configuración", false,
                "No fue posible completar el diagnóstico de forma segura"));
            ListoParaFacturar = false;
            TieneAdvertencias = false;
            EstadoGeneral = "REVISIÓN REQUERIDA";
        }
    }

    private void InvalidarDiagnostico(string detalle)
    {
        diagnosticoBaseListo = false;
        Diagnostico.Clear();
        Diagnostico.Add(new ItemDiagnosticoFacturacionElectronica(
            "DIAGNOSTICO", "Diagnóstico de configuración", false, detalle));
        ActualizarEstadoDerivado();
    }

    private void AplicarDiagnostico(
        ResultadoDiagnosticoFacturacionElectronica result)
    {
        Diagnostico.Clear();
        foreach (var item in result.Items) Diagnostico.Add(item);
        diagnosticoBaseListo = result.ListoParaFacturar;
        ActualizarEstadoDerivado();
    }

    private void ActualizarEstadoDesdeItems()
    {
        diagnosticoBaseListo = Diagnostico.Count > 0 &&
                               Diagnostico.All(x => x.Correcto);
        ActualizarEstadoDerivado();
    }

    private void ActualizarEstadoDerivado()
    {
        TieneAdvertencias = Diagnostico.Any(x =>
            string.Equals(x.Nivel, "ADVERTENCIA",
                StringComparison.OrdinalIgnoreCase));
        ListoParaFacturar = !HayCambiosSinGuardar && diagnosticoBaseListo;
        EstadoGeneral = HayCambiosSinGuardar
            ? "CAMBIOS SIN GUARDAR"
            : ListoParaFacturar
            ? TieneAdvertencias
                ? "LISTO CON ADVERTENCIAS"
                : "LISTO PARA FACTURAR"
            : "CONFIGURACIÓN PENDIENTE";
        CertificadoRevocacion = Diagnostico.FirstOrDefault(x =>
            x.Codigo == "REVOCACION_CERTIFICADO")?.Detalle ??
            "NO COMPROBADA";
    }

    private async Task RecargarNumeracionesAmbienteAsync()
    {
        await EjecutarAsync(token => CargarNumeracionesAsync(
            session.EmpresaId!.Value, token));
    }

    private async Task CargarNumeracionesAsync(
        long empresaId,
        CancellationToken cancellationToken,
        long? seleccionarSecuencialId = null,
        long? seleccionarPuntoId = null,
        string? seleccionarPuntoCodigo = null)
    {
        if (!PuedeAdministrarNumeracion)
        {
            LimpiarNumeraciones();
            return;
        }

        var previousEstablishmentId =
            EstablecimientoNumeracionSeleccionado?.Id ??
            session.EstablecimientoId;
        var previousSequenceId = seleccionarSecuencialId ??
                                 NumeracionSeleccionada?.Id;
        var previousPointId = seleccionarPuntoId ??
                              PuntoEmisionSeleccionado?.Id;
        var administracion = await service.ObtenerPuntosEmisionAsync(
            empresaId, session.UsuarioId, cancellationToken);
        numeracionesCargadas = await service.ObtenerNumeracionesAsync(
            empresaId, session.UsuarioId,
            EsProduccion ? "PRODUCCION" : "PRUEBAS", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        puntosEmisionCargados = administracion.Puntos;
        plantillasSecuencialesPunto = administracion.PlantillasSecuenciales;
        ConstruirSecuencialesIniciales();

        EstablecimientosNumeracion.Clear();
        foreach (var option in administracion.Establecimientos)
            EstablecimientosNumeracion.Add(
                new EstablecimientoNumeracionSriOption(option.Id,
                    option.Codigo, option.Nombre));

        EstablecimientoNumeracionSeleccionado =
            EstablecimientosNumeracion.FirstOrDefault(x =>
                x.Id == previousEstablishmentId) ??
            EstablecimientosNumeracion.FirstOrDefault();
        AplicarFiltroPuntosEmision(previousPointId,
            seleccionarPuntoCodigo);
        AplicarFiltroNumeracion(previousSequenceId);
    }

    private void AplicarFiltroPuntosEmision(
        long? seleccionarPuntoId = null,
        string? seleccionarPuntoCodigo = null)
    {
        var previousPointId = seleccionarPuntoId ??
                              PuntoEmisionSeleccionado?.Id;
        PuntosEmisionVisibles.Clear();
        if (EstablecimientoNumeracionSeleccionado is { } establishment)
            foreach (var item in puntosEmisionCargados.Where(x =>
                         x.EstablecimientoId == establishment.Id))
                PuntosEmisionVisibles.Add(item);

        PuntoEmisionSeleccionado = !string.IsNullOrWhiteSpace(
            seleccionarPuntoCodigo)
            ? PuntosEmisionVisibles.FirstOrDefault(x =>
                x.Codigo == seleccionarPuntoCodigo)
            : PuntosEmisionVisibles.FirstOrDefault(x =>
                  x.Id == previousPointId) ??
              PuntosEmisionVisibles.FirstOrDefault(x =>
                  x.EsPredeterminado) ??
              PuntosEmisionVisibles.FirstOrDefault();
        OnPropertyChanged(nameof(TienePuntosEmision));
        OnPropertyChanged(nameof(PuedeCambiarEstadoPunto));
        CambiarEstadoPuntoCommand.NotifyCanExecuteChanged();
    }

    private void AplicarFiltroNumeracion(long? seleccionarSecuencialId = null)
    {
        var previousSequenceId = seleccionarSecuencialId ??
                                 NumeracionSeleccionada?.Id;
        NumeracionesVisibles.Clear();
        if (EstablecimientoNumeracionSeleccionado is { } establishment &&
            PuntoEmisionSeleccionado is { Activo: true } point)
            foreach (var item in numeracionesCargadas.Where(x =>
                         x.EstablecimientoId == establishment.Id &&
                         x.PuntoEmisionId == point.Id))
                NumeracionesVisibles.Add(item);

        NumeracionSeleccionada = NumeracionesVisibles.FirstOrDefault(x =>
            x.Id == previousSequenceId) ?? NumeracionesVisibles.FirstOrDefault();
        OnPropertyChanged(nameof(TieneNumeraciones));
    }

    private void LimpiarNumeraciones()
    {
        numeracionesCargadas = [];
        puntosEmisionCargados = [];
        plantillasSecuencialesPunto = [];
        EstablecimientosNumeracion.Clear();
        NumeracionesVisibles.Clear();
        PuntosEmisionVisibles.Clear();
        InicializacionSecuenciales.Clear();
        EstablecimientoNumeracionSeleccionado = null;
        PuntoEmisionSeleccionado = null;
        NumeracionSeleccionada = null;
        OnPropertyChanged(nameof(TieneNumeraciones));
        OnPropertyChanged(nameof(TienePuntosEmision));
    }

    private void SugerirCodigoPunto()
    {
        var usados = puntosEmisionCargados
            .Where(x => x.EstablecimientoId ==
                        EstablecimientoNumeracionSeleccionado?.Id)
            .Select(x => x.Codigo)
            .ToHashSet(StringComparer.Ordinal);
        var sugerido = Enumerable.Range(100, 900)
            .Concat(Enumerable.Range(1, 99))
            .Select(x => x.ToString("D3",
                System.Globalization.CultureInfo.InvariantCulture))
            .FirstOrDefault(x => !usados.Contains(x));
        CodigoNuevoPunto = sugerido ?? string.Empty;
        NombreNuevoPunto = sugerido is null
            ? string.Empty
            : $"PUNTO KONTAXPRO {sugerido}";
        NuevoPuntoPredeterminado = PuedePredeterminarPuntoNuevo;
        ContinuarNumeracionExistente = false;
        ReiniciarSecuencialesIniciales();
    }

    private void ConstruirSecuencialesIniciales()
    {
        InicializacionSecuenciales.Clear();
        foreach (var tipo in plantillasSecuencialesPunto
                     .GroupBy(x => new
                     {
                         x.TipoComprobanteId,
                         x.TipoComprobanteCodigoSri,
                         x.TipoComprobanteNombre
                     })
                     .OrderBy(x => x.Key.TipoComprobanteCodigoSri))
        {
            var pruebas = tipo.FirstOrDefault(x =>
                !EsAmbienteProduccion(x.Ambiente));
            var produccion = tipo.FirstOrDefault(x =>
                EsAmbienteProduccion(x.Ambiente));
            InicializacionSecuenciales.Add(
                new InicializacionSecuencialPuntoItem(
                    tipo.Key.TipoComprobanteId,
                    tipo.Key.TipoComprobanteCodigoSri,
                    tipo.Key.TipoComprobanteNombre,
                    pruebas?.TipoAmbienteId,
                    produccion?.TipoAmbienteId));
        }
    }

    private void ReiniciarSecuencialesIniciales()
    {
        foreach (var item in InicializacionSecuenciales)
        {
            item.TieneEmisionesPruebas = false;
            item.UltimoSecuencialPruebas = 0;
            item.TieneEmisionesProduccion = false;
            item.UltimoSecuencialProduccion = 0;
        }
    }

    private SecuencialInicialPuntoEmisionSriDto[]
        CrearSecuencialesIniciales()
    {
        var items = new List<SecuencialInicialPuntoEmisionSriDto>();
        foreach (var item in InicializacionSecuenciales)
        {
            if (item.AmbientePruebasId.HasValue)
                items.Add(new SecuencialInicialPuntoEmisionSriDto(
                    item.TipoComprobanteId,
                    item.AmbientePruebasId.Value,
                    item.TieneEmisionesPruebas
                        ? item.UltimoSecuencialPruebas
                        : 0));
            if (item.AmbienteProduccionId.HasValue)
                items.Add(new SecuencialInicialPuntoEmisionSriDto(
                    item.TipoComprobanteId,
                    item.AmbienteProduccionId.Value,
                    item.TieneEmisionesProduccion
                        ? item.UltimoSecuencialProduccion
                        : 0));
        }

        return items.ToArray();
    }

    private static bool EsAmbienteProduccion(string ambiente) =>
        ambiente.Contains("PRODU", StringComparison.OrdinalIgnoreCase);

    private void SincronizarPuntoSesion()
    {
        if (!session.EstablecimientoId.HasValue) return;
        var punto = puntosEmisionCargados.FirstOrDefault(x =>
            x.EstablecimientoId == session.EstablecimientoId &&
            x.EsPredeterminado && x.Activo);
        if (punto is null) return;
        session.PuntoEmisionId = punto.Id;
        session.PuntoEmisionCodigo = punto.Codigo;
        session.PuntoEmisionNombre = punto.Nombre;
    }

    private static string FormatearCodigoPunto(string value)
    {
        var codigo = (value ?? string.Empty).Trim();
        if (!EsCodigoPuntoValido(codigo) ||
            !int.TryParse(codigo, out var number))
            throw new ArgumentException(
                "El punto de emisión debe ser un número entre 001 y 999.");
        return number.ToString("D3",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool EsCodigoPuntoValido(string? value) =>
        int.TryParse(value?.Trim(), out var number) &&
        number is >= 1 and <= 999 && value!.Trim().Length <= 3;

    private void ActualizarVistaPreviaNumeracion()
    {
        ProximoNumeroCompleto = NumeracionSeleccionada is not { } selected
            ? "SELECCIONA UNA NUMERACIÓN"
            : UltimoSecuencialEditado is < 0 or >= 999_999_999
                ? "SECUENCIAL NO DISPONIBLE"
                : $"{selected.EstablecimientoCodigo}-" +
                  $"{selected.PuntoEmisionCodigo}-" +
                  $"{UltimoSecuencialEditado + 1:D9}";
        OnPropertyChanged(nameof(TieneNumeracionSeleccionada));
        OnPropertyChanged(nameof(NumeracionBloqueada));
        OnPropertyChanged(nameof(PuedeGuardarNumeracion));
        GuardarNumeracionCommand.NotifyCanExecuteChanged();
    }

    private async Task EjecutarAsync(
        Func<CancellationToken, Task> action,
        CancellationToken externalToken = default,
        bool showErrors = true)
    {
        if (IsBusy || disposed) return;
        IsBusy = true;
        var operation = CancellationTokenSource.CreateLinkedTokenSource(
            lifetime.Token, externalToken);
        var previous = Interlocked.Exchange(ref currentOperation, operation);
        previous?.Cancel();
        previous?.Dispose();
        try
        {
            await action(operation.Token);
        }
        catch (OperationCanceledException) when (
            operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            if (showErrors)
                await dialogs.ShowErrorAsync(
                    "No fue posible completar la operación",
                    MensajeSeguro(exception), null);
            else
                throw;
        }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(
                    ref currentOperation, null, operation), operation))
                operation.Dispose();
            IsBusy = false;
        }
    }

    private static string MensajeSeguro(Exception exception) => exception switch
    {
        UnauthorizedAccessException => exception.Message,
        ArgumentException => exception.Message,
        InvalidOperationException => exception.Message,
        _ => "Revisa la configuración e inténtalo nuevamente."
    };

    private static bool EsConflicto(InvalidOperationException exception) =>
        exception.Message.Contains("modificada por otro usuario",
            StringComparison.OrdinalIgnoreCase);

    private async void OnEmpresaActivaChanged(
        object? sender,
        EmpresaActivaChangedEventArgs e)
    {
        OnPropertyChanged(nameof(PuedeConfigurar));
        OnPropertyChanged(nameof(PuedeEditarConfiguracion));
        OnPropertyChanged(nameof(PuedeCambiarAmbienteConfiguracion));
        OnPropertyChanged(nameof(PuedeCambiarCertificado));
        OnPropertyChanged(nameof(PuedeDiagnosticar));
        OnPropertyChanged(nameof(PuedeDiagnosticarContexto));
        OnPropertyChanged(nameof(PuedeAdministrarNumeracion));
        OnPropertyChanged(nameof(AlcanceDiagnostico));
        await RecargarContextoActivoAsync(
            () => session.EmpresaId == e.EmpresaActualId);
    }

    private async void OnEstablecimientoActivoChanged(
        object? sender,
        EstablecimientoActivoChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AlcanceDiagnostico));
        await RecargarContextoActivoAsync(
            () => session.EstablecimientoId == e.EstablecimientoActualId);
    }

    private async Task RecargarContextoActivoAsync(
        Func<bool> sigueSiendoContextoActual)
    {
        currentOperation?.Cancel();
        LimpiarContextoVisual("CARGANDO CONTEXTO");
        try
        {
            while (IsBusy && !disposed)
                await Task.Delay(25, lifetime.Token);

            if (disposed || !session.EmpresaId.HasValue ||
                !sigueSiendoContextoActual())
                return;

            await EjecutarCargaAsync(lifetime.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            LimpiarContextoVisual("NO SE PUDO CARGAR");
            if (!disposed)
                await dialogs.ShowErrorAsync(
                    "No fue posible actualizar Configuración SRI",
                    MensajeSeguro(exception), null);
        }
    }

    private void LimpiarContextoVisual(string estado)
    {
        suppressEnvironmentReload = true;
        try
        {
        version = 0;
        produccionOriginal = false;
        habilitadaOriginal = false;
        diagnosticoBaseListo = false;
        Habilitada = false;
        EsProduccion = false;
        Ambiente = "PRUEBAS";
        CertificadoNombre = "Sin certificado";
        CertificadoTitular = "No configurado";
        CertificadoEmisor = "No configurado";
        CertificadoSerie = "—";
        CertificadoVigencia = "—";
        CertificadoRevocacion = "NO COMPROBADA";
        Diagnostico.Clear();
        LimpiarNumeraciones();
        ListoParaFacturar = false;
        TieneAdvertencias = false;
        EstadoGeneral = estado;
        OnPropertyChanged(nameof(HayCambiosSinGuardar));
        OnPropertyChanged(nameof(PuedeDiagnosticarContexto));
        OnPropertyChanged(nameof(AlcanceDiagnostico));
        }
        finally
        {
            suppressEnvironmentReload = false;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        session.EmpresaActivaChanged -= OnEmpresaActivaChanged;
        session.EstablecimientoActivoChanged -=
            OnEstablecimientoActivoChanged;
        lifetime.Cancel();
        currentOperation?.Cancel();
        currentOperation?.Dispose();
        lifetime.Dispose();
    }
}

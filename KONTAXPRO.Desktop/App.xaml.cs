using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;
using KONTAXPRO.Desktop.ViewModels;
using KONTAXPRO.Desktop.ViewModels.Products;
using KONTAXPRO.Desktop.ViewModels.Clientes;
using KONTAXPRO.Desktop.ViewModels.Proveedores;
using KONTAXPRO.Desktop.ViewModels.Compras;
using KONTAXPRO.Desktop.ViewModels.Tesoreria;
using KONTAXPRO.Desktop.ViewModels.Inventory;
using KONTAXPRO.Desktop.ViewModels.FacturacionElectronica;
using KONTAXPRO.Desktop.Views;
using KONTAXPRO.Desktop.Views.Products;
using KONTAXPRO.Desktop.Views.Clientes;
using KONTAXPRO.Desktop.Views.Proveedores;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using KONTAXPRO.Infrastructure.Products;
using KONTAXPRO.Infrastructure.Security;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Clientes;
using KONTAXPRO.Infrastructure.Proveedores;
using KONTAXPRO.Infrastructure.Interoperabilidad;
using KONTAXPRO.Infrastructure.Compras;
using KONTAXPRO.Infrastructure.Tesoreria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KONTAXPRO.Application.FacturacionElectronica;
using System;
using System.Windows;

namespace KONTAXPRO.Desktop
{
    public partial class App : System.Windows.Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IFacturacionElectronicaWorkerScheduler? _sriWorkerScheduler;

        public App()
        {
            // Cargar configuración desde appsettings.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: true)
                .AddJsonFile(
                    "appsettings.Local.json",
                    optional: true,
                    reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();

            // Registrar configuración
            services.AddSingleton(_configuration);
            services.AddLogging();

            // Registrar conexión a PostgreSQL mediante DbContextFactory
            var connectionString = Environment.GetEnvironmentVariable(
                "KONTAXPRO_CONNECTION_STRING") ??
                _configuration.GetConnectionString("DefaultConnection");
            services.AddDbContextFactory<KontaxDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Servicios
            services.AddSingleton<ThemeService>();
            services.AddSingleton<IMessageDialogService, MessageDialogService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<ILoadingService, LoadingService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<SessionFlowService>();
            services.AddTransient<IInventoryService, InventoryService>();
            services.AddTransient<IInventoryQueryService, InventoryQueryService>();
            services.AddTransient<IInventoryTransferService, InventoryTransferService>();
            services.AddTransient<IInventoryCancellationService, InventoryCancellationService>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductCatalogService, ProductCatalogService>();
            services.AddTransient<IClienteService, ClienteService>();
            services.AddTransient<IProveedorService, ProveedorService>();
            var documentsPath = Environment.GetEnvironmentVariable(
                "KONTAXPRO_DOCUMENTS_PATH") ??
                _configuration["DocumentStorage:ComprasRoot"] ??
                "data/documentos";
            if (!System.IO.Path.IsPathRooted(documentsPath))
                documentsPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory, documentsPath);
            services.AddSingleton(new CompraStorageOptions
            {
                DirectorioBase = documentsPath
            });
            services.AddSingleton<IContextoInstalacion>(new ContextoInstalacionLocal(
                documentsPath,
                Environment.GetEnvironmentVariable("KONTAXPRO_INSTALLATION_TYPE") ??
                _configuration["Installation:Type"] ?? "SERVIDOR",
                Environment.GetEnvironmentVariable("KONTAXPRO_INSTALLATION_ID") ??
                _configuration["Installation:Id"]));
            services.AddSingleton<IArchivoCompraStorage,
                ArchivoCompraFileStorage>();
            services.AddSingleton<ISoporteSinSustentoStorage,
                SoporteSinSustentoFileStorage>();
            services.AddSingleton<ImportacionCompraStore>();
            services.AddSingleton<IComprobanteCompraXmlReader,
                ComprobanteCompraXmlReader>();
            var sriValidationOptions = new ConsultaAutorizacionSriOptions
            {
                UrlPruebas = _configuration[
                    "Sri:ConsultaComprobante:UrlPruebas"] ??
                    new ConsultaAutorizacionSriOptions().UrlPruebas,
                UrlProduccion = _configuration[
                    "Sri:ConsultaComprobante:UrlProduccion"] ??
                    new ConsultaAutorizacionSriOptions().UrlProduccion,
                UrlAutorizacionPruebas = _configuration[
                    "Sri:ConsultaComprobante:UrlAutorizacionPruebas"] ??
                    new ConsultaAutorizacionSriOptions().UrlAutorizacionPruebas,
                UrlAutorizacionProduccion = _configuration[
                    "Sri:ConsultaComprobante:UrlAutorizacionProduccion"] ??
                    new ConsultaAutorizacionSriOptions().UrlAutorizacionProduccion,
                TimeoutSegundos = int.TryParse(_configuration[
                        "Sri:ConsultaComprobante:TimeoutSegundos"],
                    out var sriTimeout)
                    ? Math.Clamp(sriTimeout, 3, 60)
                    : 12
            };
            services.AddSingleton(sriValidationOptions);
            services.AddHttpClient<IConsultaAutorizacionComprobanteSri,
                ConsultaAutorizacionComprobanteSri>(client =>
                client.Timeout = TimeSpan.FromSeconds(
                    sriValidationOptions.TimeoutSegundos));
            var environmentName =
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable(
                    "ASPNETCORE_ENVIRONMENT");
            services.AddSingleton(new ComprasRuntimeOptions
            {
                EsDevelopment = string.Equals(
                    environmentName,
                    "Development",
                    StringComparison.OrdinalIgnoreCase)
            });
            services.AddTransient<ICompraImportacionService,
                CompraImportacionService>();
            services.AddTransient<ICompraProductoResolverService,
                CompraProductoResolverService>();
            services.AddTransient<ICompraService, CompraService>();
            services.AddTransient<ICompraRecepcionService,
                CompraRecepcionService>();
            services.AddTransient<IOperacionSinSustentoService,
                OperacionSinSustentoService>();
            services.AddTransient<IEstadoComprobanteElectronicoService,
                EstadoComprobanteElectronicoService>();
            var sriElectronicOptions = new SriEndpointsOptions
            {
                RecepcionPruebas = _configuration["Sri:FacturacionElectronica:RecepcionPruebas"] ?? new SriEndpointsOptions().RecepcionPruebas,
                AutorizacionPruebas = _configuration["Sri:FacturacionElectronica:AutorizacionPruebas"] ?? new SriEndpointsOptions().AutorizacionPruebas,
                RecepcionProduccion = _configuration["Sri:FacturacionElectronica:RecepcionProduccion"] ?? new SriEndpointsOptions().RecepcionProduccion,
                AutorizacionProduccion = _configuration["Sri:FacturacionElectronica:AutorizacionProduccion"] ?? new SriEndpointsOptions().AutorizacionProduccion,
                TimeoutSeconds = int.TryParse(_configuration["Sri:FacturacionElectronica:TimeoutSeconds"], out var electronicTimeout) ? Math.Clamp(electronicTimeout, 5, 120) : 45
            };
            services.AddSingleton(Options.Create(sriElectronicOptions));
            services.AddHttpClient<IClienteRecepcionSri, ClienteRecepcionSri>(client =>
                client.Timeout = TimeSpan.FromSeconds(sriElectronicOptions.TimeoutSeconds));
            services.AddHttpClient<IClienteAutorizacionSri, ClienteAutorizacionSri>(client =>
                client.Timeout = TimeSpan.FromSeconds(sriElectronicOptions.TimeoutSeconds));
            services.AddHttpClient<IDiagnosticoComunicacionSri, DiagnosticoComunicacionSri>(client =>
                client.Timeout = TimeSpan.FromSeconds(sriElectronicOptions.TimeoutSeconds));
            services.AddSingleton<IGeneradorClaveAccesoSri, GeneradorClaveAccesoSri>();
            services.AddSingleton<IGeneradorCodigoNumericoSri, GeneradorCodigoNumericoSri>();
            services.AddSingleton<IGeneradorXmlFacturaSri, GeneradorXmlFacturaSri>();
            services.AddSingleton<IValidadorXmlSri, ValidadorXmlSri>();
            services.AddSingleton<IValidadorCertificadoSri, ValidadorCertificadoSri>();
            services.AddSingleton<IFirmadorXadesSri, FirmadorXadesSri>();
            services.AddSingleton<IProtectorSecretosLocal, ProtectorSecretosWindows>();
            services.AddSingleton<IAlmacenamientoCertificadoSri, AlmacenamientoCertificadoSri>();
            services.AddSingleton<IAlmacenamientoDocumentosElectronicos, AlmacenamientoDocumentosElectronicos>();
            services.AddSingleton<IFirmadorXmlComprobanteElectronico, FirmadorXmlComprobanteElectronico>();
            services.AddSingleton<IClienteSriComprobantesElectronicos, ClienteSriComprobantesElectronicos>();
            services.AddTransient<IGeneradorXmlComprobanteElectronico, GeneradorXmlComprobanteElectronico>();
            services.AddTransient<IConfiguracionFacturacionElectronicaService, ConfiguracionFacturacionElectronicaService>();
            services.AddTransient<IAsignadorSecuencialComprobanteSri, AsignadorSecuencialComprobanteSri>();
            services.AddTransient<IMotorFacturaElectronicaSri, MotorFacturaElectronicaSri>();
            services.AddTransient<IProcesadorFacturacionElectronica, ProcesadorFacturacionElectronica>();
            services.AddSingleton<IWorkerFacturacionElectronica, WorkerFacturacionElectronica>();
            services.AddSingleton(new FacturacionElectronicaWorkerOptions
            {
                Intervalo = TimeSpan.FromSeconds(int.TryParse(
                    _configuration["Sri:FacturacionElectronica:WorkerIntervalSeconds"],
                    out var workerInterval)
                    ? Math.Clamp(workerInterval, 5, 300)
                    : 15)
            });
            services.AddSingleton<IFacturacionElectronicaWorkerScheduler,
                FacturacionElectronicaWorkerScheduler>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<ProductFormViewModel>();
            services.AddTransient<ClienteFormViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<ProveedorFormViewModel>();
            services.AddTransient<ProveedoresViewModel>();
            services.AddTransient<ComprasViewModel>();
            services.AddTransient<OperacionSinSustentoViewModel>();
            services.AddTransient<OperacionesSinSustentoViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<FacturacionElectronicaConfiguracionViewModel>();

            var interoperabilidadOptions =
                CreateInteroperabilidadOptions(_configuration);
            services.AddSingleton(interoperabilidadOptions);
            services.AddSingleton(TimeProvider.System);
            services.AddHttpClient(
                InteroperabilidadHttpClients.GuiaToken,
                client => client.Timeout = TimeSpan.FromSeconds(
                    interoperabilidadOptions.Guia.TimeoutSeconds));
            services.AddHttpClient(
                InteroperabilidadHttpClients.GuiaServicio,
                client => client.Timeout = TimeSpan.FromSeconds(
                    interoperabilidadOptions.Guia.TimeoutSeconds));
            services.AddHttpClient(
                InteroperabilidadHttpClients.Sifae,
                client => client.Timeout = TimeSpan.FromSeconds(
                    interoperabilidadOptions.Sifae.TimeoutSeconds));
            services.AddSingleton<GuiaTokenClient>();
            services.AddSingleton<GuiaIdentificacionProvider>();
            services.AddSingleton<SifaeIdentificacionProvider>();
            services.AddSingleton<IProveedorConsultaIdentificacion>(provider =>
                provider.GetRequiredService<GuiaIdentificacionProvider>());
            services.AddSingleton<IProveedorConsultaIdentificacion>(provider =>
                provider.GetRequiredService<SifaeIdentificacionProvider>());
            services.AddSingleton<ConstanciaVerificacionIdentificacionStore>();
            services.AddSingleton<IConstanciaVerificacionIdentificacionStore>(
                provider => provider.GetRequiredService<
                    ConstanciaVerificacionIdentificacionStore>());
            services.AddSingleton<IConsultaIdentificacionService,
                ConsultaIdentificacionService>();

            // ViewModels
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<VentasViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<SeleccionarEmpresaViewModel>();
            services.AddTransient<SeleccionarEstablecimientoViewModel>();

            // Ventanas
            services.AddSingleton<MainWindow>();
            services.AddTransient<SeleccionarEmpresaWindow>();
            services.AddTransient<SeleccionarEstablecimientoWindow>();

            // Secretos y Auth
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<CurrentSession>();

            services.AddSingleton<IUsuarioEmpresaService, UsuarioEmpresaService>();

            services.AddSingleton<StructuralSeeder>();
            services.AddSingleton<DemoSeeder>();

            _serviceProvider = services.BuildServiceProvider();
        }

        private static InteroperabilidadOptions CreateInteroperabilidadOptions(
            IConfiguration configuration)
        {
            var guia = new GuiaOptions
            {
                TokenUrl = configuration["Interoperabilidad:Guia:TokenUrl"] ??
                    string.Empty,
                ServicioUrl =
                    configuration["Interoperabilidad:Guia:ServicioUrl"] ??
                    string.Empty,
                ClientId = Environment.GetEnvironmentVariable(
                    "KONTAXPRO_GUIA_CLIENT_ID") ??
                    configuration["Interoperabilidad:Guia:ClientId"] ??
                    string.Empty,
                ClientSecret = Environment.GetEnvironmentVariable(
                    "KONTAXPRO_GUIA_CLIENT_SECRET") ??
                    configuration["Interoperabilidad:Guia:ClientSecret"] ??
                    string.Empty,
                TimeoutSeconds = ParseTimeout(
                    configuration["Interoperabilidad:Guia:TimeoutSeconds"])
            };
            var sifae = new SifaeOptions
            {
                BaseUrl = configuration["Interoperabilidad:Sifae:BaseUrl"] ??
                    string.Empty,
                TimeoutSeconds = ParseTimeout(
                    configuration["Interoperabilidad:Sifae:TimeoutSeconds"])
            };
            return new InteroperabilidadOptions
            {
                Guia = guia,
                Sifae = sifae
            };
        }

        private static int ParseTimeout(string? value) =>
            int.TryParse(value, out var seconds)
                ? Math.Clamp(seconds, 5, 60)
                : 15;

        protected override async void OnStartup(
    StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Cargar la preferencia visual guardada
                var themeService =
                    _serviceProvider.GetRequiredService<
                        ThemeService>();

                themeService.Initialize();

                /*
                 * Durante el Login y la selección de empresa todavía no
                 * existe una MainWindow visible. Por eso el cierre debe ser
                 * controlado explícitamente.
                 */
                ShutdownMode =
                    ShutdownMode.OnExplicitShutdown;

                var environmentName =
                    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                    ?? Environment.GetEnvironmentVariable(
                        "ASPNETCORE_ENVIRONMENT");
                var initializeDevelopmentDatabase = e.Args.Any(x =>
                    string.Equals(x, "--initialize-development-database",
                        StringComparison.OrdinalIgnoreCase));
                if (initializeDevelopmentDatabase &&
                    !string.Equals(environmentName, "Development",
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "La inicialización automática solo está disponible en Development.");

                // Verificar conexión con PostgreSQL
                var dbContextFactory =
                    _serviceProvider.GetRequiredService<
                        IDbContextFactory<KontaxDbContext>>();

                await using var context =
                    await dbContextFactory.CreateDbContextAsync();

                if (initializeDevelopmentDatabase)
                    await context.Database.MigrateAsync();

                var puedeConectar =
                    await context.Database.CanConnectAsync();

                if (!puedeConectar)
                {
                    var messageService =
                        _serviceProvider.GetRequiredService<IMessageDialogService>();

                    await messageService.ShowErrorAsync(
                        "KONTAXPRO · Conexión no disponible",
                        "No fue posible establecer conexión con PostgreSQL.",
                        "Verifica que el servidor esté disponible e intenta nuevamente.");

                    Shutdown();
                    return;
                }

                // Inicializar datos base temporales
                var seeder =
                    _serviceProvider.GetRequiredService<
                        StructuralSeeder>();

                await seeder.SeedAsync();

                if (string.Equals(
                        environmentName,
                        "Development",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var demoSeeder =
                        _serviceProvider.GetRequiredService<DemoSeeder>();

                    await demoSeeder.SeedAsync(environmentName);
                }

                if (initializeDevelopmentDatabase)
                {
                    Shutdown(0);
                    return;
                }

                _sriWorkerScheduler = _serviceProvider.GetRequiredService<
                    IFacturacionElectronicaWorkerScheduler>();
                await _sriWorkerScheduler.IniciarAsync();

                // Ejecutar Login y selección automática/manual de empresa
                var sessionFlowService =
                    _serviceProvider.GetRequiredService<
                        SessionFlowService>();

                var accesoCorrecto =
                    await sessionFlowService.IniciarSesionAsync();

                if (!accesoCorrecto)
                {
                    Shutdown();
                    return;
                }

                // Abrir ventana principal
                var mainWindow =
                    _serviceProvider.GetRequiredService<
                        MainWindow>();

                MainWindow = mainWindow;

                ShutdownMode =
                    ShutdownMode.OnMainWindowClose;

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                var messageService =
                    _serviceProvider.GetRequiredService<IMessageDialogService>();

                await messageService.ShowErrorAsync(
                    "Error de inicialización",
                    "KONTAXPRO no pudo completar el inicio.",
                    "Cierra la aplicación, verifica la configuración e intenta nuevamente.");

                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _sriWorkerScheduler?.DetenerAsync()
                    .GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
            }
            base.OnExit(e);
        }

    }
}

using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;
using KONTAXPRO.Desktop.ViewModels;
using KONTAXPRO.Desktop.ViewModels.Products;
using KONTAXPRO.Desktop.ViewModels.Clientes;
using KONTAXPRO.Desktop.ViewModels.Proveedores;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace KONTAXPRO.Desktop
{
    public partial class App : System.Windows.Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

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
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductCatalogService, ProductCatalogService>();
            services.AddTransient<IClienteService, ClienteService>();
            services.AddTransient<IProveedorService, ProveedorService>();
            services.AddTransient<IEstadoComprobanteElectronicoService,
                EstadoComprobanteElectronicoService>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<ProductFormViewModel>();
            services.AddTransient<ClienteFormViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<ClientesView>();
            services.AddTransient<ProveedorFormViewModel>();
            services.AddTransient<ProveedoresViewModel>();
            services.AddTransient<ProveedoresView>();

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
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<ProductsView>();

            // Ventanas
            services.AddSingleton<MainWindow>();
            services.AddTransient<SeleccionarEmpresaWindow>();

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

                // Verificar conexión con PostgreSQL
                var dbContextFactory =
                    _serviceProvider.GetRequiredService<
                        IDbContextFactory<KontaxDbContext>>();

                await using var context =
                    await dbContextFactory.CreateDbContextAsync();

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

                var environmentName =
                    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                if (string.Equals(
                        environmentName,
                        "Development",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var demoSeeder =
                        _serviceProvider.GetRequiredService<DemoSeeder>();

                    await demoSeeder.SeedAsync(environmentName);
                }

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

    }
}

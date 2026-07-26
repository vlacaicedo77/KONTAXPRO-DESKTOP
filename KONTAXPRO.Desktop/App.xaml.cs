using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;
using KONTAXPRO.Desktop.ViewModels;
using KONTAXPRO.Desktop.ViewModels.Products;
using KONTAXPRO.Desktop.Views;
using KONTAXPRO.Desktop.Views.Products;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using KONTAXPRO.Infrastructure.Products;
using KONTAXPRO.Infrastructure.Security;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
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
                .Build();

            var services = new ServiceCollection();

            // Registrar configuración
            services.AddSingleton(_configuration);

            // Registrar conexión a PostgreSQL mediante DbContextFactory
            services.AddDbContextFactory<KontaxDbContext>(options =>
                options.UseNpgsql(
                    _configuration.GetConnectionString("DefaultConnection")));

            // Servicios
            services.AddSingleton<ThemeService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<SessionFlowService>();
            services.AddTransient<IInventoryService, InventoryService>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductCatalogService, ProductCatalogService>();
            services.AddTransient<IEstadoComprobanteElectronicoService,
                EstadoComprobanteElectronicoService>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<ProductFormViewModel>();

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
                    MessageBox.Show(
                        "No fue posible establecer conexión con PostgreSQL.\n\n" +
                        "Verifica que el servidor esté disponible e intenta nuevamente.",
                        "KONTAXPRO · Conexión no disponible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

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
                MessageBox.Show(
                    $"KONTAXPRO no pudo completar el inicio.\n\n{ex.Message}",
                    "Error de inicialización",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }

    }
}

using System.Windows;
using System.Reflection;
using KONTAXPRO.Desktop;
using KONTAXPRO.Desktop.ViewModels.FacturacionElectronica;
using KONTAXPRO.Desktop.Views.FacturacionElectronica;
using KONTAXPRO.Application.Session;
using Microsoft.Extensions.DependencyInjection;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class FacturacionElectronicaViewSmokeTests
{
    [Fact]
    public void Vista_de_configuracion_puede_cargarse_y_medirse()
    {
        var failure = WpfTestHost.Run(() =>
        {
            var view = new FacturacionElectronicaConfiguracionView
            {
                Width = 1280,
                Height = 820
            };
            view.Measure(new Size(1280, 820));
            view.Arrange(new Rect(0, 0, 1280, 820));
            view.UpdateLayout();
        });

        Assert.Null(failure);
    }

    [Fact]
    public void ViewModel_de_configuracion_puede_resolverse_desde_el_contenedor_real()
    {
        FacturacionElectronicaConfiguracionViewModel? viewModel = null;
        var failure = WpfTestHost.Run(() =>
        {
            var app = Assert.IsType<App>(System.Windows.Application.Current);
            var providerField = typeof(App).GetField("_serviceProvider",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var provider = Assert.IsAssignableFrom<IServiceProvider>(
                providerField?.GetValue(app));
            viewModel = provider.GetRequiredService<
                FacturacionElectronicaConfiguracionViewModel>();
        });

        Assert.Null(failure);
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void Ambiente_no_guardado_bloquea_diagnostico_y_se_muestra_como_pendiente()
    {
        var failure = WpfTestHost.Run(() =>
        {
            var app = Assert.IsType<App>(System.Windows.Application.Current);
            var providerField = typeof(App).GetField("_serviceProvider",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var provider = Assert.IsAssignableFrom<IServiceProvider>(
                providerField?.GetValue(app));
            var session = provider.GetRequiredService<CurrentSession>();
            var permisosAnteriores = session.Permisos.ToList();
            try
            {
                session.Permisos =
                [
                    "SRI_CONFIGURAR_FACTURACION",
                    "SRI_CAMBIAR_AMBIENTE",
                    "SRI_EJECUTAR_DIAGNOSTICO"
                ];
                using var viewModel = provider.GetRequiredService<
                    FacturacionElectronicaConfiguracionViewModel>();

                Assert.True(viewModel.PuedeDiagnosticarContexto);
                viewModel.EsProduccion = true;
                Assert.True(viewModel.HayCambiosSinGuardar);
                Assert.False(viewModel.PuedeDiagnosticarContexto);
                Assert.Equal("CAMBIOS SIN GUARDAR", viewModel.EstadoGeneral);

                viewModel.EsProduccion = false;
                Assert.False(viewModel.HayCambiosSinGuardar);
                Assert.True(viewModel.PuedeDiagnosticarContexto);
            }
            finally
            {
                session.Permisos = permisosAnteriores;
            }
        });

        Assert.Null(failure);
    }
}

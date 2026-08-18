using System.Windows;
using System.Windows.Controls;
using KONTAXPRO.Desktop.ViewModels.FacturacionElectronica;
using Microsoft.Win32;

namespace KONTAXPRO.Desktop.Views.FacturacionElectronica;

public partial class FacturacionElectronicaConfiguracionView : UserControl
{
    public FacturacionElectronicaConfiguracionView() => InitializeComponent();

    private async void SelectCertificate_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FacturacionElectronicaConfiguracionViewModel vm)
            return;
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar certificado electrónico",
            Filter = "Certificados PKCS#12 (*.p12;*.pfx)|*.p12;*.pfx",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() != true) return;
        var passwordDialog = new CertificatePasswordWindow();
        passwordDialog.Owner = Window.GetWindow(this);
        if (passwordDialog.ShowDialog() == true)
            await vm.ImportarCertificadoAsync(dialog.FileName,
                passwordDialog.CertificatePassword);
    }
}

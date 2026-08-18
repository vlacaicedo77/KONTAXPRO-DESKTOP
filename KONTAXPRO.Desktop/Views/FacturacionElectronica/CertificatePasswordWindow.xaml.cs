using System.Windows;

namespace KONTAXPRO.Desktop.Views.FacturacionElectronica;

public partial class CertificatePasswordWindow : Window
{
    public CertificatePasswordWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordInput.Focus();
    }
    public string CertificatePassword => PasswordInput.Password;
    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordInput.Password)) return;
        DialogResult = true;
    }
}

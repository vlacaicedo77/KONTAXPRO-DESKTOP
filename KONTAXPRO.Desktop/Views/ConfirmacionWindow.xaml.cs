using System.Windows;

namespace KONTAXPRO.Desktop.Views;

public partial class ConfirmacionWindow : Window
{
    public string NombreUsuario { get; }

    public string Empresa { get; }

    public ConfirmacionWindow(
        string nombreUsuario,
        string empresa)
    {
        InitializeComponent();

        NombreUsuario = nombreUsuario;
        Empresa = empresa;

        DataContext = this;
    }

    private void Cancelar_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirmar_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
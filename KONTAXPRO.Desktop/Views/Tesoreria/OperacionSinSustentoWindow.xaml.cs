using System.Windows;
using System.Windows.Controls;
using KONTAXPRO.Desktop.ViewModels.Tesoreria;
using Microsoft.Win32;

namespace KONTAXPRO.Desktop.Views.Tesoreria;

public partial class OperacionSinSustentoWindow : UserControl
{
    private OperacionSinSustentoViewModel? _viewModel;

    public OperacionSinSustentoWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Unsubscribe();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Unsubscribe();
        _viewModel = e.NewValue as OperacionSinSustentoViewModel;
        if (_viewModel is not null)
            _viewModel.SelectEvidenceRequested += SelectEvidenceAsync;
    }

    private Task SelectEvidenceAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Adjuntar soporte opcional",
            Filter = "Soportes (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
            if (_viewModel is not null) _viewModel.EvidencePath = dialog.FileName;
        return Task.CompletedTask;
    }

    private void Unsubscribe()
    {
        if (_viewModel is not null)
            _viewModel.SelectEvidenceRequested -= SelectEvidenceAsync;
        _viewModel = null;
    }

}

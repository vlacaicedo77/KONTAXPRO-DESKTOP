using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using KONTAXPRO.Desktop;
using KONTAXPRO.Desktop.ViewModels.Compras;
using KONTAXPRO.Desktop.Views.Compras;
using Xunit;

namespace KONTAXPRO.Tests.Compras;

public sealed class ComprasTraceabilityViewSmokeTests
{
    [Fact]
    public void ComplementaryEditors_CanBeMeasuredWhenOpened()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                var view = new ComprasView
                {
                    Width = 1280,
                    Height = 820,
                    DataContext = new TraceabilitySmokeModel()
                };
                view.Measure(new Size(1280, 820));
                view.Arrange(new Rect(0, 0, 1280, 820));
                view.UpdateLayout();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    private sealed class TraceabilitySmokeModel
    {
        public TraceabilitySmokeModel()
        {
            ExistingReceiptLots.Add(new KONTAXPRO.Application.Models.Inventario.EstadoControlLoteDto
            {
                LoteId = 1,
                NumeroLote = "LOTE-1",
                StockActual = 24
            });
        }

        public bool IsTraceabilityEditorOpen => true;
        public bool IsManualFormOpen => true;
        public bool ManualIsInvoiced => true;
        public bool ManualIsCredit { get; set; }
        public string ManualPurchaseType { get; set; } = "FACTURADA";
        public string ManualSupplierSearchText { get; set; } = "Proveedor demo";
        public bool ShowManualSupplierSuggestions { get; set; }
        public string ManualDocumentNumber { get; set; } = "001-001-000000001";
        public string ManualDocumentEstablishment { get; set; } = "001";
        public string ManualDocumentEmissionPoint { get; set; } = "001";
        public string ManualDocumentSequential { get; set; } = "000000001";
        public DateTime ManualIssueDate { get; set; } = DateTime.Today;
        public DateTime? ManualDueDate { get; set; }
        public string? ManualObservation { get; set; }
        public decimal ManualSubtotal => 12;
        public decimal ManualDiscountTotal => 0;
        public decimal ManualTaxTotal => 1.8m;
        public decimal ManualTotal => 13.8m;
        public ObservableCollection<CompraManualLineViewModel> ManualLines { get; }
            = [new() { Description = "Línea manual", Quantity = 1, UnitPrice = 12 }];
        public bool TraceabilityHandlesLots => true;
        public bool TraceabilityHandlesSeries => false;
        public decimal TraceabilityRequiredBase => 10;
        public decimal TraceabilityAssignedLots => 10;
        public decimal TraceabilityPendingLots => 0;
        public int TraceabilityAssignedSeries => 0;
        public int TraceabilityPendingSeries => 0;
        public decimal TraceabilityAssignedControl => 10;
        public decimal TraceabilityPendingControl => 0;
        public string? TraceabilityError => null;
        public ObservableCollection<CompraReceiptLotEditorViewModel>
            TraceabilityLots { get; } = [new() { Number = "LOTE-1", QuantityBase = 10 }];
        public ObservableCollection<CompraReceiptSeriesEditorViewModel>
            TraceabilitySeries { get; } = [];
        public ObservableCollection<KONTAXPRO.Application.Models.Inventario.EstadoControlLoteDto>
            ExistingReceiptLots { get; } = [];
    }
}

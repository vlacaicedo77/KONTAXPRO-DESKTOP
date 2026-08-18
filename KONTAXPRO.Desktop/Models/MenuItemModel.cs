using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace KONTAXPRO.Desktop.Models
{
    public partial class MenuItemModel : ObservableObject
    {
        public string Titulo { get; set; } = string.Empty;

        public string Icono { get; set; } = string.Empty;

        public string? Permiso { get; set; }

        public string? ComandoNavegacion { get; set; }

        public ObservableCollection<MenuItemModel> Hijos { get; set; } = new();

        public bool TieneHijos => Hijos.Count > 0;

        [ObservableProperty]
        private bool estaExpandido;

        [ObservableProperty]
        private bool estaActivo;

        [ObservableProperty]
        private bool esVisible = true;
    }
}

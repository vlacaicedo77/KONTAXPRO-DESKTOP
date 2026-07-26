using System.Windows;
using System.Windows.Controls;
using KONTAXPRO.Desktop.Models;
using KONTAXPRO.Desktop.ViewModels;

namespace KONTAXPRO.Desktop.Controls
{
    public partial class SidebarMenuItem : UserControl
    {
        public SidebarMenuItem()
        {
            InitializeComponent();
        }

        private void MenuButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not MenuItemModel menuItem)
                return;

            if (!menuItem.TieneHijos)
                return;


            /*
             * Guardamos el estado actual antes de cerrar
             * los demás módulos.
             *
             * Esto permite que al hacer clic nuevamente
             * sobre el mismo padre también pueda cerrarse.
             */
            var estabaExpandido =
                menuItem.EstaExpandido;


            /*
             * Obtenemos el MainViewModel de la ventana
             * principal.
             */
            if (Window.GetWindow(this)?.DataContext
                is MainViewModel mainViewModel)
            {
                /*
                 * Cerramos todos los módulos principales.
                 */
                foreach (var item in mainViewModel.MenuItems)
                {
                    if (item.TieneHijos)
                    {
                        item.EstaExpandido = false;
                    }
                }
            }


            /*
             * Si estaba cerrado, lo abrimos.
             *
             * Si ya estaba abierto, permanece cerrado.
             */
            menuItem.EstaExpandido =
                !estabaExpandido;


            /*
             * Evitamos que el clic siga propagándose.
             */
            e.Handled = true;
        }
    }
}
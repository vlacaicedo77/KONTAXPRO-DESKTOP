using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Models;
using KONTAXPRO.Desktop.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace KONTAXPRO.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ThemeService _themeService;
        private readonly INavigationService _navigationService;
        private readonly CurrentSession _currentSession;
        private readonly ILoadingService _loadingService;
        private readonly IMessageDialogService _messageDialogService;

        public event Action? CambioEmpresaRequested;
        public event Action? CerrarSesionRequested;

        [RelayCommand]
        private void CambiarEmpresa()
        {
            CambioEmpresaRequested?.Invoke();
        }

        [RelayCommand]
        private void CerrarSesion()
        {
            CerrarSesionRequested?.Invoke();
        }

        [ObservableProperty]
        private ObservableObject? currentViewModel;

        [ObservableProperty]
        private string moduloActivo = "Inicio";

        [ObservableProperty]
        private string usuarioConectado = string.Empty;

        [ObservableProperty]
        private string empresaActiva = string.Empty;

        [ObservableProperty]
        private string establecimientoActivo = string.Empty;

        [ObservableProperty]
        private string identificacionUsuario = string.Empty;

        public ObservableCollection<MenuItemModel> MenuItems { get; }

        [ObservableProperty]
        private bool mostrarCambiarEmpresa;

        public MainViewModel(
            ThemeService themeService,
            INavigationService navigationService,
            CurrentSession currentSession,
            ILoadingService loadingService,
            IMessageDialogService messageDialogService)
        {
            _themeService = themeService;
            _navigationService = navigationService;
            _currentSession = currentSession;
            _loadingService = loadingService;
            _messageDialogService = messageDialogService;

            // Cargar información del contexto actual del usuario
            ActualizarContexto();

            // Crear menú principal
            MenuItems = CrearMenu();

            var inicio = MenuItems
                .FirstOrDefault(x =>
                    x.ComandoNavegacion == "Inicio");

            if (inicio != null)
            {
                inicio.EstaActivo = true;
            }

            // Escuchar cambios de navegación
            _navigationService.CurrentViewModelChanged += () =>
            {
                CurrentViewModel =
                    _navigationService.CurrentViewModel;
            };

            // Navegar inicialmente al Dashboard
            _navigationService.NavigateTo("Inicio");
        }

        [RelayCommand]
        private async Task NavegarAsync(MenuItemModel item)
        {
            if (item == null)
                return;

            if (string.IsNullOrWhiteSpace(
                item.ComandoNavegacion))
            {
                return;
            }

            if (string.Equals(
                    _navigationService.CurrentRoute,
                    item.ComandoNavegacion,
                    StringComparison.OrdinalIgnoreCase))
                return;


            /*
             * Quitar la selección anterior.
             */
            DesactivarTodos(MenuItems);


            /*
             * Marcar la nueva opción activa.
             */
            item.EstaActivo = true;


            /*
             * Determinar a qué módulo pertenece.
             */
            var padre =
                BuscarPadre(
                    MenuItems,
                    item);


            /*
             * Si navegamos hacia un hijo,
             * dejamos abierto únicamente su padre.
             */
            if (padre != null)
            {
                foreach (var menu in MenuItems)
                {
                    menu.EstaExpandido =
                        ReferenceEquals(
                            menu,
                            padre);
                }
            }


            /*
             * Actualizar módulo activo.
             */
            ModuloActivo =
                item.ComandoNavegacion;


            /*
             * Navegar.
             */
            try
            {
                var loadingMessage = ObtenerMensajeCarga(
                    item.ComandoNavegacion);
                if (loadingMessage.HasValue)
                {
                    await using var loading = await _loadingService.ShowAsync(
                        loadingMessage.Value.Title,
                        loadingMessage.Value.Message);
                    await _navigationService.NavigateToAsync(
                        item.ComandoNavegacion);
                    return;
                }

                await _navigationService.NavigateToAsync(
                    item.ComandoNavegacion);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
                await _messageDialogService.ShowErrorAsync(
                    "No fue posible abrir la opción",
                    "La pantalla no pudo cargarse. Puedes volver a intentarlo.");
            }
        }

        private static (string Title, string Message)? ObtenerMensajeCarga(
            string route) => route switch
            {
                "Productos" => ("Cargando productos",
                    "Estamos preparando el catálogo y sus precios."),
                "Clientes" => ("Cargando clientes",
                    "Estamos preparando el listado y su configuración comercial."),
                "Proveedores" => ("Cargando proveedores",
                    "Estamos preparando el listado de proveedores."),
                "OperacionesSinComprobante" => ("Cargando operaciones",
                    "Estamos preparando las operaciones registradas."),
                "Inventario" => ("Cargando inventario",
                    "Estamos consultando existencias, bodegas y costos."),
                "Compras/Xml" => null,
                _ when route.StartsWith("Compras",
                    StringComparison.OrdinalIgnoreCase) =>
                    ("Cargando compras",
                        "Estamos preparando la información de compras."),
                _ => null
            };

        private MenuItemModel? BuscarPadre(
    IEnumerable<MenuItemModel> items,
    MenuItemModel seleccionado)
        {
            foreach (var menuItem in items)
            {
                if (menuItem.Hijos.Contains(
                    seleccionado))
                {
                    return menuItem;
                }

                if (menuItem.Hijos.Count > 0)
                {
                    var padre =
                        BuscarPadre(
                            menuItem.Hijos,
                            seleccionado);

                    if (padre != null)
                    {
                        return padre;
                    }
                }
            }

            return null;
        }

        private void DesactivarTodos(
            IEnumerable<MenuItemModel> items)
        {
            foreach (var menuItem in items)
            {
                menuItem.EstaActivo = false;

                if (menuItem.Hijos.Count > 0)
                {
                    DesactivarTodos(
                        menuItem.Hijos);
                }
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
        }

        public void ActualizarContexto()
        {
            UsuarioConectado =
                _currentSession.NombreCompleto;

            IdentificacionUsuario =
                _currentSession.NumeroIdentificacion;

            EmpresaActiva =
                _currentSession.RazonSocial
                ?? "Sin empresa seleccionada";

            EstablecimientoActivo =
                _currentSession.EstablecimientoId.HasValue
                    ? $"[{_currentSession.EstablecimientoCodigo}] " +
                      $"{_currentSession.EstablecimientoNombre}"
                    : "Sin establecimiento configurado";
            MostrarCambiarEmpresa =
                _currentSession.PuedeCambiarEmpresa;
        }

        private ObservableCollection<MenuItemModel> CrearMenu()
        {
            return new ObservableCollection<MenuItemModel>
    {
        /*
         * =========================================================
         * INICIO
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Inicio",
            Icono = "Home",
            ComandoNavegacion = "Inicio"
        },

        /*
         * =========================================================
         * INVENTARIO
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Inventario",
            Icono = "PackageVariantClosed",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Productos",
                    Icono = "PackageVariant",
                    ComandoNavegacion = "Productos"
                },

                new MenuItemModel
                {
                    Titulo = "Categorías",
                    Icono = "Shape"
                },

                new MenuItemModel
                {
                    Titulo = "Marcas",
                    Icono = "Tag"
                },

                new MenuItemModel
                {
                    Titulo = "Unidades de medida",
                    Icono = "RulerSquare"
                },

                new MenuItemModel
                {
                    Titulo = "Bodegas",
                    Icono = "Warehouse"
                },

                new MenuItemModel
                {
                    Titulo = "Inventario y Kardex",
                    Icono = "PackageVariantClosedCheck",
                    ComandoNavegacion = "Inventario"
                }
            }
        },

        /*
         * =========================================================
         * COMPRAS
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Compras",
            Icono = "CartArrowDown",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Gestión de compras",
                    Icono = "ClipboardTextClockOutline",
                    ComandoNavegacion = "Compras/Lista"
                },

                new MenuItemModel
                {
                    Titulo = "Operaciones sin comprobante",
                    Icono = "ReceiptTextRemoveOutline",
                    Permiso = "TESORERIA_REGISTRAR_SIN_SUSTENTO",
                    ComandoNavegacion = "OperacionesSinComprobante"
                },

                new MenuItemModel
                {
                    Titulo = "Proveedores",
                    Icono = "Truck",
                    ComandoNavegacion = "Proveedores"
                },

                new MenuItemModel
                {
                    Titulo = "Liquidaciones de compra",
                    Icono = "FileDocumentEditOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Retenciones",
                    Icono = "PercentBoxOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Devoluciones en compra",
                    Icono = "PackageVariantMinus"
                },

                new MenuItemModel
                {
                    Titulo = "Documentos pendientes",
                    Icono = "FileClockOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Diferencias de recepción",
                    Icono = "AlertDecagramOutline"
                }
            }
        },

        /*
         * =========================================================
         * VENTAS
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Ventas",
            Icono = "Cart",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Nueva venta",
                    Icono = "CartPlus",
                    ComandoNavegacion = "Ventas"
                },

                new MenuItemModel
                {
                    Titulo = "Ventas registradas",
                    Icono = "ClipboardTextClock"
                },

                new MenuItemModel
                {
                    Titulo = "Facturación electrónica",
                    Icono = "ReceiptTextCheck"
                },

                new MenuItemModel
                {
                    Titulo = "Clientes",
                    Icono = "AccountGroup",
                    ComandoNavegacion = "Clientes"
                },

                new MenuItemModel
                {
                    Titulo = "Proformas",
                    Icono = "FileDocumentEdit"
                },

                new MenuItemModel
                {
                    Titulo = "Notas de crédito",
                    Icono = "ReceiptTextMinus"
                },

                new MenuItemModel
                {
                    Titulo = "Notas de débito",
                    Icono = "ReceiptTextPlus"
                },

                new MenuItemModel
                {
                    Titulo = "Guías de remisión",
                    Icono = "TruckFastOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Devoluciones en venta",
                    Icono = "CartRemove"
                },

                new MenuItemModel
                {
                    Titulo = "Documentos no autorizados",
                    Icono = "FileAlertOutline"
                }
            }
        },

        /*
         * =========================================================
         * CARTERA
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Cartera",
            Icono = "WalletOutline",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Cuentas por cobrar",
                    Icono = "CashClock"
                },

                new MenuItemModel
                {
                    Titulo = "Registrar cobro",
                    Icono = "CashPlus"
                },

                new MenuItemModel
                {
                    Titulo = "Anticipos de clientes",
                    Icono = "AccountCashOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Estados de cuenta",
                    Icono = "FileAccountOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Cartera vencida",
                    Icono = "CalendarAlert"
                },

                new MenuItemModel
                {
                    Titulo = "Cuentas por pagar",
                    Icono = "CashMinus"
                },

                new MenuItemModel
                {
                    Titulo = "Registrar pago",
                    Icono = "CashCheck"
                },

                new MenuItemModel
                {
                    Titulo = "Anticipos a proveedores",
                    Icono = "TruckCheckOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Programación de pagos",
                    Icono = "CalendarClock"
                }
            }
        },

        /*
         * =========================================================
         * CAJA Y BANCOS
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Caja y Bancos",
            Icono = "BankOutline",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Apertura de caja",
                    Icono = "CashRegister"
                },

                new MenuItemModel
                {
                    Titulo = "Movimientos de caja",
                    Icono = "CashMultiple"
                },

                new MenuItemModel
                {
                    Titulo = "Arqueo y cierre",
                    Icono = "ClipboardCheckOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Cuentas bancarias",
                    Icono = "Bank"
                },

                new MenuItemModel
                {
                    Titulo = "Depósitos",
                    Icono = "BankTransferIn"
                },

                new MenuItemModel
                {
                    Titulo = "Transferencias bancarias",
                    Icono = "BankTransfer"
                },

                new MenuItemModel
                {
                    Titulo = "Conciliación bancaria",
                    Icono = "ScaleBalance"
                },

                new MenuItemModel
                {
                    Titulo = "Formas de pago",
                    Icono = "CreditCardOutline"
                }
            }
        },

        /*
         * =========================================================
         * CONTABILIDAD
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Contabilidad",
            Icono = "CalculatorVariantOutline",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Plan de cuentas",
                    Icono = "FileTreeOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Asientos contables",
                    Icono = "BookOpenPageVariantOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Diario general",
                    Icono = "BookOpenVariant"
                },

                new MenuItemModel
                {
                    Titulo = "Mayor general",
                    Icono = "BookMultipleOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Balance de comprobación",
                    Icono = "ScaleBalance"
                },

                new MenuItemModel
                {
                    Titulo = "Estado de resultados",
                    Icono = "ChartLine"
                },

                new MenuItemModel
                {
                    Titulo = "Situación financiera",
                    Icono = "ChartBoxOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Períodos contables",
                    Icono = "CalendarRange"
                },

                new MenuItemModel
                {
                    Titulo = "Cierre contable",
                    Icono = "LockCheckOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Configuración contable",
                    Icono = "CogOutline"
                }
            }
        },

        /*
         * =========================================================
         * SRI Y TRIBUTACIÓN
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "SRI y Tributación",
            Icono = "ShieldCheckOutline",
            Hijos =
            {
                /*
                 * Todos estos documentos se implementarán sobre
                 * el mismo motor de generación, firma y autorización.
                 */
                new MenuItemModel
                {
                    Titulo = "Factura electrónica",
                    Icono = "ReceiptTextCheck"
                },

                new MenuItemModel
                {
                    Titulo = "Liquidación de compra",
                    Icono = "FileDocumentEditOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Nota de crédito",
                    Icono = "ReceiptTextMinus"
                },

                new MenuItemModel
                {
                    Titulo = "Nota de débito",
                    Icono = "ReceiptTextPlus"
                },

                new MenuItemModel
                {
                    Titulo = "Guía de remisión",
                    Icono = "TruckFastOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Comprobante de retención",
                    Icono = "PercentBoxOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Documentos emitidos",
                    Icono = "FileSendOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Documentos recibidos",
                    Icono = "FileDownloadOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Documentos pendientes",
                    Icono = "FileClockOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Documentos rechazados",
                    Icono = "FileCancelOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Anulación de comprobantes",
                    Icono = "FileUndoOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Libro de compras",
                    Icono = "BookArrowDownOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Libro de ventas",
                    Icono = "BookArrowUpOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Generar ATS",
                    Icono = "FileCodeOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Resumen de IVA",
                    Icono = "PercentOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Resumen de retenciones",
                    Icono = "Finance"
                },

                new MenuItemModel
                {
                    Titulo = "Validaciones tributarias",
                    Icono = "ShieldSearchOutline"
                }
            }
        },

        /*
         * =========================================================
         * REPORTES
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Reportes",
            Icono = "ChartBox",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Inventario",
                    Icono = "PackageVariant"
                },

                new MenuItemModel
                {
                    Titulo = "Compras",
                    Icono = "CartArrowDown"
                },

                new MenuItemModel
                {
                    Titulo = "Ventas",
                    Icono = "CartArrowUp"
                },

                new MenuItemModel
                {
                    Titulo = "Rentabilidad",
                    Icono = "ChartLineVariant"
                },

                new MenuItemModel
                {
                    Titulo = "Cuentas por cobrar",
                    Icono = "CashClock"
                },

                new MenuItemModel
                {
                    Titulo = "Cuentas por pagar",
                    Icono = "CashMinus"
                },

                new MenuItemModel
                {
                    Titulo = "Caja",
                    Icono = "CashRegister"
                },

                new MenuItemModel
                {
                    Titulo = "Impuestos",
                    Icono = "PercentOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Auditoría",
                    Icono = "ClipboardSearchOutline"
                }
            }
        },

        /*
         * =========================================================
         * ADMINISTRACIÓN
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Administración",
            Icono = "Cog",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Empresa",
                    Icono = "OfficeBuildingCog"
                },

                new MenuItemModel
                {
                    Titulo = "Establecimientos",
                    Icono = "StoreCogOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Puntos de emisión",
                    Icono = "PointOfSale"
                },

                new MenuItemModel
                {
                    Titulo = "Bodegas",
                    Icono = "Warehouse"
                },

                new MenuItemModel
                {
                    Titulo = "Usuarios",
                    Icono = "AccountMultiple"
                },

                new MenuItemModel
                {
                    Titulo = "Roles",
                    Icono = "ShieldAccount"
                },

                new MenuItemModel
                {
                    Titulo = "Permisos",
                    Icono = "ShieldKeyOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Certificado digital",
                    Icono = "CertificateOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Parámetros",
                    Icono = "TuneVariant"
                },

                new MenuItemModel
                {
                    Titulo = "Auditoría del sistema",
                    Icono = "History"
                },

                new MenuItemModel
                {
                    Titulo = "Backup y restauración",
                    Icono = "BackupRestore"
                }
            }
        },

        /*
         * =========================================================
         * AYUDA
         * =========================================================
         */
        new MenuItemModel
        {
            Titulo = "Ayuda",
            Icono = "HelpCircleOutline",
            Hijos =
            {
                new MenuItemModel
                {
                    Titulo = "Centro de ayuda",
                    Icono = "Lifebuoy"
                },

                new MenuItemModel
                {
                    Titulo = "Manual de usuario",
                    Icono = "BookOpenOutline"
                },

                new MenuItemModel
                {
                    Titulo = "Soporte técnico",
                    Icono = "Headset"
                },

                new MenuItemModel
                {
                    Titulo = "Acerca de KONTAXPRO",
                    Icono = "InformationOutline"
                }
            }
        }
    };
        }
    }
}

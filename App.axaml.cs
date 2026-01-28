using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Services;
using CasaCejaRemake.ViewModels.Shared;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Views.Shared;
using CasaCejaRemake.Views.POS;

namespace CasaCejaRemake
{
    public partial class App : Application
    {
        // Servicios estaticos
        public static DatabaseService? DatabaseService { get; private set; }
        public static AuthService? AuthService { get; private set; }
        
        // Servicios del POS
        private CartService? _cartService;
        private SalesService? _salesService;
        private CreditService? _creditService;
        private LayawayService? _layawayService;
        private CustomerService? _customerService;
        private CashCloseService? _cashCloseService;

        // Sucursal actual (por defecto 1)
        private int _currentBranchId = 1;
        private string _currentBranchName = "Sucursal Principal";

        // Referencia a la ventana de login actual (para evitar duplicados)
        private LoginView? _currentLoginView;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Inicializar servicios
                await InitializeServicesAsync();
                
                // Mostrar login
                ShowLogin();
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Inicializa todos los servicios de la aplicacion.
        /// </summary>
        private async Task InitializeServicesAsync()
        {
            try
            {
                // Inicializar base de datos
                DatabaseService = new DatabaseService();
                await DatabaseService.InitializeAsync();

                // Inicializar datos por defecto
                var initializer = new DatabaseInitializer(DatabaseService);
                await initializer.InitializeDefaultDataAsync();

                // Inicializar AuthService
                var userRepository = new BaseRepository<Models.User>(DatabaseService);
                AuthService = new AuthService(userRepository);

                // Inicializar servicios del POS
                _cartService = new CartService();
                _salesService = new SalesService(DatabaseService);

                // Inicializar servicio de clientes
                _customerService = new CustomerService(DatabaseService);

                // Inicializar servicios de crédito y apartados
                _creditService = new CreditService(DatabaseService);
                _layawayService = new LayawayService(DatabaseService);

                // Inicializar servicio de cortes de caja
                _cashCloseService = new CashCloseService(DatabaseService);

                Console.WriteLine("[App] Servicios inicializados correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Error inicializando servicios: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Muestra la pantalla de login.
        /// </summary>
        private void ShowLogin(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (AuthService == null) return;

            Console.WriteLine("[App] ShowLogin() llamado");

            // Si ya hay un login abierto, no crear otro
            if (_currentLoginView != null)
            {
                Console.WriteLine("[App] Login ya existe, activando...");
                _currentLoginView.Activate();
                windowToClose?.Close();
                return;
            }

            var loginViewModel = new LoginViewModel(AuthService);
            var loginView = new LoginView
            {
                DataContext = loginViewModel
            };

            // Guardar referencia
            _currentLoginView = loginView;

            loginView.Closed += (sender, args) =>
            {
                Console.WriteLine($"[App] LoginView.Closed disparado. Tag = {loginView.Tag}");
                // Limpiar referencia al cerrar
                _currentLoginView = null;

                if (loginView.Tag is string result && result == "success")
                {
                    Console.WriteLine("[App] Login exitoso, llamando HandleSuccessfulLogin()");
                    HandleSuccessfulLogin();
                }
                else
                {
                    Console.WriteLine("[App] Login cancelado o sin resultado");
                    // Usuario cancelo, cerrar aplicacion
                    if (desktop.MainWindow == null)
                    {
                        desktop.Shutdown();
                    }
                }
            };

            Console.WriteLine("[App] Mostrando LoginView...");
            loginView.Show();
            
            // Cerrar la ventana anterior DESPUÉS de mostrar el login
            windowToClose?.Close();
        }

        /// <summary>
        /// Maneja el login exitoso segun el rol del usuario.
        /// </summary>
        private void HandleSuccessfulLogin()
        {
            if (AuthService?.IsAdmin == true)
            {
                // Admin: Mostrar selector de modulos
                ShowModuleSelector();
            }
            else
            {
                // Cajero: Ir directo al POS
                ShowPOS();
            }
        }

        /// <summary>
        /// Muestra el selector de modulos (solo Admin).
        /// </summary>
        private void ShowModuleSelector()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (AuthService?.CurrentUser == null) return;

            Console.WriteLine("[App] ShowModuleSelector() llamado");

            var selectorViewModel = new ModuleSelectorViewModel(AuthService);
            var selectorView = new ModuleSelectorView
            {
                DataContext = selectorViewModel
            };

            // Suscribirse a eventos - NO cerrar aquí, solo marcar y dejar que ShowPOS maneje todo
            selectorViewModel.POSSelected += (s, e) => 
            {
                Console.WriteLine("[App] POS seleccionado");
                selectorView.Tag = "module_selected";
                // Primero mostrar POS, luego cerrar selector
                ShowPOS(selectorView);
            };

            selectorViewModel.InventorySelected += (s, e) =>
            {
                Console.WriteLine("[App] Inventario seleccionado");
                selectorView.Tag = "module_selected";
                ShowInventory(selectorView);
            };

            selectorViewModel.AdminSelected += (s, e) =>
            {
                Console.WriteLine("[App] Admin seleccionado");
                selectorView.Tag = "module_selected";
                ShowAdmin(selectorView);
            };

            selectorViewModel.LogoutRequested += (s, e) =>
            {
                Console.WriteLine("[App] Logout solicitado");
                selectorView.Tag = "logout";
                AuthService?.Logout();
                // Primero mostrar login, luego cerrar selector
                ShowLogin(selectorView);
            };

            selectorView.Closed += (sender, args) =>
            {
                Console.WriteLine($"[App] ModuleSelector cerrado. Tag = {selectorView.Tag}");
                // Si se cerro sin seleccionar módulo ni logout (ej: botón X), hacer logout
                if (selectorView.Tag == null)
                {
                    Console.WriteLine("[App] Cerrado sin selección, haciendo logout");
                    AuthService?.Logout();
                    ShowLogin();
                }
            };

            Console.WriteLine("[App] Mostrando ModuleSelector");
            selectorView.Show();
        }

        /// <summary>
        /// Muestra el modulo POS.
        /// </summary>
        private async void ShowPOS(Window? windowToClose = null)
        {
            Console.WriteLine("[App] ShowPOS() llamado");
            
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("[App] ERROR: No hay ApplicationLifetime");
                return;
            }
            
            if (AuthService == null || _cartService == null || _salesService == null || _cashCloseService == null)
            {
                Console.WriteLine($"[App] ERROR: Servicios null - Auth:{AuthService != null}, Cart:{_cartService != null}, Sales:{_salesService != null}, CashClose:{_cashCloseService != null}");
                return;
            }

            Console.WriteLine("[App] Creando SalesViewModel...");
            
            // Crear ViewModel con servicios inyectados
            var salesViewModel = new SalesViewModel(
                _cartService,
                _salesService,
                AuthService,
                _currentBranchId,
                _currentBranchName);

            Console.WriteLine("[App] Creando SalesView...");
            
            var salesView = new SalesView
            {
                DataContext = salesViewModel
            };

            // IMPORTANTE: Establecer como MainWindow ANTES de mostrar
            desktop.MainWindow = salesView;
            
            Console.WriteLine("[App] Mostrando SalesView...");
            salesView.Show();
            Console.WriteLine("[App] SalesView mostrada correctamente");
            
            // Cerrar la ventana anterior DESPUÉS de mostrar la nueva
            windowToClose?.Close();

            // Verificar si hay caja abierta
            var openCash = await _cashCloseService.GetOpenCashAsync(_currentBranchId);
            if (openCash == null)
            {
                Console.WriteLine("[App] No hay caja abierta, mostrando modal de apertura...");
                
                var openCashView = new OpenCashView();
                var openCashViewModel = new OpenCashViewModel(_cashCloseService, AuthService, _currentBranchId);
                openCashView.DataContext = openCashViewModel;

                await openCashView.ShowDialog(salesView);

                if (openCashView.Tag is Models.CashClose newCash)
                {
                    Console.WriteLine($"[App] Caja abierta exitosamente: {newCash.Folio}");
                }
                else
                {
                    Console.WriteLine("[App] Apertura de caja cancelada, saliendo del POS...");
                    // Usuario canceló, salir del POS
                    salesView.Tag = "exit";
                    salesView.Close();
                    return;
                }
            }
            else
            {
                Console.WriteLine($"[App] Caja ya abierta: {openCash.Folio}");
            }

            // Manejar salida del POS
            salesView.Closed += (sender, args) =>
            {
                Console.WriteLine($"[App] SalesView cerrada. Tag = {salesView.Tag}");
                if (salesView.Tag is string result && result == "exit")
                {
                    if (AuthService.IsAdmin)
                    {
                        // Admin regresa al selector
                        ShowModuleSelector();
                    }
                    else
                    {
                        // Cajero regresa al login
                        AuthService.Logout();
                        ShowLogin();
                    }
                }
            };
        }


        /// <summary>
        /// Muestra el modulo de Inventario (placeholder).
        /// </summary>
        private void ShowInventory(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

            var placeholderWindow = new Window
            {
                Title = "Inventario - Proximamente",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Avalonia.Media.Brushes.DimGray,
                Content = new TextBlock
                {
                    Text = "Modulo de Inventario\n\nProximamente",
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 18,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                }
            };

            placeholderWindow.Closed += (s, e) =>
            {
                ShowModuleSelector();
            };

            desktop.MainWindow = placeholderWindow;
            placeholderWindow.Show();
            windowToClose?.Close();
        }

        /// <summary>
        /// Muestra el modulo de Administrador (placeholder).
        /// </summary>
        private void ShowAdmin(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

            var placeholderWindow = new Window
            {
                Title = "Administrador - Proximamente",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Avalonia.Media.Brushes.DimGray,
                Content = new TextBlock
                {
                    Text = "Modulo de Administrador\n\nProximamente",
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 18,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                }
            };

            placeholderWindow.Closed += (s, e) =>
            {
                ShowModuleSelector();
            };

            desktop.MainWindow = placeholderWindow;
            placeholderWindow.Show();
            windowToClose?.Close();
        }

        /// <summary>
        /// Obtiene el servicio de ventas (usado por las vistas).
        /// </summary>
        public SalesService? GetSaleService()
        {
            return _salesService;
        }

        /// <summary>
        /// Obtiene el servicio de crédito (usado por las vistas).
        /// </summary>
        public CreditService? GetCreditService()
        {
            return _creditService;
        }

        /// <summary>
        /// Obtiene el servicio de apartados (usado por las vistas).
        /// </summary>
        public LayawayService? GetLayawayService()
        {
            return _layawayService;
        }

        /// <summary>
        /// Obtiene el servicio de clientes (usado por las vistas).
        /// </summary>
        public CustomerService? GetCustomerService()
        {
            return _customerService;
        }

        /// <summary>
        /// Obtiene el servicio de autenticación (usado por las vistas).
        /// </summary>
        public AuthService? GetAuthService()
        {
            return AuthService;
        }

        /// <summary>
        /// Obtiene el servicio de cortes de caja (usado por las vistas).
        /// </summary>
        public CashCloseService? GetCashCloseService()
        {
            return _cashCloseService;
        }
    }
}

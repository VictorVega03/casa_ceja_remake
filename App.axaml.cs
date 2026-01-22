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

        // Sucursal actual (por defecto 1)
        private int _currentBranchId = 1;
        private string _currentBranchName = "Sucursal Principal";

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
        private void ShowLogin()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (AuthService == null) return;

            var loginViewModel = new LoginViewModel(AuthService);
            var loginView = new LoginView
            {
                DataContext = loginViewModel
            };

            loginView.Closed += (sender, args) =>
            {
                if (loginView.Tag is string result && result == "success")
                {
                    HandleSuccessfulLogin();
                }
                else
                {
                    // Usuario cancelo, cerrar aplicacion
                    if (desktop.MainWindow == null)
                    {
                        desktop.Shutdown();
                    }
                }
            };

            loginView.Show();
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

            var selectorViewModel = new ModuleSelectorViewModel(AuthService);
            var selectorView = new ModuleSelectorView
            {
                DataContext = selectorViewModel
            };

            // Suscribirse a eventos
            selectorViewModel.POSSelected += (s, e) => 
            {
                selectorView.Close();
                ShowPOS();
            };

            selectorViewModel.InventorySelected += (s, e) =>
            {
                selectorView.Close();
                ShowInventory();
            };

            selectorViewModel.AdminSelected += (s, e) =>
            {
                selectorView.Close();
                ShowAdmin();
            };

            selectorViewModel.LogoutRequested += (s, e) =>
            {
                AuthService?.Logout();
                selectorView.Close();
                ShowLogin();
            };

            selectorView.Closed += (sender, args) =>
            {
                // Si se cerro sin resultado, logout
                if (selectorView.Tag == null && desktop.MainWindow == null)
                {
                    AuthService?.Logout();
                    ShowLogin();
                }
            };

            selectorView.Show();
        }

        /// <summary>
        /// Muestra el modulo POS.
        /// </summary>
        private void ShowPOS()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (AuthService == null || _cartService == null || _salesService == null) return;

            // Crear ViewModel con servicios inyectados
            var salesViewModel = new SalesViewModel(
                _cartService,
                _salesService,
                AuthService,
                _currentBranchId,
                _currentBranchName);

            var salesView = new SalesView
            {
                DataContext = salesViewModel
            };

            // Manejar salida del POS
            salesView.Closed += (sender, args) =>
            {
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

            salesView.Show();
            desktop.MainWindow?.Close();
        }

        /// <summary>
        /// Muestra el modulo de Inventario (placeholder).
        /// </summary>
        private void ShowInventory()
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

            placeholderWindow.Show();
        }

        /// <summary>
        /// Muestra el modulo de Administrador (placeholder).
        /// </summary>
        private void ShowAdmin()
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

            placeholderWindow.Show();
        }

        /// <summary>
        /// Obtiene el servicio de ventas (usado por las vistas).
        /// </summary>
        public SalesService? GetSaleService()
        {
            return _salesService;
        }
    }
}
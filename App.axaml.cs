using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Services;
using CasaCejaRemake.ViewModels.Shared;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Views.Shared;using casa_ceja_remake.Helpers;using CasaCejaRemake.Views.POS;

namespace CasaCejaRemake
{
    public partial class App : Application
    {
        // Servicios estaticos
        public static DatabaseService? DatabaseService { get; private set; }
        public static AuthService? AuthService { get; private set; }
        public static RoleService? RoleService { get; private set; }
        public static ConfigService? ConfigService { get; private set; }
        public static PrintService? PrintService { get; private set; }
        public static ExportService? ExportService { get; private set; }
        public static FolioService? FolioService { get; private set; }
        public static UserService? UserService { get; private set; }
        
        // Servicios del POS
        private CartService? _cartService;
        private SalesService? _salesService;
        private CreditService? _creditService;
        private LayawayService? _layawayService;
        private CustomerService? _customerService;
        private CashCloseService? _cashCloseService;

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

                // Inicializar datos por defecto (incluye roles)
                var initializer = new DatabaseInitializer(DatabaseService);
                await initializer.InitializeDefaultDataAsync();

                // Inicializar RoleService (cargar roles desde la BD)
                RoleService = new RoleService(DatabaseService);
                await RoleService.LoadRolesAsync();

                // Inicializar AuthService con RoleService
                var userRepository = new BaseRepository<Models.User>(DatabaseService);
                AuthService = new AuthService(userRepository, RoleService);

                // Inicializar UserService shared
                UserService = new UserService(userRepository, RoleService);

                // Inicializar ConfigService (configuración local JSON)
                ConfigService = new ConfigService();
                await ConfigService.LoadAsync();

                // Sincronizar la sucursal inicial en AuthService desde ConfigService
                // Esto asegura que al hacer login, la sucursal configurada esté disponible
                if (AuthService != null && ConfigService != null)
                {
                    var initialBranchId = ConfigService.AppConfig.BranchId;
                    AuthService.SetCurrentBranch(initialBranchId);
                    Console.WriteLine($"[App] Sucursal inicial sincronizada: {initialBranchId}");
                }

                // Suscribirse a cambios de configuración
                ConfigService.AppConfigChanged += OnAppConfigChanged;

                // Inicializar PrintService
                PrintService = new PrintService(ConfigService);

                // Inicializar ExportService
                ExportService = new ExportService();

                // Inicializar FolioService
                FolioService = new FolioService(DatabaseService);

                // Inicializar estructura de carpetas para documentos
                FileHelper.EnsureDirectoriesExist();

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
        /// Maneja el login exitoso: siempre va al selector de módulos.
        /// Sincroniza la sucursal con ConfigService si es Admin.
        /// </summary>
        private void HandleSuccessfulLogin()
        {
            // Sincronizar sucursal desde ConfigService para TODOS los usuarios
            if (AuthService != null && ConfigService != null)
            {
                var configBranchId = ConfigService.AppConfig.BranchId;
                AuthService.SetCurrentBranch(configBranchId);
                Console.WriteLine($"[App] Sucursal sincronizada desde ConfigService: {configBranchId} (Usuario: {AuthService.CurrentUserName})");
            }
            
            // Todos los usuarios van al selector de módulos
            ShowModuleSelector();
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

            selectorViewModel.ConfigSelected += async (s, e) =>
            {
                Console.WriteLine("[App] Configuración seleccionada");
                await ShowAppConfigDialog(selectorView);
            };

            selectorViewModel.LogoutRequested += (s, e) =>
            {
                Console.WriteLine("[App] Logout solicitado");
                selectorView.Tag = "logout";
                AuthService?.Logout();
                // Primero mostrar login, luego cerrar selector
                ShowLogin(selectorView);
            };

            selectorViewModel.ExitRequested += (s, e) =>
            {
                Console.WriteLine("[App] Salida solicitada - Cerrando aplicación");
                selectorView.Tag = "exit";
                // Cerrar la aplicación completa
                desktop.Shutdown();
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
            
            // Obtener sucursal actual desde ConfigService
            var currentBranchId = AuthService.CurrentBranchId;

            // Obtener el nombre real desde la BD (nunca usar valor hardcodeado del config)
            var currentBranchName = "Sucursal";
            try
            {
                var branchRepo = new Data.Repositories.BaseRepository<Models.Branch>(DatabaseService);
                var branch = await branchRepo.GetByIdAsync(currentBranchId);
                currentBranchName = branch?.Name ?? $"Sucursal #{currentBranchId}";

                // Sincronizar el nombre en el config para que quede actualizado
                if (ConfigService != null && branch != null && ConfigService.AppConfig.BranchName != branch.Name)
                {
                    await ConfigService.UpdateAppConfigAsync(c => c.BranchName = branch.Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Advertencia al obtener nombre de sucursal: {ex.Message}");
            }

            Console.WriteLine($"[App] Usando sucursal: {currentBranchName} (ID: {currentBranchId})");
            
            // Crear ViewModel con servicios inyectados
            var salesViewModel = new SalesViewModel(
                _cartService,
                _salesService,
                AuthService,
                currentBranchId,
                currentBranchName);

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
            var openCash = await _cashCloseService.GetOpenCashAsync(currentBranchId);
            if (openCash == null)
            {
                Console.WriteLine("[App] No hay caja abierta, mostrando modal de apertura...");
                
                var openCashView = new OpenCashView();
                var openCashViewModel = new OpenCashViewModel(_cashCloseService, AuthService, currentBranchId);
                openCashView.DataContext = openCashViewModel;

                await openCashView.ShowDialog(salesView);

                if (openCashView.Tag is Models.CashClose newCash)
                {
                    Console.WriteLine($"[App] Caja abierta exitosamente: {newCash.Folio}");
                    
                    // Recargar folio en el ViewModel
                    salesViewModel?.RefreshCashCloseFolio();
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
                if (salesView.Tag is string result)
                {
                    if (result == "module_selector")
                    {
                        // Volver al selector de módulos manteniendo la sesión
                        ShowModuleSelector();
                    }
                    else if (result == "logout")
                    {
                        // Cerrar sesión y volver al login
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
        /// Muestra el diálogo de configuración general (solo Admin).
        /// </summary>
        /// <summary>
        /// Muestra el diálogo de configuración general (sucursal).
        /// Si se cambia la sucursal, cierra la aplicación automáticamente.
        /// </summary>
        private async Task ShowAppConfigDialog(Window? parentWindow)
        {
            if (ConfigService == null || AuthService == null || DatabaseService == null || PrintService == null || UserService == null)
            {
                Console.WriteLine("[App] Servicios no disponibles para configuración");
                return;
            }

            var viewModel = new ViewModels.Shared.AppConfigViewModel(
                ConfigService,
                AuthService,
                DatabaseService,
                PrintService,
                UserService);

            // Suscribirse al evento de configuración guardada (cambio de sucursal)
            viewModel.ConfigurationSaved += async (s, e) =>
            {
                Console.WriteLine("[App] Sucursal cambiada - La aplicación se cerrará automáticamente...");
                
                // Mostrar mensaje al usuario SIEMPRE
                await DialogHelper.ShowMessageDialog(
                    parentWindow!,
                    "Reinicio Requerido",
                    "La sucursal ha sido cambiada.\n\nLa aplicación se cerrará automáticamente para aplicar los cambios.");
                
                // Marcar el parent para evitar que su evento Closed abra el login
                if (parentWindow != null)
                    parentWindow.Tag = "branch_changed";

                // Cerrar la aplicación COMPLETA
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    Console.WriteLine("[App] Cerrando aplicación por cambio de sucursal...");
                    desktop.Shutdown();
                }
            };

            var view = new Views.Shared.AppConfigView
            {
                DataContext = viewModel
            };

            await viewModel.InitializeAsync();
            await view.ShowDialog(parentWindow);
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
        /// Obtiene el servicio de configuración (usado por las vistas).
        /// </summary>
        public ConfigService? GetConfigService()
        {
            return ConfigService;
        }

        /// <summary>
        /// Obtiene el servicio de impresión (usado por las vistas).
        /// </summary>
        public PrintService? GetPrintService()
        {
            return PrintService;
        }

        public UserService? GetUserService()
        {
            return UserService;
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

        /// <summary>
        /// Obtiene la sucursal actual desde la base de datos.
        /// </summary>
        public async System.Threading.Tasks.Task<Models.Branch?> GetCurrentBranchAsync()
        {
            if (DatabaseService == null || ConfigService == null) return null;
            var branchId = ConfigService.AppConfig.BranchId;
            var repo = new Data.Repositories.BaseRepository<Models.Branch>(DatabaseService);
            return await repo.GetByIdAsync(branchId);
        }

        /// <summary>
        /// Maneja cambios en la configuración general (sucursal).
        /// </summary>
        private void OnAppConfigChanged(object? sender, EventArgs e)
        {
            if (ConfigService == null) return;
            
            var branchId = ConfigService.AppConfig.BranchId;
            var branchName = ConfigService.AppConfig.BranchName;
            
            Console.WriteLine($"[App] Configuración cambiada - Nueva sucursal: {branchName} (ID: {branchId})");
            
            // Nota: El POS necesitará reiniciarse para aplicar cambios de sucursal
            // ya que el ViewModel se inicializa con el BranchId al crearse
        }
    }
}

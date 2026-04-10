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
using CasaCejaRemake.ViewModels.Inventory;
using CasaCejaRemake.Views.Shared;using casa_ceja_remake.Helpers;using CasaCejaRemake.Views.POS;
using CasaCejaRemake.Views.Inventory;

namespace CasaCejaRemake
{
    public partial class App : Application
    {
        // Servicios estaticos
        public static ApiClient? ApiClient { get; private set; }
        public static SyncService? SyncService { get; private set; }
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
        private TicketService? _ticketService;
        
        // Servicios de Inventario
        private InventoryService? _inventoryService;

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
                // ── Servicios de sincronización ───────────────────────────────────
                DatabaseService = new DatabaseService();
                await DatabaseService.InitializeAsync();

                ConfigService = new ConfigService();
                await ConfigService.LoadAsync();

                // Inicializar datos por defecto (incluye roles)
                var initializer = new DatabaseInitializer(DatabaseService, ConfigService);
                await initializer.InitializeDefaultDataAsync();

                // ── Repos especializados ──────────────────────────────────────
                var productRepo = new ProductRepository(DatabaseService);
                var saleRepo = new SaleRepository(DatabaseService);
                var cashCloseRepo = new CashCloseRepository(DatabaseService);
                var creditRepo = new CreditRepository(DatabaseService);
                var layawayRepo = new LayawayRepository(DatabaseService);

                // ── Repos genéricos ───────────────────────────────────────────
                var userRepo         = new BaseRepository<Models.User>(DatabaseService);
                var saleProductRepo  = new BaseRepository<Models.SaleProduct>(DatabaseService);
                var branchRepo       = new BaseRepository<Models.Branch>(DatabaseService);
                var categoryRepo     = new BaseRepository<Models.Category>(DatabaseService);
                var unitRepo         = new BaseRepository<Models.Unit>(DatabaseService);
                var customerRepo     = new BaseRepository<Models.Customer>(DatabaseService);
                var creditProductRepo  = new BaseRepository<Models.CreditProduct>(DatabaseService);
                var creditPaymentRepo  = new BaseRepository<Models.CreditPayment>(DatabaseService);
                var layawayProductRepo = new BaseRepository<Models.LayawayProduct>(DatabaseService);
                var layawayPaymentRepo = new BaseRepository<Models.LayawayPayment>(DatabaseService);
                var cashMovementRepo   = new BaseRepository<Models.CashMovement>(DatabaseService);

                // ── Servicios base ────────────────────────────────────────────
                RoleService = new RoleService(DatabaseService);
                await RoleService.LoadRolesAsync();

                AuthService = new AuthService(userRepo, RoleService);
                UserService = new UserService(userRepo, RoleService);

                // Sincronizar la sucursal inicial en AuthService desde ConfigService
                if (AuthService != null && ConfigService != null)
                {
                    var initialBranchId = ConfigService.AppConfig.CurrentBranchId ?? 0;
                    AuthService.SetCurrentBranch(initialBranchId);
                    Console.WriteLine($"[App] Sucursal inicial sincronizada: {initialBranchId}");
                }

               
                // Suscribirse a cambios de configuración
                ConfigService.AppConfigChanged += OnAppConfigChanged;

                ApiClient   = new ApiClient(ConfigService);
                SyncService = new SyncService(ApiClient, ConfigService, DatabaseService);

                PrintService = new PrintService(ConfigService);
                ExportService = new ExportService();

                var productStockRepo = new BaseRepository<Models.ProductStock>(DatabaseService);
                var entryRepo = new BaseRepository<Models.StockEntry>(DatabaseService);
                var outputRepo = new BaseRepository<Models.StockOutput>(DatabaseService);

                FolioService = new FolioService(
                    cashCloseRepo, saleRepo, creditRepo,
                    layawayRepo, creditPaymentRepo, layawayPaymentRepo,
                    DatabaseService, entryRepo, outputRepo);

                FileHelper.EnsureDirectoriesExist();

                // ── Servicios POS ─────────────────────────────────────────────
                var ticketService  = new TicketService();
                _ticketService = ticketService;
                var pricingService = new PricingService();

                _cartService = new CartService();

                _salesService = new SalesService(
                    productRepo, saleRepo, saleProductRepo,
                    branchRepo, categoryRepo, unitRepo, userRepo,
                    ticketService, pricingService, FolioService, ConfigService);

                _customerService = new CustomerService(customerRepo, SyncService);

                _creditService = new CreditService(
                    creditRepo, creditProductRepo, creditPaymentRepo,
                    customerRepo, branchRepo, ticketService, FolioService, ConfigService);

                _layawayService = new LayawayService(
                    layawayRepo, layawayProductRepo, layawayPaymentRepo,
                    customerRepo, branchRepo, ticketService, FolioService, ConfigService);

                _cashCloseService = new CashCloseService(
                    cashCloseRepo, cashMovementRepo,
                    saleRepo, creditRepo, layawayRepo,
                    layawayPaymentRepo, creditPaymentRepo,
                    FolioService, ConfigService);
                var entryProductRepo = new BaseRepository<Models.EntryProduct>(DatabaseService);
                var outputProductRepo = new BaseRepository<Models.OutputProduct>(DatabaseService);
                var supplierRepo = new BaseRepository<Models.Supplier>(DatabaseService);

                _inventoryService = new InventoryService(
                    productRepo, categoryRepo, unitRepo, productStockRepo,
                    entryRepo, outputRepo, entryProductRepo, outputProductRepo, supplierRepo, branchRepo);

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

            var userRepo       = new BaseRepository<Models.User>(DatabaseService!);
            var loginViewModel = new LoginViewModel(AuthService, ApiClient, ConfigService, userRepo);

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

        /// Maneja el login exitoso: siempre va al selector de módulos.
        /// Sincroniza la sucursal con ConfigService si es Admin.
    private void HandleSuccessfulLogin()
    {
        if (AuthService != null && ConfigService != null)
        {
            var configBranchId = ConfigService.AppConfig.CurrentBranchId ?? 0;
            AuthService.SetCurrentBranch(configBranchId);
            Console.WriteLine($"[App] Sucursal sincronizada desde ConfigService: {configBranchId} (Usuario: {AuthService.CurrentUserName})");
        }

        // Mostrar pantalla de carga y sincronización
        ShowSyncLoading();
    }

    private void ShowSyncLoading()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        if (ApiClient == null || SyncService == null || ConfigService == null || AuthService == null)
        {
            ShowModuleSelector();
            return;
        }

        var syncViewModel = new SyncLoadingViewModel(ApiClient, SyncService, ConfigService, AuthService);
        var syncView = new SyncLoadingView
        {
            DataContext = syncViewModel
        };

        syncView.Closed += (s, e) =>
        {
            ShowModuleSelector();
        };

        // Establecer como MainWindow antes de mostrar
        desktop.MainWindow = syncView;
        syncView.Show();
        syncView.StartSync();
    }
        /// Muestra el selector de modulos (solo Admin).
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
            desktop.MainWindow = selectorView;
            selectorView.Show();
        }

        /// Muestra el modulo POS.
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
                if (ConfigService != null && branch != null && ConfigService.AppConfig.CurrentBranchName != branch.Name)
                {
                    await ConfigService.UpdateAppConfigAsync(c => c.CurrentBranchName = branch.Name);
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
        /// Muestra el módulo de Inventario.
        /// </summary>
        private async void ShowInventory(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (AuthService == null || ApiClient == null) return;

            Console.WriteLine("[App] ShowInventory() llamado");

            // Obtener sucursal actual
            var currentBranchId = AuthService.CurrentBranchId;
            var currentBranchName = "Sucursal";
            try
            {
                var branchRepo = new BaseRepository<Models.Branch>(DatabaseService!);
                var branch = await branchRepo.GetByIdAsync(currentBranchId);
                currentBranchName = branch?.Name ?? $"Sucursal #{currentBranchId}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Advertencia al obtener nombre de sucursal: {ex.Message}");
            }

            var viewModel = new InventoryMainViewModel(
                AuthService, ApiClient, currentBranchId, currentBranchName);

            var inventoryView = new InventoryMainView
            {
                DataContext = viewModel
            };

            // ── Suscribir eventos de navegación ──

            viewModel.CatalogSelected += (s, e) =>
            {
                Console.WriteLine("[App] Catálogo seleccionado");
                inventoryView.Tag = "catalog";
                ShowCatalog(inventoryView);
            };

            viewModel.CategoriesSelected += (s, e) =>
            {
                Console.WriteLine("[App] Abriendo Gestión de Categorías");
                var catalogsViewModel = new ViewModels.Inventory.CatalogsManagementViewModel(_inventoryService!);
                var catalogsView = new Views.Inventory.CatalogsManagementView
                {
                    DataContext = catalogsViewModel
                };

                catalogsViewModel.GoBackRequested += (sender, args) =>
                {
                    catalogsView.Close();
                };

                catalogsView.ShowDialog(inventoryView);
            };

            viewModel.EntriesSelected += (s, e) =>
            {
                Console.WriteLine("[App] Abriendo Entrada de Mercancía");
                ShowEntry(inventoryView);
            };

            viewModel.OutputsSelected += (s, e) =>
            {
                Console.WriteLine("[App] Abriendo Salida de Mercancía");
                ShowOutput(inventoryView);
            };

            viewModel.ConfirmEntrySelected += (s, e) =>
            {
                Console.WriteLine("[App] Abriendo Confirmar Entrada");
                ShowConfirmEntry(inventoryView);
            };

            viewModel.HistorySelected += (s, e) =>
            {
                Console.WriteLine("[App] Historial seleccionado");
                inventoryView.Tag = "history";
                ShowHistory(inventoryView);
            };

            viewModel.ExitRequested += (s, e) =>
            {
                Console.WriteLine("[App] Regresando a selector de módulos desde Inventario");
                inventoryView.Tag = "module_selector";
                inventoryView.Close();
            };

            viewModel.LogoutRequested += (s, e) =>
            {
                Console.WriteLine("[App] Cerrando sesión desde Inventario");
                inventoryView.Tag = "logout";
                inventoryView.Close();
            };

            inventoryView.Closed += (sender, args) =>
            {
                Console.WriteLine($"[App] InventoryMainView cerrada. Tag = {inventoryView.Tag}");
                if (inventoryView.Tag is string result)
                {
                    if (result == "module_selector")
                    {
                        ShowModuleSelector();
                    }
                    else if (result == "logout")
                    {
                        AuthService?.Logout();
                        ShowLogin();
                    }
                }
                else
                {
                    // Cerrado sin tag (Botón Salir, botón X o Cmd+Q) -> dejar que se cierre la app
                    if (desktop.MainWindow == inventoryView || desktop.MainWindow == null)
                    {
                        Console.WriteLine("[App] Cerrando aplicación.");
                    }
                }
            };

            desktop.MainWindow = inventoryView;
            inventoryView.Show();
            windowToClose?.Close();

            Console.WriteLine("[App] InventoryMainView mostrada correctamente");
        }

        private async void ShowEntry(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (_inventoryService == null || FolioService == null) return;

            var branchId   = AuthService.CurrentBranchId;
            var branchName = "Sucursal";
            var userId     = AuthService?.CurrentUser?.Id ?? 0;

            try
            {
                var branchRepo = new Data.Repositories.BaseRepository<Models.Branch>(DatabaseService!);
                var branch = await branchRepo.GetByIdAsync(branchId);
                branchName = branch?.Name ?? $"Sucursal #{branchId}";
            }
            catch { /* use default */ }

            var viewModel = new ViewModels.Inventory.EntriesViewModel(
                _inventoryService, FolioService, branchId, branchName, userId);

            var entryView = new Views.Inventory.EntryView
            {
                DataContext = viewModel
            };

            viewModel.GoBackRequested += (s, e) =>
            {
                Console.WriteLine("[App] Volviendo a Inventario desde Entrada");
                entryView.Tag = "inventory";
                entryView.Close();
            };

            entryView.Closed += (sender, args) =>
            {
                ShowInventory(null);
            };

            desktop.MainWindow = entryView;
            entryView.Show();
            windowToClose?.Close();

            Console.WriteLine("[App] EntryView mostrada");
        }

        private async void ShowOutput(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (_inventoryService == null || FolioService == null) return;

            var branchId   = AuthService.CurrentBranchId;
            var branchName = "Sucursal";
            var userId     = AuthService?.CurrentUser?.Id ?? 0;

            try
            {
                var branchRepo = new Data.Repositories.BaseRepository<Models.Branch>(DatabaseService!);
                var branch = await branchRepo.GetByIdAsync(branchId);
                branchName = branch?.Name ?? $"Sucursal #{branchId}";
            }
            catch { /* use default */ }

            var viewModel = new ViewModels.Inventory.OutputsViewModel(
                _inventoryService, FolioService, branchId, branchName, userId);

            var outputView = new Views.Inventory.OutputView
            {
                DataContext = viewModel
            };

            viewModel.GoBackRequested += (s, e) =>
            {
                Console.WriteLine("[App] Volviendo a Inventario desde Salida");
                outputView.Tag = "inventory";
                outputView.Close();
            };

            outputView.Closed += (sender, args) =>
            {
                ShowInventory(null);
            };

            desktop.MainWindow = outputView;
            outputView.Show();
            windowToClose?.Close();

            Console.WriteLine("[App] OutputView mostrada");
        }

        private async void ShowConfirmEntry(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (_inventoryService == null) return;

            var branchId   = AuthService.CurrentBranchId;
            var branchName = "Sucursal";
            var userId     = AuthService?.CurrentUser?.Id ?? 0;

            try
            {
                var branchRepo = new Data.Repositories.BaseRepository<Models.Branch>(DatabaseService!);
                var branch = await branchRepo.GetByIdAsync(branchId);
                branchName = branch?.Name ?? $"Sucursal #{branchId}";
            }
            catch { /* use default */ }

            var viewModel = new ViewModels.Inventory.ConfirmEntryViewModel(
                _inventoryService, branchId, branchName, userId);

            var confirmView = new Views.Inventory.ConfirmEntryView
            {
                DataContext = viewModel
            };

            viewModel.GoBackRequested += (s, e) =>
            {
                Console.WriteLine("[App] Volviendo a Inventario desde ConfirmEntry");
                confirmView.Tag = "inventory";
                confirmView.Close();
            };

            confirmView.Closed += (sender, args) =>
            {
                ShowInventory(null);
            };

            desktop.MainWindow = confirmView;
            confirmView.Show();
            windowToClose?.Close();

            Console.WriteLine("[App] ConfirmEntryView mostrada");
        }

        private void ShowCatalog(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (_inventoryService == null) return;

            var branchId = AuthService?.CurrentUser?.BranchId ?? 0;
            var viewModel = new ViewModels.Inventory.CatalogViewModel(_inventoryService, branchId);
            
            var catalogView = new Views.Inventory.CatalogView
            {
                DataContext = viewModel
            };

            viewModel.GoBackRequested += (s, e) =>
            {
                catalogView.Tag = "inventory";
                ShowInventory(catalogView);
            };

            viewModel.ProductFormRequested += (s, product) =>
            {
                Console.WriteLine("[App] Abriendo ProductFormView");
                var c = s as ViewModels.Inventory.CatalogViewModel;
                int bId = c != null ? c.CurrentBranchId : 1;
                var formViewModel = new ViewModels.Inventory.ProductFormViewModel(_inventoryService, bId, product);
                var formView = new Views.Inventory.ProductFormView
                {
                    DataContext = formViewModel
                };

                formViewModel.SaveCompleted += (s, e) =>
                {
                    formView.Close();
                    viewModel.RefreshData();
                };

                // La cancelación se maneja internamente en ProductFormView.axaml.cs con un popup de confirmación.

                // As it is a child window
                formView.ShowDialog(catalogView);
            };

            viewModel.ProductDetailRequested += (s, product) =>
            {
                var detailViewModel = new ViewModels.Inventory.ProductDetailViewModel(product);
                var detailView = new Views.Inventory.ProductDetailView
                {
                    DataContext = detailViewModel
                };

                detailViewModel.CloseRequested += (sender, args) =>
                {
                    detailView.Close();
                };

                detailView.ShowDialog(catalogView);
            };
            catalogView.Closed += (sender, args) =>
            {
                // Si no tiene tag, se cerró con la 'X' o Cmd+Q -> dejar que se cierre la app.
            };

            desktop.MainWindow = catalogView;
            catalogView.Show();
            windowToClose?.Close();
        }

        private async void ShowHistory(Window? windowToClose = null)
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (_inventoryService == null) return;

            var branchId = AuthService?.CurrentUser?.BranchId ?? 0;
            var viewModel = new ViewModels.Inventory.HistoryViewModel(_inventoryService, branchId);
            
            var historyView = new Views.Inventory.HistoryView
            {
                DataContext = viewModel
            };

            viewModel.GoBackRequested += (s, e) =>
            {
                historyView.Tag = "inventory";
                ShowInventory(historyView);
            };

            viewModel.MovementDetailRequested += (s, historyItem) =>
            {
                var detailVM = new ViewModels.Inventory.MovementDetailViewModel(_inventoryService!, historyItem);
                var detailView = new Views.Inventory.MovementDetailView
                {
                    DataContext = detailVM
                };

                detailVM.CloseRequested += (s2, a2) => detailView.Close();
                detailView.ShowDialog(historyView);
            };

            historyView.Closed += (sender, args) =>
            {
                // Cerrado vía X
            };

            desktop.MainWindow = historyView;
            historyView.Show();
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
                new BaseRepository<Models.Branch>(DatabaseService!),
                ConfigService!,
                AuthService!,
                PrintService!,
                UserService!,
                SyncService!,
                ApiClient!);

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
        /// Obtiene el servicio de tickets (usado por las vistas).
        /// </summary>
        public TicketService? GetTicketService()
        {
            return _ticketService;
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
            var branchId = ConfigService.AppConfig.CurrentBranchId ?? 0;
            var repo = new Data.Repositories.BaseRepository<Models.Branch>(DatabaseService);
            return await repo.GetByIdAsync(branchId);
        }

        /// <summary>
        /// Maneja cambios en la configuración general (sucursal).
        /// </summary>
        private void OnAppConfigChanged(object? sender, EventArgs e)
        {
            if (ConfigService == null) return;
            
            var branchId = ConfigService.AppConfig.CurrentBranchId ?? 0;
            var branchName = ConfigService.AppConfig.CurrentBranchName ?? string.Empty;
            
            Console.WriteLine($"[App] Configuración cambiada - Nueva sucursal: {branchName} (ID: {branchId})");
            
            // Nota: El POS necesitará reiniciarse para aplicar cambios de sucursal
            // ya que el ViewModel se inicializa con el BranchId al crearse
        }
    }
}

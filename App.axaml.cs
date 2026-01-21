using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.ViewModels;
using CasaCejaRemake.ViewModels.Shared;
using CasaCejaRemake.Views;
using CasaCejaRemake.Views.Shared;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake
{
    public partial class App : Application
    {
        // ====================
        // SERVICIOS GLOBALES
        // ====================

        public static DatabaseService DatabaseService { get; private set; } = null!;
        public static AuthService AuthService { get; private set; } = null!;

        // ====================
        // INICIALIZACIÓN
        // ====================

        public override void Initialize()
        {
            Console.WriteLine("📋 Inicializando Avalonia XAML...");
            AvaloniaXamlLoader.Load(this);
            Console.WriteLine("✅ Avalonia XAML inicializado");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Console.WriteLine("🔧 Framework inicializado, configurando aplicación...");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("🖥️  Aplicación de escritorio detectada");
                
                // Inicializar servicios y mostrar login
                Task.Run(async () =>
                {
                    await InitializeServicesAsync();
                    
                    // Volver al hilo de UI para mostrar la ventana
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        ShowLogin(desktop);
                    });
                });
            }
            else
            {
                Console.WriteLine("⚠️ No se detectó aplicación de escritorio");
            }

            base.OnFrameworkInitializationCompleted();
        }

        // ====================
        // INICIALIZACIÓN DE SERVICIOS
        // ====================

        /// <summary>
        /// Inicializa todos los servicios de la aplicación
        /// </summary>
        private async Task InitializeServicesAsync()
        {
            try
            {
                Console.WriteLine("💾 Inicializando DatabaseService...");
                // 1. Inicializar DatabaseService
                DatabaseService = new DatabaseService();
                Console.WriteLine("⏳ Esperando inicialización de base de datos...");
                await DatabaseService.InitializeAsync();
                Console.WriteLine("✅ DatabaseService inicializado");

                Console.WriteLine("� Inicializando datos por defecto...");
                var dbInitializer = new DatabaseInitializer(DatabaseService);
                await dbInitializer.InitializeDefaultDataAsync();
                Console.WriteLine("✅ Datos por defecto inicializados");

                Console.WriteLine("�👤 Inicializando AuthService...");
                // 2. Crear AuthService con repositorio de usuarios
                var userRepository = new BaseRepository<User>(DatabaseService);
                AuthService = new AuthService(userRepository);
                Console.WriteLine("✅ AuthService inicializado");

                Console.WriteLine("✅ Servicios inicializados correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error inicializando servicios: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        // ====================
        // FLUJO DE LOGIN
        // ====================

        /// <summary>
        /// Muestra la pantalla de login
        /// </summary>
        private void ShowLogin(IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                Console.WriteLine("🔐 Creando pantalla de login...");
                // Crear LoginView con su ViewModel
                var loginViewModel = new LoginViewModel(AuthService);
                Console.WriteLine("✅ LoginViewModel creado");
                
                var loginView = new LoginView
                {
                    DataContext = loginViewModel
                };
                Console.WriteLine("✅ LoginView creado");

                // Manejar resultado del login
                loginView.Closed += (sender, args) =>
                {
                    Console.WriteLine("🚪 LoginView cerrado");
                    if (sender is LoginView window && window.Tag is LoginSuccessEventArgs loginResult)
                    {
                        // Login exitoso
                        HandleSuccessfulLogin(desktop, loginResult.IsAdmin);
                    }
                    else
                    {
                        // Login cancelado o cerrado - cerrar aplicación
                        desktop.Shutdown();
                    }
                };

                // Modificar el event handler en LoginView para pasar el resultado
                loginViewModel.LoginSuccess += (sender, e) =>
                {
                    loginView.Tag = e; // Guardar resultado
                };

                // Mostrar ventana de login
                Console.WriteLine("🪟 Estableciendo MainWindow como LoginView...");
                desktop.MainWindow = loginView;
                loginView.Show();
                loginView.Activate();
                Console.WriteLine("✅ LoginView establecido como MainWindow y mostrado");
                Console.WriteLine("👀 Si no ves la ventana, verifica tu pantalla o Cmd+Tab");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error mostrando login: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Maneja un login exitoso
        /// </summary>
        private void HandleSuccessfulLogin(IClassicDesktopStyleApplicationLifetime desktop, bool isAdmin)
        {
            if (isAdmin)
            {
                // Usuario Admin: Mostrar selector de módulos
                ShowModuleSelector(desktop);
            }
            else
            {
                // Usuario Cajero: Ir directo a POS
                ShowPOS(desktop);
            }
        }

        // ====================
        // SELECTOR DE MÓDULOS (ADMIN)
        // ====================

        /// <summary>
        /// Muestra el selector de módulos para usuarios Admin
        /// </summary>
        private void ShowModuleSelector(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Crear ModuleSelectorView con su ViewModel
            var moduleSelectorViewModel = new ModuleSelectorViewModel(AuthService);
            var moduleSelectorView = new ModuleSelectorView
            {
                DataContext = moduleSelectorViewModel
            };

            // Manejar resultado
            moduleSelectorView.Closed += (sender, args) =>
            {
                if (sender is ModuleSelectorView window && window.Tag is string moduleSelected)
                {
                    HandleModuleSelection(desktop, moduleSelected);
                }
                else
                {
                    // No se seleccionó módulo - volver a login
                    ShowLogin(desktop);
                }
            };

            // Capturar el resultado seleccionado
            moduleSelectorViewModel.POSSelected += (s, e) =>
            {
                moduleSelectorView.Tag = "POS";
            };
            moduleSelectorViewModel.InventorySelected += (s, e) =>
            {
                moduleSelectorView.Tag = "Inventory";
            };
            moduleSelectorViewModel.AdminSelected += (s, e) =>
            {
                moduleSelectorView.Tag = "Admin";
            };
            moduleSelectorViewModel.LogoutRequested += (s, e) =>
            {
                moduleSelectorView.Tag = "Logout";
            };

            // Mostrar ventana de selector
            desktop.MainWindow = moduleSelectorView;
        }

        /// <summary>
        /// Maneja la selección de un módulo
        /// </summary>
        private void HandleModuleSelection(IClassicDesktopStyleApplicationLifetime desktop, string module)
        {
            switch (module)
            {
                case "POS":
                    ShowPOS(desktop);
                    break;

                case "Inventory":
                    ShowInventory(desktop);
                    break;

                case "Admin":
                    ShowAdmin(desktop);
                    break;

                case "Logout":
                    // Cerrar sesión y volver a login
                    AuthService.Logout();
                    ShowLogin(desktop);
                    break;

                default:
                    // Opción desconocida - volver a login
                    ShowLogin(desktop);
                    break;
            }
        }

        // ====================
        // VENTANAS DE MÓDULOS
        // ====================

        /// <summary>
        /// Muestra el módulo POS
        /// </summary>
        private void ShowPOS(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // TODO: Implementar en Fase 3
            // Por ahora, mostrar la MainWindow existente
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

            // Al cerrar POS, volver a login si es cajero, o selector si es admin
            mainWindow.Closed += (sender, args) =>
            {
                if (AuthService.IsAuthenticated)
                {
                    if (AuthService.IsAdmin)
                    {
                        ShowModuleSelector(desktop);
                    }
                    else
                    {
                        // Cajero cerrando POS = logout
                        AuthService.Logout();
                        ShowLogin(desktop);
                    }
                }
                else
                {
                    ShowLogin(desktop);
                }
            };

            desktop.MainWindow = mainWindow;
        }

        /// <summary>
        /// Muestra el módulo Inventario
        /// </summary>
        private void ShowInventory(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // TODO: Implementar en Fase 7
            Console.WriteLine("⚠️ Módulo Inventario aún no implementado");

            // Por ahora, volver al selector
            ShowModuleSelector(desktop);
        }

        /// <summary>
        /// Muestra el módulo Administrador
        /// </summary>
        private void ShowAdmin(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // TODO: Implementar en Fase 8
            Console.WriteLine("⚠️ Módulo Administrador aún no implementado");

            // Por ahora, volver al selector
            ShowModuleSelector(desktop);
        }
    }
}
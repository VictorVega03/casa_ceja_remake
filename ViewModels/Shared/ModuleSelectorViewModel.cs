using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Services;
using System;

namespace CasaCejaRemake.ViewModels.Shared
{
    /// <summary>
    /// ViewModel para el selector de módulos (Admin solamente)
    /// </summary>
    public partial class ModuleSelectorViewModel : ViewModelBase
    {
        private readonly AuthService _authService;

        // ====================
        // PROPIEDADES OBSERVABLES
        // ====================

        [ObservableProperty]
        private string _welcomeMessage = string.Empty;

        [ObservableProperty]
        private bool _isAdmin = true;

        [ObservableProperty]
        private bool _isCashier = false;

        // ====================
        // EVENTOS
        // ====================

        /// <summary>
        /// Evento cuando se selecciona abrir el módulo POS
        /// </summary>
        public event EventHandler? POSSelected;

        /// <summary>
        /// Evento cuando se selecciona abrir el módulo Inventario
        /// </summary>
        public event EventHandler? InventorySelected;

        /// <summary>
        /// Evento cuando se selecciona abrir el módulo Admin
        /// </summary>
        public event EventHandler? AdminSelected;

        /// <summary>
        /// Evento cuando se selecciona abrir Configuración General
        /// </summary>
        public event EventHandler? ConfigSelected;

        /// <summary>
        /// Evento cuando se cierra sesión
        /// </summary>
        public event EventHandler? LogoutRequested;

        /// <summary>
        /// Evento cuando se solicita salir de la aplicación
        /// </summary>
        public event EventHandler? ExitRequested;

        /// <summary>
        /// Evento cuando se solicita abrir la carpeta de documentos
        /// </summary>
        public event EventHandler<string>? FolderOpenError;

        // ====================
        // CONSTRUCTOR
        // ====================

        public ModuleSelectorViewModel(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            
            // Configurar mensaje de bienvenida
            var userName = _authService.CurrentUserName ?? "Administrador";
            WelcomeMessage = $"Bienvenido, {userName}";
            
            // Configurar permisos
            IsAdmin = _authService.IsAdmin;
            IsCashier = _authService.IsCajero;
        }

        // ====================
        // COMANDOS
        // ====================

        /// <summary>
        /// Comando para abrir el módulo POS
        /// </summary>
        [RelayCommand]
        private void OpenPOS()
        {
            POSSelected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para abrir el módulo Inventario
        /// </summary>
        [RelayCommand]
        private void OpenInventory()
        {
            // Solo admins pueden acceder
            if (!IsAdmin) return;
            
            InventorySelected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para abrir el módulo Admin
        /// </summary>
        [RelayCommand]
        private void OpenAdmin()
        {
            // Solo admins pueden acceder
            if (!IsAdmin) return;
            
            AdminSelected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para abrir Configuración General
        /// </summary>
        [RelayCommand]
        private void OpenConfig()
        {
            ConfigSelected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para cerrar sesión
        /// </summary>
        [RelayCommand]
        private void Logout()
        {
            // Cerrar sesión en el AuthService
            _authService.Logout();

            // Notificar evento
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para salir de la aplicación
        /// </summary>
        [RelayCommand]
        private void Exit()
        {
            // Notificar evento
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para abrir la carpeta de documentos en el explorador de archivos
        /// </summary>
        [RelayCommand]
        public void OpenDocumentsFolder()
        {
            try
            {
                var success = Helpers.FileHelper.OpenFolderInExplorer();
                
                if (!success)
                {
                    FolderOpenError?.Invoke(this, "No se pudo abrir la carpeta de documentos. Verifique que el sistema tenga permisos de acceso.");
                }
            }
            catch (Exception ex)
            {
                FolderOpenError?.Invoke(this, $"Error al abrir la carpeta: {ex.Message}");
            }
        }

        /// <summary>
        /// Comando para abrir la carpeta de la base de datos en el explorador de archivos
        /// </summary>
        [RelayCommand]
        private void OpenDbFolder()
        {
            try
            {
                var success = Helpers.FileHelper.OpenDatabaseFolderInExplorer();
                
                if (!success)
                {
                    FolderOpenError?.Invoke(this, "No se pudo abrir la carpeta de la base de datos.");
                }
            }
            catch (Exception ex)
            {
                FolderOpenError?.Invoke(this, $"Error al abrir la carpeta DB: {ex.Message}");
            }
        }
    }
}
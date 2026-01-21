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
        /// Evento cuando se cierra sesión
        /// </summary>
        public event EventHandler? LogoutRequested;

        // ====================
        // CONSTRUCTOR
        // ====================

        public ModuleSelectorViewModel(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            
            // Configurar mensaje de bienvenida
            var userName = _authService.CurrentUserName ?? "Administrador";
            WelcomeMessage = $"Bienvenido, {userName}";
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
            InventorySelected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para abrir el módulo Admin
        /// </summary>
        [RelayCommand]
        private void OpenAdmin()
        {
            AdminSelected?.Invoke(this, EventArgs.Empty);
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
    }
}
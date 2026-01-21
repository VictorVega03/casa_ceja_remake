using Avalonia.Controls;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Shared;
using System;

namespace CasaCejaRemake.Views.Shared
{
    /// <summary>
    /// Vista del Selector de Módulos - Code Behind
    /// </summary>
    public partial class ModuleSelectorView : Window
    {
        public ModuleSelectorView()
        {
            InitializeComponent();

            // Configurar eventos de la vista
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Evento cuando la ventana se ha cargado
        /// </summary>
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Configurar eventos del ViewModel si existe
            if (DataContext is ModuleSelectorViewModel viewModel)
            {
                viewModel.POSSelected += OnPOSSelected;
                viewModel.InventorySelected += OnInventorySelected;
                viewModel.AdminSelected += OnAdminSelected;
                viewModel.LogoutRequested += OnLogoutRequested;
            }
        }

        /// <summary>
        /// Evento cuando se selecciona POS
        /// </summary>
        private void OnPOSSelected(object? sender, EventArgs e)
        {
            // Cerrar con resultado "POS"
            Close("POS");
        }

        /// <summary>
        /// Evento cuando se selecciona Inventario
        /// </summary>
        private void OnInventorySelected(object? sender, EventArgs e)
        {
            // Cerrar con resultado "Inventory"
            Close("Inventory");
        }

        /// <summary>
        /// Evento cuando se selecciona Admin
        /// </summary>
        private void OnAdminSelected(object? sender, EventArgs e)
        {
            // Cerrar con resultado "Admin"
            Close("Admin");
        }

        /// <summary>
        /// Evento cuando se cierra sesión
        /// </summary>
        private void OnLogoutRequested(object? sender, EventArgs e)
        {
            // Cerrar con resultado "Logout"
            Close("Logout");
        }

        /// <summary>
        /// Cleanup al cerrar
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Desuscribir eventos
            if (DataContext is ModuleSelectorViewModel viewModel)
            {
                viewModel.POSSelected -= OnPOSSelected;
                viewModel.InventorySelected -= OnInventorySelected;
                viewModel.AdminSelected -= OnAdminSelected;
                viewModel.LogoutRequested -= OnLogoutRequested;
            }

            base.OnClosed(e);
        }
    }
}
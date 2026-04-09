using Avalonia.Controls;
using CasaCejaRemake.ViewModels.Inventory;
using System;

namespace CasaCejaRemake.Views.Inventory
{
    /// <summary>
    /// Vista del Menú Principal de Inventario - Code Behind
    /// </summary>
    public partial class InventoryMainView : Window
    {
        public InventoryMainView()
        {
            InitializeComponent();

            // Verificar conectividad al cargar
            this.Opened += OnOpened;
        }

        private async void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is InventoryMainViewModel viewModel)
            {
                await viewModel.CheckConnectivityCommand.ExecuteAsync(null);
            }
        }
    }
}

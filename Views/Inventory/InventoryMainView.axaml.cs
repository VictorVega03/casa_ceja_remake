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
                viewModel.RequestExitConfirmation += async (s, args) =>
                {
                    // Minimal confirmation dialog logic
                    var result = await casa_ceja_remake.Helpers.DialogHelper.ShowConfirmDialog(this, "Salir", "¿Está seguro de salir del inventario?");
                    if (result)
                    {
                        viewModel.ConfirmExit();
                    }
                };

                await viewModel.CheckConnectivityCommand.ExecuteAsync(null);
            }
        }
    }
}

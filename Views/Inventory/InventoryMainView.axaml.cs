using Avalonia.Controls;
using CasaCejaRemake.ViewModels.Inventory;
using System;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class InventoryMainView : Window
    {
        public InventoryMainView()
        {
            InitializeComponent();
            this.Opened += OnOpened;
        }

        private async void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is InventoryMainViewModel viewModel)
            {
                viewModel.RequestExitConfirmation += async (s, args) =>
                {
                    var result = await casa_ceja_remake.Helpers.DialogHelper.ShowConfirmDialog(
                        this, "Salir", "¿Está seguro de salir del inventario?");
                    if (result)
                        viewModel.ConfirmExit();
                };

                await viewModel.CheckConnectivityCommand.ExecuteAsync(null);
            }
        }
    }
}

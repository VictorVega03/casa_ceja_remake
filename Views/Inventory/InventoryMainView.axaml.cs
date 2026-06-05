using Avalonia.Controls;
using CasaCejaRemake.ViewModels.Inventory;
using System;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class InventoryMainView : Window
    {
        private bool _isExitDialogOpen;

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
                    if (_isExitDialogOpen)
                        return;

                    _isExitDialogOpen = true;
                    try
                    {
                        var result = await CasaCejaRemake.Views.Shared.ModuleExitDialog.ShowAsync(
                            this,
                            "Salir de Inventario",
                            "¿Está seguro de regresar al menú principal?",
                            "#2F5D8A");

                        if (result)
                            viewModel.ConfirmExit();
                    }
                    finally
                    {
                        _isExitDialogOpen = false;
                    }
                };

                await viewModel.CheckConnectivityCommand.ExecuteAsync(null);
            }
        }
    }
}

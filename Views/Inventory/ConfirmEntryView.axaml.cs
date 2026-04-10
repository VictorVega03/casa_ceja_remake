using Avalonia.Controls;
using Avalonia.Input;
using System;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class ConfirmEntryView : Window
    {
        public ConfirmEntryView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            this.Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is not ConfirmEntryViewModel vm) return;

            vm.ShowMessageRequested += async (s, msg) =>
            {
                await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", msg);
            };

            vm.ConfirmRequested += async (s, entry) =>
            {
                var confirmed = await casa_ceja_remake.Helpers.DialogHelper.ShowConfirmDialog(
                    this,
                    "Confirmar entrada",
                    $"¿Confirmas la recepción de la entrada {entry.Folio}?\n\nEsta acción marcará los productos como recibidos en esta sucursal.");

                if (confirmed)
                    await vm.DoConfirmEntryAsync(entry);
            };
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && DataContext is ConfirmEntryViewModel vm)
            {
                vm.GoBackCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}

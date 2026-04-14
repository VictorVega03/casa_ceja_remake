using Avalonia.Controls;
using Avalonia.Input;
using System;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class ConfirmEntryView : Window
    {
        private bool _hasOpenDialog;

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
                _hasOpenDialog = true;
                await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", msg);
                _hasOpenDialog = false;
            };

            vm.ConfirmRequested += async (s, entry) =>
            {
                _hasOpenDialog = true;
                var detailVm = new ConfirmEntryDetailViewModel(entry);
                var detailView = new ConfirmEntryDetailView { DataContext = detailVm };

                await detailView.ShowDialog(this);
                _hasOpenDialog = false;

                if (detailView.Confirmed)
                    await vm.DoConfirmEntryAsync(entry);
            };
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !_hasOpenDialog && DataContext is ConfirmEntryViewModel vm)
            {
                vm.GoBackCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}

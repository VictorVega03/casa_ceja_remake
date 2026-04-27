using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
            this.Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is not ConfirmEntryViewModel vm) return;

            // Handler de Enter en el DataGrid (Tunnel para interceptar antes que el DataGrid)
            var grid = this.FindControl<DataGrid>("EntriesGrid");
            if (grid != null)
            {
                grid.AddHandler(KeyDownEvent, OnGridKeyDown, RoutingStrategies.Tunnel);
            }

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

        private void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (_hasOpenDialog || DataContext is not ConfirmEntryViewModel vm) return;

            if (e.Key == Key.Enter && vm.SelectedEntry != null)
            {
                vm.RequestConfirmCommand.Execute(vm.SelectedEntry);
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!_hasOpenDialog && e.Key == Key.Escape && DataContext is ConfirmEntryViewModel vm)
            {
                vm.GoBackCommand.Execute(null);
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }
    }
}

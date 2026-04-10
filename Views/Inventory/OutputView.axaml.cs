using Avalonia.Controls;
using Avalonia.Input;
using System;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class OutputView : Window
    {
        public OutputView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            this.Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is OutputsViewModel vm)
            {
                vm.ShowMessageRequested += async (s, msg) =>
                {
                    await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", msg);
                };
            }
            SearchBox?.Focus();
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not OutputsViewModel vm) return;

            if (e.Key == Key.Escape)
            {
                vm.CancelCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                vm.SaveOutputCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is OutputsViewModel vm)
            {
                vm.SearchProductCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Inventory;
using System;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class EntryView : Window
    {
        public EntryView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            this.Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is EntriesViewModel vm)
            {
                vm.ShowMessageRequested += async (s, msg) =>
                {
                    await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", msg);
                };
            }
            // Focus search box on open
            SearchBox?.Focus();
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not EntriesViewModel vm) return;

            if (e.Key == Key.Escape)
            {
                vm.CancelCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && SearchBox?.IsFocused == true)
            {
                // Enter in search box is handled by OnSearchBoxKeyDown
            }
            else if (e.Key == Key.F5)
            {
                vm.SaveEntryCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is EntriesViewModel vm)
                {
                    vm.SearchProductCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            // LineTotal recalculates automatically via [NotifyPropertyChangedFor]
            // This handler is kept for potential future use
        }
    }
}

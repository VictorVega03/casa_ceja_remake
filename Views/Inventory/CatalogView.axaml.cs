using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;
using System;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class CatalogView : Window
    {
        private CatalogViewModel? _subscribedViewModel;
        private bool _isDialogOpen;

        public CatalogView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.StockDataReady -= OnStockDataReady;
            }

            if (DataContext is CatalogViewModel vm)
            {
                _subscribedViewModel = vm;
                vm.StockDataReady += OnStockDataReady;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.StockDataReady -= OnStockDataReady;
                _subscribedViewModel = null;
            }

            base.OnClosed(e);
        }

        private async void OnStockDataReady(object? sender, (CasaCejaRemake.Models.Product Product, System.Collections.Generic.List<CasaCejaRemake.Models.ProductStockItem> Items, bool IsFromCache, System.Collections.Generic.List<CasaCejaRemake.Models.Branch> AllBranches) data)
        {
            _isDialogOpen = true;
            await DialogHelper.ShowStockDialog(this, data.Product, data.Items, data.IsFromCache, data.AllBranches);
            _isDialogOpen = false;
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (_isDialogOpen) return;

            if (DataContext is CatalogViewModel vm)
            {
                if (e.Key == Key.F1)
                {
                    SearchBox?.Focus();
                    SearchBox?.SelectAll();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Escape)
                {
                    vm.GoBackCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F2)
                {
                    vm.CreateProductCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F3)
                {
                    vm.ShowStockCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F4)
                {
                    vm.ClearSearchCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F5)
                {
                    vm.SearchCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Enter)
                {
                    // Si el foco está en el TextBox de búsqueda, no queremos interceptar el Enter
                    // porque el KeyBinding del TextBox ya desencadena la búsqueda.
                    if (e.Source is TextBox) 
                    {
                        return; // Dejar que el TextBox lo maneje.
                    }

                    if (vm.SelectedProduct != null)
                    {
                        // Open Dialog mode
                        vm.RequestProductDetail(vm.SelectedProduct);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}

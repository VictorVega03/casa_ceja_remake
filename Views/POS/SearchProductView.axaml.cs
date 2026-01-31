using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class SearchProductView : Window
    {
        private SearchProductViewModel? _viewModel;

        public SearchProductView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as SearchProductViewModel;
            TxtSearch.Focus();
            TxtSearch.SelectAll();

            // Handler para seleccionar todo el texto al recibir focus
            TxtSearch.GotFocus += (s, args) => TxtSearch.SelectAll();

            if (GridResults != null)
            {
                GridResults.DoubleTapped += GridResults_DoubleTapped;
                // Usar PreviewKeyDown para interceptar Enter en el DataGrid
                GridResults.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // PreviewKeyDown (Tunneling) - interceptar Enter ANTES del DataGrid
            if (e.Key == Key.Enter && _viewModel?.SelectedProduct != null)
            {
                _viewModel.ConfirmCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void GridResults_DoubleTapped(object? sender, TappedEventArgs e)
        {
            _viewModel?.SelectCurrentProduct();
        }

        private async void OnVerExistenciaClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedProduct == null)
            {
                await DialogHelper.ShowMessageDialog(this, "Aviso", "Seleccione un producto primero.");
                return;
            }

            // Mostrar diálogo de existencia (pendiente de implementación)
            await DialogHelper.ShowMessageDialog(
                this, 
                $"Existencia - {_viewModel.SelectedProduct.Name}",
                "⚠️ Función en desarrollo.\n\nAquí se mostrará la existencia del producto en cada sucursal.");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is SearchProductViewModel vm)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => vm.CancelCommand.Execute(null) },
                    { Key.F3, () => { TxtSearch.Focus(); TxtSearch.SelectAll(); } }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // Enter solo busca si está en el TextBox de búsqueda
                if (e.Key == Key.Enter && TxtSearch.IsFocused)
                {
                    vm.SearchCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // Handle navigation for Down/Up keys
                if (e.Key == Key.Down)
                {
                    if (TxtSearch.IsFocused && vm.SearchResults.Count > 0)
                    {
                        if (GridResults != null)
                        {
                            GridResults.Focus();
                            vm.SelectedProductIndex = 0;
                        }
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Up)
                {
                    if (GridResults?.IsFocused == true && vm.SelectedProductIndex == 0)
                    {
                        TxtSearch.Focus();
                        TxtSearch.SelectAll();
                    }
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
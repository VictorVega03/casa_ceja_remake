using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class SalesHistoryView : Window
    {
        private SalesHistoryViewModel? _viewModel;

        public SalesHistoryView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as SalesHistoryViewModel;

            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.ItemSelected += OnItemSelected;
                _viewModel.ReprintRequested += OnReprintRequested;
                _viewModel.ExportRequested += OnExportRequested;
            }

            // Configurar PreviewKeyDown en DataGrid para interceptar Enter ANTES del DataGrid
            if (DataGridItems != null)
            {
                DataGridItems.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }

            // Enfocar el campo de búsqueda
            SearchTextBox.Focus();
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // PreviewKeyDown (Tunneling) se ejecuta ANTES de que el DataGrid procese la tecla
            if (e.Key == Key.Enter && _viewModel?.SelectedItem != null)
            {
                _viewModel.ViewDetailCommand.Execute(null);
                e.Handled = true; // Evitar que el DataGrid navegue a la siguiente fila
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel == null)
            {
                base.OnKeyDown(e);
                return;
            }

            // F1 para enfocar búsqueda
            if (e.Key == Key.F1)
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                e.Handled = true;
                return;
            }

            var shortcuts = new Dictionary<Key, Action>
            {
                { Key.Escape, () => Close() },
                { Key.F5, () => _viewModel.ApplyFiltersCommand.Execute(null) },
                { Key.F6, () => _viewModel.ClearFiltersCommand.Execute(null) },
                { Key.F7, () => _viewModel.ExecuteSearchCommand.Execute(null) },
                { Key.F8, () => _viewModel.ExportToExcelCommand.Execute(null) }
            };

            if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
            {
                return;
            }

            // Page navigation
            if (e.Key == Key.PageDown)
            {
                _viewModel.NextPageCommand.Execute(null);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.PageUp)
            {
                _viewModel.PreviousPageCommand.Execute(null);
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }



        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private async void OnItemSelected(object? sender, SaleListItemWrapper item)
        {
            // Mostrar vista de detalle
            await ShowSaleDetailAsync(item);
        }

        private async System.Threading.Tasks.Task ShowSaleDetailAsync(SaleListItemWrapper item)
        {
            if (_viewModel == null) return;

            var detailView = new SaleDetailView();
            var detailViewModel = new SaleDetailViewModel(_viewModel.SalesService);

            // IMPORTANT: Asignar DataContext ANTES de cargar datos
            detailView.DataContext = detailViewModel;
            
            detailViewModel.SetSale(item.Sale, item.UserName);
            await detailViewModel.LoadProductsCommand.ExecuteAsync(null);

            await detailView.ShowDialog(this);
        }

        private async void OnReprintRequested(object? sender, (CasaCejaRemake.Models.Sale Sale, string TicketText) args)
        {
            await DialogHelper.ShowTicketDialog(this, args.Sale.Folio, args.TicketText);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.ItemSelected -= OnItemSelected;
                _viewModel.ReprintRequested -= OnReprintRequested;
                _viewModel.ExportRequested -= OnExportRequested;
            }
            
            base.OnClosed(e);
        }

        private async void OnExportRequested(object? sender, EventArgs e)
        {
            if (_viewModel == null || App.ExportService == null) return;

            var sheets = await _viewModel.PrepareMultiSheetExportAsync(App.ExportService);
            
            await ExportHelper.ExportMultiSheetAsync(
                this,
                sheets,
                "Reporte de Ventas");
        }
    }
}

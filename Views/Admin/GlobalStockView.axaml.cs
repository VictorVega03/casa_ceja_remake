using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.Models.DTOs;
using CasaCejaRemake.Services;
using CasaCejaRemake.ViewModels.Admin;

namespace CasaCejaRemake.Views.Admin
{
    public partial class GlobalStockView : Window
    {
        private GlobalStockViewModel? _viewModel;

        public GlobalStockView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is GlobalStockViewModel vm)
            {
                _viewModel = vm;

                _viewModel.GoBackRequested += (_, _) => Close();

                _viewModel.NetworkErrorOccurred += async (_, msg) =>
                {
                    await DialogHelper.ShowMessageDialog(this, msg, "Sin conexión");
                };

                _viewModel.ExportRequested += async (_, _) =>
                {
                    if (_viewModel == null) return;

                    var columns = new List<ExportColumn<ProductStockDto>>
                    {
                        new() { Header = "Código",     ValueSelector = s => s.Product?.Barcode  ?? string.Empty },
                        new() { Header = "Nombre",     ValueSelector = s => s.Product?.Name     ?? string.Empty },
                        new() { Header = "Sucursal",   ValueSelector = s => s.Branch?.Name      ?? string.Empty },
                        new() { Header = "Precio",     ValueSelector = s => s.Product?.PriceRetail ?? 0m, Format = "$#,##0.00" },
                        new() { Header = "Existencia", ValueSelector = s => s.Quantity },
                    };

                    await Helpers.ExportHelper.ExportSingleSheetAsync(
                        this,
                        _viewModel.StockItems,
                        columns,
                        sheetName:   "Existencias",
                        reportTitle: "Existencias por Sucursal",
                        fileBaseName: "Existencias_productos");
                };

                await _viewModel.LoadAsync(1);
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}

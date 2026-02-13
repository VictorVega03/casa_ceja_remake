using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CashCloseHistoryView : Window
    {
        private CashCloseHistoryViewModel? _viewModel;

        public CashCloseHistoryView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            // Asegurar que la ventana tenga focus para recibir eventos de teclado
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CashCloseHistoryViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.ItemSelected += OnItemSelected;
                _viewModel.ExportRequested += OnExportRequested;
                
                // Seleccionar primer item automáticamente si hay items
                if (_viewModel.Items.Count > 0)
                {
                    _viewModel.SelectedItem = _viewModel.Items[0];
                }
            }
            
            // Configurar el TextBox de búsqueda
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            if (searchTextBox != null)
            {
                searchTextBox.KeyDown += SearchTextBox_KeyDown;
            }
            
            // Establecer focus en el DataGrid
            var dataGrid = this.FindControl<DataGrid>("DataGridItems");
            if (dataGrid != null)
            {
                dataGrid.Focus();
                
                // Usar PreviewKeyDown (Tunneling) para interceptar Enter ANTES del DataGrid
                dataGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnItemSelected(object? sender, CashCloseListItemWrapper item)
        {
            Tag = ("ItemSelected", item);
            Close();
        }

        private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel != null)
            {
                _viewModel.ExecuteSearchCommand.Execute(null);
                e.Handled = true;
                
                // Mover focus al DataGrid después de buscar
                var dataGrid = this.FindControl<DataGrid>("DataGridItems");
                dataGrid?.Focus();
            }
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // PreviewKeyDown (Tunneling) se ejecuta ANTES de que el DataGrid procese la tecla
            if (e.Key == Key.Enter && _viewModel?.SelectedItem != null)
            {
                _viewModel.SelectItemCommand.Execute(null);
                e.Handled = true; // Evitar que el DataGrid navegue a la siguiente fila
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CloseCommand.Execute(null) },
                    { Key.F5, () => _viewModel.ApplyDateFilterCommand.Execute(null) },
                    { Key.F6, () => _viewModel.ClearFiltersCommand.Execute(null) },
                    { Key.F7, () => {
                        var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
                        searchTextBox?.Focus();
                        searchTextBox?.SelectAll();
                    }},
                    { Key.F8, () => _viewModel.ExportToExcelCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.ItemSelected -= OnItemSelected;
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
                "Reporte de Cortes");
        }
    }
}

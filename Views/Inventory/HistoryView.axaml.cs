using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Inventory;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Views.POS;
using static CasaCejaRemake.Helpers.FileHelper;
using System.Collections.Generic;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class HistoryView : Window
    {
        private HistoryViewModel? _viewModel;
        internal bool IsDetailOpen { get; set; }

        public HistoryView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, System.EventArgs e)
        {
            _viewModel = DataContext as HistoryViewModel;

            if (HistoryDataGrid != null)
            {
                HistoryDataGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }

            if (ExportButton != null)
            {
                ExportButton.Click += async (_, __) => await OnExportRequestedAsync();
            }
        }

        private async System.Threading.Tasks.Task OnExportRequestedAsync()
        {
            if (_viewModel == null || App.ExportService == null || IsDetailOpen) return;

            var choice = await casa_ceja_remake.Helpers.DialogHelper.ShowInventoryExportTypeDialog(this);
            if (choice == casa_ceja_remake.Helpers.DialogHelper.InventoryExportChoice.Cancelar) return;

            bool entradas = choice != casa_ceja_remake.Helpers.DialogHelper.InventoryExportChoice.Salidas;
            bool salidas  = choice != casa_ceja_remake.Helpers.DialogHelper.InventoryExportChoice.Entradas;

            var sheets = await _viewModel.PrepareExportAsync(App.ExportService, entradas, salidas);

            var fileName = choice switch
            {
                casa_ceja_remake.Helpers.DialogHelper.InventoryExportChoice.Entradas => "Historial de Entradas",
                casa_ceja_remake.Helpers.DialogHelper.InventoryExportChoice.Salidas  => "Historial de Salidas",
                _                                                                     => "Historial Entradas y Salidas"
            };

            await ExportHelper.ExportMultiSheetAsync(this, sheets, fileName, DocumentModule.Inventario);
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel?.SelectedItem != null)
            {
                _viewModel.RequestDetail(_viewModel.SelectedItem);
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsDetailOpen) return;

            if (_viewModel == null)
            {
                base.OnKeyDown(e);
                return;
            }

            var shortcuts = new Dictionary<Key, System.Action>
            {
                { Key.Escape, () => _viewModel.GoBackCommand.Execute(null) },
                { Key.Enter, () => {
                    if (_viewModel.SelectedItem != null)
                        _viewModel.RequestDetail(_viewModel.SelectedItem);
                }},
                { Key.F8, () => _ = OnExportRequestedAsync() }
            };

            if (casa_ceja_remake.Helpers.KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
            {
                return;
            }

            base.OnKeyDown(e);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Views.POS;
using CasaCejaRemake.ViewModels.Inventory;
using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class EntryView : Window
    {
        private EntriesViewModel? _viewModel;
        private bool _allowClose;
        private bool _hasOpenDialog;
        private DispatcherTimer? _quantityTimer;
        private Key _currentArrowKey;

        public EntryView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDownGlobal, RoutingStrategies.Tunnel, handledEventsToo: true);
            this.AddHandler(InputElement.KeyUpEvent, OnPreviewKeyUpGlobal, RoutingStrategies.Tunnel, handledEventsToo: true);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as EntriesViewModel;

            if (_viewModel != null)
            {
                _viewModel.ShowMessageRequested += async (s, msg) =>
                {
                    _hasOpenDialog = true;
                    await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", msg);
                    _hasOpenDialog = false;
                };

                _viewModel.ShowSuccessRequested += async (s, msg) =>
                {
                    _hasOpenDialog = true;
                    await casa_ceja_remake.Helpers.DialogHelper.ShowResultDialog(
                        this, true, "Entrada guardada exitosamente", msg);
                    _hasOpenDialog = false;
                    _allowClose = true;
                    _viewModel.CancelCommand.Execute(null);
                };

                _viewModel.ShowErrorRequested += async (s, msg) =>
                {
                    _hasOpenDialog = true;
                    await casa_ceja_remake.Helpers.DialogHelper.ShowResultDialog(
                        this, false, "Error al guardar la entrada", msg);
                    _hasOpenDialog = false;
                };

                _viewModel.RequestConfirmSave += async (s, data) =>
                {
                    _hasOpenDialog = true;
                    var confirmed = await casa_ceja_remake.Helpers.DialogHelper.ShowEntryConfirmDialog(
                        this, data.BranchName, data.SupplierName, data.ProductCount, data.TotalAmount);
                    _hasOpenDialog = false;
                    if (confirmed)
                        await _viewModel.DoSaveEntryAsync();
                };

                _viewModel.OpenPosCatalogRequested += OnOpenPosCatalogRequested;
                _viewModel.ProductAddedOrUpdated += OnProductAddedOrUpdated;
                _viewModel.GoBackRequested += (s, args) => _allowClose = true;
            }

            SearchBox?.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            HandleGlobalShortcuts(e);

            if (e.Handled)
            {
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if ((e.Key == Key.Left || e.Key == Key.Right) && _quantityTimer != null && _quantityTimer.IsEnabled)
            {
                _quantityTimer.Stop();
                e.Handled = true;
            }

            base.OnKeyUp(e);
        }

        private void OnPreviewKeyDownGlobal(object? sender, KeyEventArgs e)
        {
            HandleGlobalShortcuts(e);
        }

        private void OnPreviewKeyUpGlobal(object? sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Left || e.Key == Key.Right) && _quantityTimer != null && _quantityTimer.IsEnabled)
            {
                _quantityTimer.Stop();
                e.Handled = true;
            }
        }

        private void HandleGlobalShortcuts(KeyEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            // Cuando el foco está en el buscador, las flechas deben comportarse
            // como navegación de texto y NO ajustar cantidades.
            if ((e.Key == Key.Left || e.Key == Key.Right) && SearchBox?.IsFocused == true)
            {
                return;
            }

            // Ajuste de cantidad con flechas izquierda/derecha (incluye repetición al mantener presionado)
            if ((e.Key == Key.Left || e.Key == Key.Right) && _viewModel.SelectedLine != null)
            {
                HandleQuantityArrowKey(e.Key);
                e.Handled = true;
                return;
            }

            var shortcuts = new Dictionary<Key, Action>
            {
                { Key.F5, () => _viewModel.SaveEntryCommand.Execute(null) }
            };

            if (casa_ceja_remake.Helpers.KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
            {
                return;
            }

            if (e.Key == Key.F1)
            {
                SearchBox?.Focus();
                SearchBox?.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F3)
            {
                _viewModel.OpenCatalogCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && !_hasOpenDialog)
            {
                _ = ConfirmAndExitAsync();
                e.Handled = true;
                return;
            }
        }

        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            if (_allowClose)
            {
                base.OnClosing(e);
                return;
            }

            if (_hasOpenDialog)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = true;

            var confirmed = await casa_ceja_remake.Helpers.DialogHelper.ShowConfirmDialog(
                this,
                "Salir de Entradas",
                "¿Deseas salir de la vista de entradas?\n\nLos cambios no guardados se perderán.");

            if (confirmed)
            {
                _allowClose = true;
                _viewModel?.CancelCommand.Execute(null);
            }
        }

        private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel?.SearchProductCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
        }

        private void OnQuantityTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var text = tb.Text ?? string.Empty;
            var clean = new string(text.Where(char.IsDigit).ToArray());
            if (clean != text)
                tb.Text = clean;
        }

        private void OnQuantityLostFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (string.IsNullOrWhiteSpace(tb.Text) || !int.TryParse(tb.Text, out var val) || val < 1)
            {
                tb.Text = "1";
                if (tb.DataContext is EntryLineItem line)
                    line.Quantity = 1;
            }
            else if (tb.DataContext is EntryLineItem lineItem)
            {
                lineItem.Quantity = val;
            }
        }

        private async void OnOpenPosCatalogRequested(object? sender, string initialSearchTerm)
        {
            var app = App.Current as App;
            var salesService = app?.GetSaleService();

            if (salesService == null || _viewModel == null)
            {
                await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", "No se pudo abrir el catálogo POS.");
                return;
            }

            var searchView = new SearchProductView();
            var searchViewModel = new SearchProductViewModel(salesService);

            if (!string.IsNullOrWhiteSpace(initialSearchTerm))
            {
                searchViewModel.SearchTerm = initialSearchTerm;
            }

            await searchViewModel.InitializeAsync();
            searchView.DataContext = searchViewModel;

            searchViewModel.ProductSelected += async (s, args) =>
            {
                var (product, quantity) = args;
                await _viewModel.AddProductFromPosCatalogAsync(product, quantity);
                searchView.Close();
            };

            searchViewModel.Cancelled += (s, args) => searchView.Close();

            await searchView.ShowDialog(this);
            SearchBox?.Focus();
        }

        private void OnProductAddedOrUpdated(object? sender, EntryLineItem line)
        {
            if (_viewModel == null || LinesGrid == null) return;

            Dispatcher.UIThread.Post(() =>
            {
                _viewModel.SelectedLine = line;
                LinesGrid.ScrollIntoView(line, null);

                var qtyEditor = LinesGrid
                    .GetVisualDescendants()
                    .OfType<TextBox>()
                    .FirstOrDefault(tb => ReferenceEquals(tb.DataContext, line));

                qtyEditor?.Focus();
                qtyEditor?.SelectAll();
            }, DispatcherPriority.Background);
        }

        private void HandleQuantityArrowKey(Key key)
        {
            _currentArrowKey = key;
            ChangeQuantityByArrow(key);

            _quantityTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(140)
            };

            _quantityTimer.Tick -= OnQuantityTimerTick;
            _quantityTimer.Tick += OnQuantityTimerTick;

            if (!_quantityTimer.IsEnabled)
            {
                _quantityTimer.Start();
            }
        }

        private void OnQuantityTimerTick(object? sender, EventArgs e)
        {
            ChangeQuantityByArrow(_currentArrowKey);
        }

        private void ChangeQuantityByArrow(Key key)
        {
            var line = _viewModel?.SelectedLine;
            if (line == null) return;

            if (key == Key.Left)
            {
                line.Quantity = Math.Max(1, line.Quantity - 1);
            }
            else if (key == Key.Right)
            {
                line.Quantity += 1;
            }
        }

        private async System.Threading.Tasks.Task ConfirmAndExitAsync()
        {
            if (_viewModel == null) return;

            var confirmed = await casa_ceja_remake.Helpers.DialogHelper.ShowConfirmDialog(
                this,
                "Salir de Entradas",
                "¿Deseas salir de la vista de entradas?\n\nLos cambios no guardados se perderán.");

            if (!confirmed) return;

            _allowClose = true;
            _viewModel.CancelCommand.Execute(null);
        }
    }
}

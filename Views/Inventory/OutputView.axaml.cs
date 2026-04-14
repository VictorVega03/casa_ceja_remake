using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Views.POS;
using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class OutputView : Window
    {
        private OutputsViewModel? _viewModel;
        private bool _allowClose;
        private DispatcherTimer? _quantityTimer;
        private Key _currentArrowKey;

        public OutputView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDownGlobal, RoutingStrategies.Tunnel, handledEventsToo: true);
            this.AddHandler(InputElement.KeyUpEvent, OnPreviewKeyUpGlobal, RoutingStrategies.Tunnel, handledEventsToo: true);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as OutputsViewModel;

            if (_viewModel != null)
            {
                _viewModel.ShowMessageRequested += async (s, msg) =>
                {
                    await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", msg);
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

            if ((e.Key == Key.Left || e.Key == Key.Right) && _viewModel.SelectedLine != null)
            {
                HandleQuantityArrowKey(e.Key);
                e.Handled = true;
                return;
            }

            var shortcuts = new Dictionary<Key, Action>
            {
                { Key.F5, () => _viewModel.SaveOutputCommand.Execute(null) }
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

            if (e.Key == Key.Escape)
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

            e.Cancel = true;

            var confirmed = await casa_ceja_remake.Helpers.DialogHelper.ShowConfirmDialog(
                this,
                "Salir de Salidas",
                "¿Deseas salir de la vista de salidas?\n\nLos cambios no guardados se perderán.");

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
                if (tb.DataContext is OutputLineItem line)
                    line.Quantity = 1;
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

        private void OnProductAddedOrUpdated(object? sender, OutputLineItem line)
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
                "Salir de Salidas",
                "¿Deseas salir de la vista de salidas?\n\nLos cambios no guardados se perderán.");

            if (!confirmed) return;

            _allowClose = true;
            _viewModel.CancelCommand.Execute(null);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.Services;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Views.POS
{
    public partial class SalesView : Window
    {
        private SalesViewModel? _viewModel;
        private DispatcherTimer? _timer;

        public SalesView()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as SalesViewModel;

            if (_viewModel != null)
            {
                _viewModel.RequestFocusBarcode += OnRequestFocusBarcode;
                _viewModel.RequestShowSearchProduct += OnRequestShowSearchProduct;
                _viewModel.RequestShowPayment += OnRequestShowPayment;
                _viewModel.RequestShowModifyQuantity += OnRequestShowModifyQuantity;
                _viewModel.RequestShowCreditsLayaways += OnRequestShowCreditsLayaways;
                _viewModel.ShowMessage += OnShowMessage;
                _viewModel.RequestExit += OnRequestExit;
                _viewModel.SaleCompleted += OnSaleCompleted;
            }

            TxtBarcode.Focus();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _timer?.Stop();
            _timer = null;

            if (_viewModel != null)
            {
                _viewModel.RequestFocusBarcode -= OnRequestFocusBarcode;
                _viewModel.RequestShowSearchProduct -= OnRequestShowSearchProduct;
                _viewModel.RequestShowPayment -= OnRequestShowPayment;
                _viewModel.RequestShowModifyQuantity -= OnRequestShowModifyQuantity;
                _viewModel.RequestShowCreditsLayaways -= OnRequestShowCreditsLayaways;
                _viewModel.ShowMessage -= OnShowMessage;
                _viewModel.RequestExit -= OnRequestExit;
                _viewModel.SaleCompleted -= OnSaleCompleted;
                _viewModel.Cleanup();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _viewModel?.UpdateClock();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var modifiers = e.KeyModifiers;

            switch (e.Key)
            {
                case Key.F1:
                    _viewModel?.FocusBarcodeCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F2:
                    _viewModel?.ModifyQuantityCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F3:
                    _viewModel?.SearchProductCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F4:
                    if (modifiers.HasFlag(KeyModifiers.Alt))
                    {
                        _viewModel?.ExitCommand.Execute(null);
                    }
                    else
                    {
                        _viewModel?.ChangeCollectionCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.F5:
                    if (modifiers.HasFlag(KeyModifiers.Shift))
                    {
                        _viewModel?.ClearCartCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.F11:
                    _viewModel?.PayCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F6:
                    ShowCashMovementDialogAsync(true); // Gasto
                    e.Handled = true;
                    break;

                case Key.F7:
                    ShowCashMovementDialogAsync(false); // Ingreso
                    e.Handled = true;
                    break;

                case Key.F10:
                    ShowCashCloseDialogAsync();
                    e.Handled = true;
                    break;

                case Key.F12:
                    _viewModel?.CreditsLayawaysCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Delete:
                    _viewModel?.RemoveProductCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (TxtBarcode.IsFocused)
                    {
                        _ = _viewModel?.SearchByCodeCommand.ExecuteAsync(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    TxtBarcode.Focus();
                    e.Handled = true;
                    break;
            }
        }

        private void OnRequestFocusBarcode(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                TxtBarcode.Focus();
                TxtBarcode.SelectAll();
            });
        }

        private async void OnRequestShowSearchProduct(object? sender, EventArgs e)
        {
            var searchView = new SearchProductView();
            var saleService = ((App)Application.Current!).GetSaleService();
            
            if (saleService != null)
            {
                var searchViewModel = new SearchProductViewModel(saleService);
                await searchViewModel.InitializeAsync();
                searchView.DataContext = searchViewModel;

                searchViewModel.ProductSelected += async (s, args) =>
                {
                    var (product, quantity) = args;
                    if (_viewModel != null)
                    {
                        await _viewModel.AddProductFromSearchAsync(product, quantity);
                    }
                    searchView.Close();
                };

                searchViewModel.Cancelled += (s, args) =>
                {
                    searchView.Close();
                };

                await searchView.ShowDialog(this);
            }

            TxtBarcode.Focus();
        }

        private async void OnRequestShowPayment(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;

            var paymentView = new PaymentView();
            var paymentViewModel = new PaymentViewModel(_viewModel.Total, _viewModel.TotalItems);
            paymentView.DataContext = paymentViewModel;

            paymentViewModel.PaymentConfirmed += async (s, args) =>
            {
                var (paymentMethod, amountPaid) = args;
                var result = await _viewModel.ProcessPaymentAsync((Models.PaymentMethod)paymentMethod, amountPaid);
                
                if (result.Success)
                {
                    paymentView.Tag = result;
                    paymentView.Close();
                }
            };

            paymentViewModel.PaymentCancelled += (s, args) =>
            {
                paymentView.Close();
            };

            await paymentView.ShowDialog(this);
            TxtBarcode.Focus();
        }

        private async void OnRequestShowModifyQuantity(object? sender, EventArgs e)
        {
            if (_viewModel == null || _viewModel.SelectedItemIndex < 0) return;

            var item = _viewModel.Items[_viewModel.SelectedItemIndex];
            
            var dialog = new Window
            {
                Title = "Modify Quantity",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = Avalonia.Media.Brushes.DimGray
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(15),
                Spacing = 10
            };

            var labelProduct = new TextBlock
            {
                Text = item.ProductName,
                Foreground = Avalonia.Media.Brushes.White,
                FontWeight = Avalonia.Media.FontWeight.Bold
            };

            var inputQuantity = new NumericUpDown
            {
                Value = item.Quantity,
                Minimum = 1,
                Maximum = 9999,
                Increment = 1,
                FormatString = "0"
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var btnAccept = new Button { Content = "Accept", Width = 80 };
            var btnCancel = new Button { Content = "Cancel", Width = 80 };

            btnAccept.Click += async (s, args) =>
            {
                var newQuantity = (int)(inputQuantity.Value ?? 1);
                await _viewModel.ApplyNewQuantityAsync(newQuantity);
                dialog.Close();
            };

            btnCancel.Click += (s, args) =>
            {
                dialog.Close();
            };

            buttonsPanel.Children.Add(btnCancel);
            buttonsPanel.Children.Add(btnAccept);

            stackPanel.Children.Add(labelProduct);
            stackPanel.Children.Add(inputQuantity);
            stackPanel.Children.Add(buttonsPanel);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(this);
            TxtBarcode.Focus();
        }

        private async void OnRequestShowCreditsLayaways(object? sender, EventArgs e)
        {
            var app = (App)Application.Current!;
            var creditService = app.GetCreditService();
            var layawayService = app.GetLayawayService();
            var customerService = app.GetCustomerService();
            var authService = app.GetAuthService();

            if (creditService == null || layawayService == null || customerService == null || authService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return;
            }

            // Crear vista de créditos/apartados con servicios y carrito
            var menuView = new CreditsLayawaysMenuView(
                creditService,
                layawayService,
                customerService,
                authService,
                _viewModel?.BranchId ?? 1,
                _viewModel?.Items.ToList());

            var menuViewModel = new CreditsLayawaysMenuViewModel();
            menuView.DataContext = menuViewModel;

            await menuView.ShowDialog(this);

            // Si se creó un crédito o apartado, limpiar el carrito
            if (menuView.Tag is string result && result == "ItemCreated")
            {
                _viewModel?.ClearCartCommand.Execute(null);
            }

            TxtBarcode.Focus();
        }

        private async void ShowMessageDialog(string title, string message)
        {
            await DialogHelper.ShowMessageDialog(this, title, message);
        }

        private async void OnShowMessage(object? sender, string message)
        {
            var dialog = new Window
            {
                Title = "Aviso",
                Width = 350,
                Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = Avalonia.Media.Brushes.DimGray
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(15),
                Spacing = 15
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = Avalonia.Media.Brushes.White,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var btnOk = new Button
            {
                Content = "Aceptar",
                Width = 80,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            btnOk.Click += (s, args) => dialog.Close();

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(btnOk);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(this);
        }

        private void OnRequestExit(object? sender, EventArgs e)
        {
            Tag = "exit";
            Close();
        }

        private async void ShowTicketDialog(string folio, string ticketText)
        {
            await DialogHelper.ShowTicketDialog(this, folio, ticketText);
        }

        private async void OnSaleCompleted(object? sender, SaleResult result)
        {
            if (result.Success && result.TicketText != null)
            {
                ShowTicketDialog(result.Sale?.Folio ?? "N/A", result.TicketText);
            }

            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra el diálogo para agregar un gasto o ingreso.
        /// </summary>
        private async void ShowCashMovementDialogAsync(bool isExpense)
        {
            var app = (App)Application.Current!;
            var cashCloseService = app.GetCashCloseService();
            var authService = app.GetAuthService();

            if (cashCloseService == null || authService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return;
            }

            // Verificar que hay caja abierta
            var openCash = await cashCloseService.GetOpenCashAsync(_viewModel?.BranchId ?? 1);
            if (openCash == null)
            {
                ShowMessageDialog("Sin caja abierta", "No hay una caja abierta para registrar movimientos.");
                return;
            }

            var movementView = new CashMovementView();
            var movementViewModel = new CashMovementViewModel(
                cashCloseService,
                openCash.Id,
                authService.CurrentUser?.Id ?? 0,
                isExpense);
            
            movementView.DataContext = movementViewModel;
            await movementView.ShowDialog(this);

            if (movementView.Tag is CashMovement movement)
            {
                var tipo = movement.IsExpense ? "Gasto" : "Ingreso";
                OnShowMessage(this, $"{tipo} registrado: {movement.Concept} - ${movement.Amount:N2}");
            }

            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra el diálogo de corte de caja.
        /// </summary>
        private async void ShowCashCloseDialogAsync()
        {
            var app = (App)Application.Current!;
            var cashCloseService = app.GetCashCloseService();
            var authService = app.GetAuthService();

            if (cashCloseService == null || authService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return;
            }

            // Verificar que hay caja abierta
            var openCash = await cashCloseService.GetOpenCashAsync(_viewModel?.BranchId ?? 1);
            if (openCash == null)
            {
                ShowMessageDialog("Sin caja abierta", "No hay una caja abierta para realizar el corte.");
                return;
            }

            var closeView = new CashCloseView();
            var closeViewModel = new CashCloseViewModel(cashCloseService, authService, openCash);
            closeView.DataContext = closeViewModel;

            await closeView.ShowDialog(this);

            if (closeView.Tag is CashClose closedCash)
            {
                // Corte completado exitosamente
                OnShowMessage(this, $"Corte de caja completado. Folio: {closedCash.Folio}");
                
                // Salir del POS (el usuario debe volver a abrir caja)
                Tag = "exit";
                Close();
            }

            TxtBarcode.Focus();
        }
    }
}
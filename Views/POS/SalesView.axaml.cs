using System;
using System.Collections.Generic;
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
        private bool _hasOpenDialog = false;

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
            Loaded += OnWindowLoaded;
            Closed += OnClosed;
        }

        private void OnWindowLoaded(object? sender, RoutedEventArgs e)
        {
            // Wire up the menu button to open context menu on click
            var btnExitMenu = this.FindControl<Button>("BtnExitMenu");
            if (btnExitMenu != null)
            {
                btnExitMenu.Click += (s, args) =>
                {
                    if (btnExitMenu.ContextMenu != null)
                    {
                        btnExitMenu.ContextMenu.Open(btnExitMenu);
                    }
                };
            }
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
                _viewModel.RequestClearCartConfirmation += OnRequestClearCartConfirmation;
                _viewModel.RequestExitConfirmation += OnRequestExitConfirmation;
                _viewModel.CollectionIndicatorsChanged += OnCollectionIndicatorsChanged;
            }

            // Configurar botones de cobranza
            SetupCollectionButtons();
            UpdateCollectionIndicators();

            // Configurar botón de clientes
            var btnCustomers = this.FindControl<Button>("BtnCustomers");
            if (btnCustomers != null)
            {
                btnCustomers.Click += (s, e) => ShowCustomersDialogAsync();
            }

            TxtBarcode.Focus();
        }

        private void SetupCollectionButtons()
        {
            // Botones de navegación
            var btnPrev = this.FindControl<Button>("BtnPrevCollection");
            var btnNext = this.FindControl<Button>("BtnNextCollection");
            
            if (btnPrev != null)
                btnPrev.Click += (s, e) => _viewModel?.ChangeCollectionPreviousCommand.Execute(null);
            if (btnNext != null)
                btnNext.Click += (s, e) => _viewModel?.ChangeCollectionCommand.Execute(null);

            // Botones de selección directa
            var btnA = this.FindControl<Button>("BtnCollectionA");
            var btnB = this.FindControl<Button>("BtnCollectionB");
            var btnC = this.FindControl<Button>("BtnCollectionC");
            var btnD = this.FindControl<Button>("BtnCollectionD");

            if (btnA != null) btnA.Click += (s, e) => SelectCollection('A');
            if (btnB != null) btnB.Click += (s, e) => SelectCollection('B');
            if (btnC != null) btnC.Click += (s, e) => SelectCollection('C');
            if (btnD != null) btnD.Click += (s, e) => SelectCollection('D');
        }

        private void SelectCollection(char identifier)
        {
            _viewModel?.SelectCollectionCommand.Execute(identifier);
            UpdateCollectionIndicators();
        }

        private void UpdateCollectionIndicators()
        {
            if (_viewModel == null) return;

            var collections = _viewModel.GetAllCollectionsInfo();
            
            foreach (var (id, items, total, isActive) in collections)
            {
                var btn = this.FindControl<Button>($"BtnCollection{id}");
                if (btn != null)
                {
                    // Color según estado
                    if (isActive)
                    {
                        btn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#66BB6A")); // Verde - activa
                        btn.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
                    }
                    else if (items > 0)
                    {
                        btn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF9800")); // Naranja - tiene items
                        btn.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
                    }
                    else
                    {
                        btn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#444")); // Gris - vacía
                        btn.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#888"));
                    }
                }
            }
        }

        private void OnCollectionIndicatorsChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() => UpdateCollectionIndicators());
        }

        /// <summary>
        /// Muestra el diálogo de búsqueda de clientes.
        /// </summary>
        private async void ShowCustomersDialogAsync()
        {
            var app = (App)Application.Current!;
            var customerService = app.GetCustomerService();

            if (customerService == null)
            {
                ShowMessageDialog("Error", "Servicio de clientes no disponible");
                return;
            }

            var searchView = new CustomerSearchView();
            var searchViewModel = new CustomerSearchViewModel(customerService);
            searchView.DataContext = searchViewModel;
            
            // Inicializar para cargar clientes (también se hace en OnLoaded pero por si acaso)
            await searchViewModel.InitializeAsync();

            searchViewModel.CustomerSelected += (s, customer) =>
            {
                if (customer != null)
                {
                    OnShowMessage(this, $"Cliente seleccionado: {customer.Name}");
                }
                searchView.Close();
            };

            searchViewModel.Cancelled += (s, args) =>
            {
                searchView.Close();
            };

            searchViewModel.CreateNewRequested += async (s, args) =>
            {
                searchView.Close();
                await ShowQuickCustomerDialogAsync();
            };

            _hasOpenDialog = true;
            await searchView.ShowDialog(this);
            _hasOpenDialog = false;

            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra el diálogo de alta rápida de cliente.
        /// </summary>
        private async Task ShowQuickCustomerDialogAsync()
        {
            var app = (App)Application.Current!;
            var customerService = app.GetCustomerService();

            if (customerService == null)
            {
                ShowMessageDialog("Error", "Servicio de clientes no disponible");
                return;
            }

            var quickView = new QuickCustomerView();
            var quickViewModel = new QuickCustomerViewModel(customerService);
            quickView.DataContext = quickViewModel;

            quickViewModel.CustomerCreated += (s, customer) =>
            {
                if (customer != null)
                {
                    OnShowMessage(this, $"Cliente creado: {customer.Name}");
                }
                quickView.Close();
            };

            quickViewModel.Cancelled += (s, args) =>
            {
                quickView.Close();
            };

            _hasOpenDialog = true;
            await quickView.ShowDialog(this);
            _hasOpenDialog = false;

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
                _viewModel.RequestClearCartConfirmation -= OnRequestClearCartConfirmation;
                _viewModel.RequestExitConfirmation -= OnRequestExitConfirmation;
                _viewModel.CollectionIndicatorsChanged -= OnCollectionIndicatorsChanged;
                _viewModel.Cleanup();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _viewModel?.UpdateClock();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Si hay un diálogo abierto, no procesar atajos de la ventana principal
            if (_hasOpenDialog)
            {
                return;
            }

            if (_viewModel != null)
            {
                // Atajos complejos con modificadores (Alt+F4, Shift+F5)
                var complexShortcuts = new Dictionary<(Key, KeyModifiers), Action>
                {
                    { (Key.F4, KeyModifiers.Alt), () => _viewModel.ExitCommand.Execute(null) },
                    { (Key.F5, KeyModifiers.Shift), () => _viewModel.ClearCartCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleComplexShortcut(e, complexShortcuts))
                {
                    return;
                }

                // Atajos simples
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.F1, () => _viewModel.FocusBarcodeCommand.Execute(null) },
                    { Key.F2, () => _viewModel.ModifyQuantityCommand.Execute(null) },
                    { Key.F3, () => _viewModel.SearchProductCommand.Execute(null) },
                    { Key.F4, () => _viewModel.ChangeCollectionCommand.Execute(null) },
                    { Key.F5, () => _viewModel.ChangeCollectionPreviousCommand.Execute(null) },
                    { Key.F6, () => ShowCashMovementDialogAsync(true) },
                    { Key.F7, () => ShowCashMovementDialogAsync(false) },
                    { Key.F8, () => ShowCustomersDialogAsync() },
                    { Key.F10, () => ShowCashCloseDialogAsync() },
                    { Key.F11, () => _viewModel.PayCommand.Execute(null) },
                    { Key.F12, () => _viewModel.CreditsLayawaysCommand.Execute(null) },
                    { Key.Delete, () => _viewModel.RemoveProductCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // Enter con lógica condicional
                if (e.Key == Key.Enter && TxtBarcode.IsFocused)
                {
                    _ = _viewModel.SearchByCodeCommand.ExecuteAsync(null);
                    e.Handled = true;
                    return;
                }
            }

            // Solo propagar si no se manejó
            base.OnKeyDown(e);
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

                _hasOpenDialog = true;
                await searchView.ShowDialog(this);
                _hasOpenDialog = false;
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
                var (paymentJson, totalPaid, change) = args;
                var result = await _viewModel.ProcessPaymentAsync(paymentJson, totalPaid, change);
                
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

            _hasOpenDialog = true;
            await paymentView.ShowDialog(this);
            _hasOpenDialog = false;
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

            _hasOpenDialog = true;
            await dialog.ShowDialog(this);
            _hasOpenDialog = false;
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

            _hasOpenDialog = true;
            await menuView.ShowDialog(this);
            _hasOpenDialog = false;

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

            _hasOpenDialog = true;
            await dialog.ShowDialog(this);
            _hasOpenDialog = false;
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
                _viewModel?.BranchId ?? 1,
                isExpense);
            
            movementView.DataContext = movementViewModel;
            
            _hasOpenDialog = true;
            await movementView.ShowDialog(this);
            _hasOpenDialog = false;

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

            _hasOpenDialog = true;
            await closeView.ShowDialog(this);
            _hasOpenDialog = false;

            if (closeView.Tag is CashCloseResult result && result.CashClose != null)
            {
                // Mostrar ticket de corte si se generó
                if (!string.IsNullOrEmpty(result.TicketText))
                {
                    ShowTicketDialog(result.CashClose.Folio, result.TicketText);
                }
                
                // Corte completado exitosamente
                OnShowMessage(this, $"Corte de caja completado. Folio: {result.CashClose.Folio}");
                
                // Salir del POS (el usuario debe volver a abrir caja)
                Tag = "exit";
                Close();
            }

            TxtBarcode.Focus();
        }

        private async void OnRequestClearCartConfirmation(object? sender, EventArgs e)
        {
            var confirmed = await DialogHelper.ShowConfirmDialog(
                this, 
                "Confirmar", 
                "¿Está seguro de vaciar el carrito?");

            if (confirmed && _viewModel != null)
            {
                _viewModel.ConfirmClearCart();
            }

            TxtBarcode.Focus();
        }

        private async void OnRequestExitConfirmation(object? sender, EventArgs e)
        {
            var confirmed = await DialogHelper.ShowConfirmDialog(
                this, 
                "Salir", 
                "¿Está seguro de salir del Punto de Venta?");

            if (confirmed && _viewModel != null)
            {
                _viewModel.ConfirmExit();
            }

            TxtBarcode.Focus();
        }

        private void OnCashCloseClick(object? sender, RoutedEventArgs e)
        {
            ShowCashCloseDialogAsync();
        }

        private void OnExpenseClick(object? sender, RoutedEventArgs e)
        {
            ShowCashMovementDialogAsync(true);
        }

        private void OnIncomeClick(object? sender, RoutedEventArgs e)
        {
            ShowCashMovementDialogAsync(false);
        }
    }
}
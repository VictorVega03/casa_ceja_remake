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
using Avalonia.VisualTree;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.ViewModels.Shared;
using CasaCejaRemake.Views.Shared;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.Services;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.Results;

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
            // Wire up the new toggle sidebar button
            var btnToggleSidebar = this.FindControl<Button>("BtnToggleSidebar");
            var mainSplitView = this.FindControl<SplitView>("MainSplitView");

            if (btnToggleSidebar != null && mainSplitView != null)
            {
                btnToggleSidebar.Click += (s, args) =>
                {
                    mainSplitView.IsPaneOpen = !mainSplitView.IsPaneOpen;
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
                _viewModel.RequestLogout += OnRequestLogout;
                _viewModel.SaleCompleted += OnSaleCompleted;
                _viewModel.RequestClearCartConfirmation += OnRequestClearCartConfirmation;
                _viewModel.RequestExitConfirmation += OnRequestExitConfirmation;
                _viewModel.RequestLogoutConfirmation += OnRequestLogoutConfirmation;
                _viewModel.CollectionIndicatorsChanged += OnCollectionIndicatorsChanged;
                _viewModel.ProductAddedToCart += OnProductAddedToCart;
                
                // Eventos de descuentos
                _viewModel.ShowDiscountApplied += OnShowDiscountApplied;
                _viewModel.ShowDiscountBlocked += OnShowDiscountBlocked;
                _viewModel.RequestShowGeneralDiscount += OnRequestShowGeneralDiscount;
                _viewModel.RequestAdminVerification += OnRequestAdminVerification;
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

            // Configurar coloreado dinámico de filas del DataGrid
            var gridProducts = this.FindControl<DataGrid>("GridProducts");
            if (gridProducts != null)
            {
                gridProducts.LoadingRow += OnDataGridLoadingRow;
                
                // Usar AddHandler con Tunnel para capturar teclas ANTES de que el DataGrid las procese
                // Esto sobrescribe el comportamiento nativo de cambiar columnas con flechas izq/der
                gridProducts.AddHandler(KeyDownEvent, (sender, e) =>
                {
                    if (e.Key == Key.Delete && _viewModel != null)
                    {
                        if (_viewModel.RemoveProductCommand.CanExecute(null))
                        {
                            _viewModel.RemoveProductCommand.Execute(null);
                        }
                        e.Handled = true; // Prevenir propagación al DataGrid
                        return;
                    }

                    if ((e.Key == Key.Left || e.Key == Key.Right) && _viewModel != null && _viewModel.SelectedItemIndex >= 0)
                    {
                        HandleQuantityArrowKey(e.Key);
                        e.Handled = true; // Prevenir propagación al DataGrid
                    }
                    // Permitir flechas arriba/abajo para navegar por las filas
                }, RoutingStrategies.Tunnel, handledEventsToo: true);
                
                // También capturar KeyUp para el timer
                gridProducts.AddHandler(KeyUpEvent, (sender, e) =>
                {
                    if ((e.Key == Key.Left || e.Key == Key.Right) && _quantityTimer != null && _quantityTimer.IsEnabled)
                    {
                        _quantityTimer.Stop();
                        e.Handled = true;
                    }
                }, RoutingStrategies.Tunnel, handledEventsToo: true);
            }

            // Fix 9: Arrow Down desde barcode → focus al DataGrid
            TxtBarcode.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Down && _viewModel?.Items.Count > 0)
                {
                    var grid = this.FindControl<DataGrid>("GridProducts");
                    if (grid != null)
                    {
                        grid.Focus();
                        if (_viewModel.SelectedItemIndex < 0)
                            _viewModel.SelectedItemIndex = 0;
                    }
                    e.Handled = true;
                }
            };

            // Botón de ayuda - Leyenda de colores
            var btnHelp = this.FindControl<Button>("BtnHelp");
            if (btnHelp != null)
            {
                btnHelp.Click += (s, e) => ShowColorLegendDialog();
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
            var creditService = app.GetCreditService();
            var layawayService = app.GetLayawayService();

            if (customerService == null)
            {
                ShowMessageDialog("Error", "Servicio de clientes no disponible");
                return;
            }

            var searchView = new CustomerSearchView();
            var searchViewModel = new CustomerSearchViewModel(customerService);
            searchViewModel.ShowActionButtons = true;
            searchView.DataContext = searchViewModel;

            // Inicializar para cargar clientes
            await searchViewModel.InitializeAsync();

            searchViewModel.CustomerSelected += async (s, customer) =>
            {
                if (customer != null)
                {
                    searchView.Close();
                    // Mostrar detalles del cliente
                    await ShowCustomerDetailAsync(customer, customerService, creditService, layawayService);
                }
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

            // Manejar navegación a créditos/apartados por Tag (evita crash por async void)
            if (searchView.Tag is ValueTuple<string, Customer, bool> viewResult && viewResult.Item1 == "ViewCreditsLayaways")
            {
                await ShowCustomerCreditsLayawaysAsync(viewResult.Item2, viewResult.Item3);
            }

            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra la vista de detalles de un cliente.
        /// </summary>
        private async Task ShowCustomerDetailAsync(
            Customer customer,
            CustomerService customerService,
            CreditService? creditService,
            LayawayService? layawayService)
        {
            if (creditService == null || layawayService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return;
            }

            var detailView = new CustomerDetailView();
            var detailViewModel = new CustomerDetailViewModel(
                customerService,
                creditService,
                layawayService);

            await detailViewModel.InitializeAsync(customer.Id);
            detailView.DataContext = detailViewModel;

            detailViewModel.ViewCreditsRequested += async (s, e) =>
            {
                detailView.Close();
                // Abrir vista de créditos del cliente
                await ShowCustomerCreditsLayawaysAsync(customer, true);
            };

            detailViewModel.ViewLayawaysRequested += async (s, e) =>
            {
                detailView.Close();
                // Abrir vista de apartados del cliente
                await ShowCustomerCreditsLayawaysAsync(customer, false);
            };

            _hasOpenDialog = true;
            await detailView.ShowDialog(this);
            _hasOpenDialog = false;
        }

        /// <summary>
        /// Muestra la vista de créditos/apartados de un cliente específico.
        /// </summary>
        private async Task ShowCustomerCreditsLayawaysAsync(Customer customer, bool showCredits)
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

            var customerView = new CustomerCreditsLayawaysView();
            var customerViewModel = new CustomerCreditsLayawaysViewModel(
                creditService,
                layawayService,
                authService);

            customerViewModel.SetCustomerAndMode(customer, showCredits);
            
            // Cargar datos ANTES de asignar DataContext
            await customerViewModel.InitializeAsync();
            
            // Asignar DataContext - ahora es seguro con los setters privados en los modelos
            customerView.DataContext = customerViewModel;

            _hasOpenDialog = true;
            await customerView.ShowDialog(this);
            _hasOpenDialog = false;
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
                _viewModel.RequestLogout -= OnRequestLogout;
                _viewModel.SaleCompleted -= OnSaleCompleted;
                _viewModel.RequestClearCartConfirmation -= OnRequestClearCartConfirmation;
                _viewModel.RequestExitConfirmation -= OnRequestExitConfirmation;
                _viewModel.RequestLogoutConfirmation -= OnRequestLogoutConfirmation;
                _viewModel.CollectionIndicatorsChanged -= OnCollectionIndicatorsChanged;
                _viewModel.ProductAddedToCart -= OnProductAddedToCart;
                
                // Desuscribir eventos de descuentos
                _viewModel.ShowDiscountApplied -= OnShowDiscountApplied;
                _viewModel.ShowDiscountBlocked -= OnShowDiscountBlocked;
                _viewModel.RequestShowGeneralDiscount -= OnRequestShowGeneralDiscount;
                
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
                // Control de cantidad con flechas izquierda/derecha
                if ((e.Key == Key.Left || e.Key == Key.Right) && _viewModel.SelectedItemIndex >= 0)
                {
                    HandleQuantityArrowKey(e.Key);
                    e.Handled = true;
                    return;
                }

                // Atajos complejos con modificadores (Alt+F4, Shift+F5, Ctrl+F para descuentos)
                var complexShortcuts = new Dictionary<(Key, KeyModifiers), Action>
                {
                    { (Key.F4, KeyModifiers.Alt), () => _viewModel.ExitCommand.Execute(null) },
                    { (Key.F5, KeyModifiers.Shift), () => _viewModel.ClearCartCommand.Execute(null) },
                    // Descuentos con Ctrl+F
                    { (Key.F2, KeyModifiers.Control), () => _viewModel.ApplySpecialPriceCommand.Execute(null) },   // Ctrl+F2: Precio especial
                    { (Key.F3, KeyModifiers.Control), () => _viewModel.ApplyDealerPriceCommand.Execute(null) },    // Ctrl+F3: Precio vendedor
                    { (Key.F6, KeyModifiers.Control), () => _viewModel.ShowGeneralDiscountCommand.Execute(null) }  // Ctrl+F6: Descuento general
                };

                if (KeyboardShortcutHelper.HandleComplexShortcut(e, complexShortcuts))
                {
                    return;
                }

                // Atajos simples (ORIGINALES - sin cambiar)
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.F1, () => _viewModel.FocusBarcodeCommand.Execute(null) },
                    { Key.F2, () => _viewModel.ModifyQuantityCommand.Execute(null) },
                    { Key.F3, () => _viewModel.SearchProductCommand.Execute(null) },
                    { Key.F4, () => _viewModel.ChangeCollectionPreviousCommand.Execute(null) },
                    { Key.F5, () => _viewModel.ChangeCollectionCommand.Execute(null) },
                    { Key.F6, () => ShowCashMovementDialogAsync(true) },   // Gasto
                    { Key.F7, () => ShowCashMovementDialogAsync(false) },  // Ingreso
                    { Key.F8, () => ShowCustomersDialogAsync() },          // Clientes
                    { Key.F10, () => ShowCashCloseDialogAsync() },         // Corte de Caja
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
                    if (string.IsNullOrWhiteSpace(TxtBarcode.Text))
                    {
                        // Si el campo está vacío y presionan Enter, abre el catálogo de productos (F3)
                        _viewModel.SearchProductCommand.Execute(null);
                    }
                    else
                    {
                        // Si tiene texto, busca el producto por código
                        _ = _viewModel.SearchByCodeCommand.ExecuteAsync(null);
                    }
                    
                    e.Handled = true;
                    return;
                }
            }

            // Solo propagar si no se manejó
            base.OnKeyDown(e);
        }

        private DispatcherTimer? _quantityTimer;
        private Key _currentArrowKey;

        private void HandleQuantityArrowKey(Key key)
        {
            _currentArrowKey = key;
            
            // Ejecutar cambio inmediato
            ChangeQuantityByArrow(key);
            
            // Iniciar timer para cambios continuos si se mantiene presionada
            if (_quantityTimer == null)
            {
                _quantityTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(150) // Repetición cada 150ms
                };
                _quantityTimer.Tick += (s, e) => ChangeQuantityByArrow(_currentArrowKey);
            }
            
            if (!_quantityTimer.IsEnabled)
            {
                _quantityTimer.Start();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            // Detener el timer cuando se suelta la tecla
            if ((e.Key == Key.Left || e.Key == Key.Right) && _quantityTimer != null && _quantityTimer.IsEnabled)
            {
                _quantityTimer.Stop();
            }
            
            base.OnKeyUp(e);
        }

        private async void ChangeQuantityByArrow(Key key)
        {
            if (_viewModel == null || _viewModel.SelectedItemIndex < 0) return;
            
            // Fix 6: No cambiar cantidad si hay un diálogo abierto
            if (_hasOpenDialog) 
            {
                _quantityTimer?.Stop();
                return;
            }
            
            var item = _viewModel.Items[_viewModel.SelectedItemIndex];
            var newQuantity = item.Quantity;
            
            if (key == Key.Left)
            {
                newQuantity = Math.Max(1, newQuantity - 1); // No menor a 1
            }
            else if (key == Key.Right)
            {
                newQuantity += 1; // Sin límite superior
            }
            
            if (newQuantity != item.Quantity)
            {
                // CRÍTICO: Detener el timer ANTES de aplicar la cantidad
                // Si ApplyNewQuantityAsync muestra un diálogo, el KeyUp nunca se captura
                // y el timer seguiría corriendo infinitamente
                if (_quantityTimer != null && _quantityTimer.IsEnabled)
                {
                    _quantityTimer.Stop();
                }
                
                await _viewModel.ApplyNewQuantityAsync(newQuantity);
            }
        }

        private void OnProductAddedToCart(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Seleccionar el último item agregado
                if (_viewModel != null && _viewModel.Items.Count > 0)
                {
                    _viewModel.SelectedItemIndex = _viewModel.Items.Count - 1;
                    
                    // Scroll al item seleccionado en el DataGrid
                    var grid = this.FindControl<DataGrid>("GridProducts");
                    if (grid != null && _viewModel.SelectedItemIndex >= 0)
                    {
                        grid.ScrollIntoView(_viewModel.Items[_viewModel.SelectedItemIndex], null);
                    }
                }
            });
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

            // Si se creó un crédito o apartado, limpiar el carrito automáticamente
            if (menuView.Tag is string result && result == "ItemCreated")
            {
                _viewModel?.ConfirmClearCart();
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
            Tag = "module_selector";
            Close();
        }

        private void OnRequestLogout(object? sender, EventArgs e)
        {
            Tag = "logout";
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
        /// Muestra el diálogo de historial de cortes de caja.
        /// </summary>
        private async void ShowCashCloseHistoryDialogAsync()
        {
            var app = (App)Application.Current!;
            var cashCloseService = app.GetCashCloseService();
            var authService = app.GetAuthService();

            if (cashCloseService == null || authService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return;
            }

            var historyView = new CashCloseHistoryView();
            var userRepo   = new Data.Repositories.BaseRepository<Models.User>(App.DatabaseService!);
            var branchRepo = new Data.Repositories.BaseRepository<Models.Branch>(App.DatabaseService!);
            var historyViewModel = new CashCloseHistoryViewModel(
                cashCloseService,
                authService,
                userRepo,
                branchRepo,
                _viewModel?.BranchId ?? 1);

            historyView.DataContext = historyViewModel;
            
            // Cargar datos antes de mostrar
            await historyViewModel.InitializeAsync();

            _hasOpenDialog = true;
            await historyView.ShowDialog(this);
            _hasOpenDialog = false;

            // Si se seleccionó un item, mostrar el detalle
            if (historyView.Tag is ValueTuple<string, CashCloseListItemWrapper> result && result.Item1 == "ItemSelected")
            {
                await ShowCashCloseDetailAsync(result.Item2);
            }

            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra el detalle de un corte de caja específico.
        /// </summary>
        private async Task ShowCashCloseDetailAsync(CashCloseListItemWrapper item)
        {
            var app = (App)Application.Current!;
            var cashCloseService = app.GetCashCloseService();

            if (cashCloseService == null)
            {
                ShowMessageDialog("Error", "Servicio no disponible");
                return;
            }

            var detailView = new CashCloseDetailView();
            var detailViewModel = new CashCloseDetailViewModel(cashCloseService);

            // Inicializar con los datos del corte
            await detailViewModel.InitializeAsync(item.CashClose, item.UserName, item.BranchName);
            
            detailView.DataContext = detailViewModel;

            _hasOpenDialog = true;
            await detailView.ShowDialog(this);
            _hasOpenDialog = false;
        }

        /// <summary>
        /// Muestra el diálogo de corte de caja.
        /// Si hay productos en el carrito, pide confirmación para vaciarlo antes de proceder.
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

            // Verificar si hay productos en el carrito antes de proceder
            if (_viewModel != null && _viewModel.Items.Count > 0)
            {
                var clearCart = await DialogHelper.ShowConfirmDialog(
                    this,
                    "Cobranza Pendiente",
                    $"Hay {_viewModel.Items.Count} producto(s) en el carrito sin cobrar.\n\n¿Desea vaciar el carrito y proceder con el corte de caja?");

                if (!clearCart)
                {
                    TxtBarcode.Focus();
                    return;
                }

                // Vaciar el carrito antes de continuar
                _viewModel.ConfirmClearCart();
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
                // Mostrar ticket de corte si se generó — AWAIT antes de cerrar la ventana
                if (!string.IsNullOrEmpty(result.TicketText))
                {
                    await DialogHelper.ShowTicketDialog(this, result.CashClose.Folio, result.TicketText);
                }
                
                // Corte completado exitosamente
                // Volver al selector de módulos (manteniendo la sesión)
                Tag = "module_selector";
                Close();
            }
            else
            {
                // Si no se completó el corte, recargar folio (por si se abrió una nueva caja)
                _viewModel?.RefreshCashCloseFolio();
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
            if (_viewModel == null) return;

            bool confirmed;

            if (_viewModel.Items.Count > 0)
            {
                // Una sola pregunta: vaciar y salir
                confirmed = await DialogHelper.ShowConfirmDialog(
                    this,
                    "Cobranza Pendiente",
                    $"Hay {_viewModel.Items.Count} producto(s) en el carrito sin cobrar.\n\n¿Desea vaciar el carrito y salir del Punto de Venta?");

                if (confirmed)
                    _viewModel.ConfirmClearCart();
            }
            else
            {
                // Una sola pregunta: confirmar salida
                confirmed = await DialogHelper.ShowConfirmDialog(
                    this,
                    "Salir",
                    "¿Está seguro de salir del Punto de Venta?");
            }

            if (confirmed)
                _viewModel.ConfirmExit();

            TxtBarcode.Focus();
        }

        private async void OnRequestLogoutConfirmation(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;

            bool confirmed;

            if (_viewModel.Items.Count > 0)
            {
                // Una sola pregunta: vaciar y cerrar sesión
                confirmed = await DialogHelper.ShowConfirmDialog(
                    this,
                    "Cobranza Pendiente",
                    $"Hay {_viewModel.Items.Count} producto(s) en el carrito sin cobrar.\n\n¿Desea vaciar el carrito y cerrar la sesión?");

                if (confirmed)
                    _viewModel.ConfirmClearCart();
            }
            else
            {
                // Una sola pregunta: confirmar cierre de sesión
                confirmed = await DialogHelper.ShowConfirmDialog(
                    this,
                    "Cerrar Sesión",
                    "¿Está seguro de cerrar la sesión?");
            }

            if (confirmed)
                _viewModel.ConfirmLogout();

            TxtBarcode.Focus();
        }

        private void OnCashCloseClick(object? sender, RoutedEventArgs e)
        {
            ShowCashCloseDialogAsync();
        }

        private void OnCashCloseHistoryClick(object? sender, RoutedEventArgs e)
        {
            ShowCashCloseHistoryDialogAsync();
        }

        private void OnExpenseClick(object? sender, RoutedEventArgs e)
        {
            ShowCashMovementDialogAsync(true);
        }

        private void OnIncomeClick(object? sender, RoutedEventArgs e)
        {
            ShowCashMovementDialogAsync(false);
        }

        private void OnSalesHistoryClick(object? sender, RoutedEventArgs e)
        {
            ShowSalesHistoryDialogAsync();
        }

        private void OnPosConfigClick(object? sender, RoutedEventArgs e)
        {
            ShowPosConfigDialogAsync();
        }

        /// <summary>
        /// Muestra el diálogo de configuración del terminal POS.
        /// </summary>
        private async void ShowPosConfigDialogAsync()
        {
            var app = (App)Application.Current!;
            var configService = App.ConfigService;
            var authService = app.GetAuthService();
            var printService = App.PrintService;

            if (configService == null || authService == null || printService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return;
            }

            var configView = new PosTerminalConfigView();
            var configViewModel = new PosTerminalConfigViewModel(
                configService,
                authService,
                printService);

            configView.DataContext = configViewModel;
            
            // Inicializar antes de mostrar
            await configViewModel.InitializeAsync();

            _hasOpenDialog = true;
            await configView.ShowDialog(this);
            _hasOpenDialog = false;

            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra el diálogo de historial de ventas.
        /// </summary>
        private async void ShowSalesHistoryDialogAsync()
        {
            var app = (App)Application.Current!;
            var salesService = app.GetSaleService();

            if (salesService == null)
            {
                ShowMessageDialog("Error", "Servicio de ventas no disponible");
                return;
            }

            var historyView = new SalesHistoryView();
            var historyViewModel = new SalesHistoryViewModel(
                salesService,
                _viewModel?.BranchId ?? 1);

            historyView.DataContext = historyViewModel;
            
            // Cargar datos antes de mostrar
            await historyViewModel.InitializeAsync();

            _hasOpenDialog = true;
            await historyView.ShowDialog(this);
            _hasOpenDialog = false;

            TxtBarcode.Focus();
        }

        // ========== HANDLERS PARA DESCUENTOS ==========

        /// <summary>
        /// Muestra diálogo emergente cuando se aplica un descuento.
        /// Color neutral oscuro fijo - el color del row indica el tipo de descuento.
        /// Se cierra con Enter o Escape para flujo rápido.
        /// </summary>
        private async void OnShowDiscountApplied(object? sender, string message)
        {
            var dialog = new Window
            {
                Title = "Descuento Aplicado",
                Width = 450,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")), // Color neutral oscuro fijo
                Topmost = true
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(30),
                Spacing = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true // Para que reciba el KeyDown
            };

            // Icono de check verde (sin emoji)
            var iconBorder = new Border
            {
                Width = 60,
                Height = 60,
                CornerRadius = new CornerRadius(30),
                Background = new SolidColorBrush(Color.Parse("#2E7D32")), // Verde
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var iconText = new TextBlock
            {
                Text = "OK",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;

            var messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 16, // Texto más grande
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            var instructionText = new TextBlock
            {
                Text = "Presiona Enter para continuar",
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Cerrar con Enter o Escape
            dialog.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    dialog.Close();
                    e.Handled = true;
                }
            };

            // Forzar focus al panel cuando se abre el diálogo
            dialog.Opened += (s, e) =>
            {
                panel.Focus();
            };

            panel.Children.Add(iconBorder);
            panel.Children.Add(messageText);
            panel.Children.Add(instructionText);
            dialog.Content = panel;

            _hasOpenDialog = true;
            await dialog.ShowDialog(this);
            _hasOpenDialog = false;
            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra diálogo con la leyenda de colores para los diferentes tipos de precios
        /// </summary>
        private async void ShowColorLegendDialog()
        {
            var dialog = new Window
            {
                Title = "Leyenda de Colores - Tipos de Precio",
                Width = 450,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
                Topmost = true
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(25),
                Spacing = 20
            };

            // Título
            panel.Children.Add(new TextBlock
            {
                Text = "Colores de Filas en el Carrito",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Función helper para crear una fila con bolita de color
            void AddColorRow(string color, string title, string description)
            {
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Margin = new Thickness(0, 5)
                };

                // Circulo dibujado para evitar problemas con Emojis en Windows
                var colorCircle = new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new SolidColorBrush(Color.Parse(color)),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                var textPanel = new StackPanel { Spacing = 2 };
                textPanel.Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                });
                textPanel.Children.Add(new TextBlock
                {
                    Text = description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
                    TextWrapping = TextWrapping.Wrap
                });

                row.Children.Add(colorCircle);
                row.Children.Add(textPanel);
                panel.Children.Add(row);
            }

            // Agregar cada tipo de precio con su color
            AddColorRow("#00897B", "Mayoreo", "Precio por mayoreo (cantidad mínima alcanzada)");
            AddColorRow("#9C27B0", "Descuento de Categoría", "Descuento por categoría del producto");
            AddColorRow("#2196F3", "Precio Especial", "Precio especial de promoción (Ctrl+F2)");
            AddColorRow("#E65100", "Precio Vendedor", "Precio especial para vendedores (Ctrl+F3)");

            panel.Children.Add(new Separator
            {
                Background = new SolidColorBrush(Color.Parse("#444")),
                Margin = new Thickness(0, 10, 0, 10)
            });

            // Nota informativa
            panel.Children.Add(new TextBlock
            {
                Text = "💡 Los colores indican qué descuento se aplicó automáticamente a cada producto.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#999")),
                TextWrapping = TextWrapping.Wrap
            });

            // Botón Cerrar
            var closeButton = new Button
            {
                Content = "Cerrar",
                Width = 100,
                Height = 35,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.Parse("#444")),
                Foreground = Brushes.White
            };
            closeButton.Click += (s, e) => dialog.Close();
            panel.Children.Add(closeButton);

            dialog.Content = panel;

            // Cerrar con Enter o Escape
            dialog.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    dialog.Close();
                    e.Handled = true;
                }
            };

            _hasOpenDialog = true;
            await dialog.ShowDialog(this);
            _hasOpenDialog = false;
            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra diálogo emergente cuando no se puede aplicar un descuento.
        /// Color neutral oscuro fijo con icono de advertencia.
        /// Se cierra con Enter o Escape para flujo rápido.
        /// </summary>
        private async void OnShowDiscountBlocked(object? sender, string message)
        {
            var dialog = new Window
            {
                Title = "No se puede aplicar",
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")), // Color neutral oscuro fijo
                Topmost = true
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(30),
                Spacing = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true // Para que reciba el KeyDown
            };

            // Icono de advertencia rojo (sin emoji)
            var iconBorder = new Border
            {
                Width = 60,
                Height = 60,
                CornerRadius = new CornerRadius(30),
                Background = new SolidColorBrush(Color.Parse("#D32F2F")), // Rojo
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var iconText = new TextBlock
            {
                Text = "X",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;

            var messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 15, // Texto más grande
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            var instructionText = new TextBlock
            {
                Text = "Presiona Enter para continuar",
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Cerrar con Enter o Escape
            dialog.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    dialog.Close();
                    e.Handled = true;
                }
            };

            // Forzar focus al panel cuando se abre el diálogo
            dialog.Opened += (s, e) =>
            {
                panel.Focus();
            };

            panel.Children.Add(iconBorder);
            panel.Children.Add(messageText);
            panel.Children.Add(instructionText);
            dialog.Content = panel;

            _hasOpenDialog = true;
            await dialog.ShowDialog(this);
            _hasOpenDialog = false;
            TxtBarcode.Focus();
        }

        /// <summary>
        /// Muestra el diálogo para aplicar descuento general (Ctrl+F6)
        /// </summary>
        private async void OnRequestShowGeneralDiscount(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;

            bool isPercentMode = true;
            decimal selectedValue = 0;

            var dialog = new Window
            {
                Title = "Descuento General",
                Width = 380,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
                Topmost = true
            };

            var root = new StackPanel
            {
                Margin = new Thickness(20, 15, 20, 15),
                Spacing = 8,
                Focusable = true
            };

            // --- Header ---
            root.Children.Add(new TextBlock
            {
                Text = "$ Descuento General",
                Foreground = new SolidColorBrush(Color.Parse("#FFC107")),
                FontSize = 16, FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            root.Children.Add(new TextBlock
            {
                Text = $"Subtotal: {_viewModel.Total:C2}",
                Foreground = Brushes.White,
                FontSize = 16, FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // --- Separador ---
            root.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#444")), Margin = new Thickness(0, 3) });

            // --- Tipo ---
            root.Children.Add(new TextBlock { Text = "TIPO", Foreground = new SolidColorBrush(Color.Parse("#999")), FontSize = 10, FontWeight = FontWeight.Bold });
            var rbPercent = new RadioButton { Content = "Porcentaje (%)", IsChecked = true, Foreground = Brushes.White, FontSize = 13, GroupName = "discType" };
            var rbFixed = new RadioButton { Content = "Cantidad fija ($)", Foreground = Brushes.White, FontSize = 13, GroupName = "discType" };
            root.Children.Add(rbPercent);
            root.Children.Add(rbFixed);

            // --- Separador ---
            root.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#444")), Margin = new Thickness(0, 3) });

            // --- Valor ---
            root.Children.Add(new TextBlock { Text = "VALOR", Foreground = new SolidColorBrush(Color.Parse("#999")), FontSize = 10, FontWeight = FontWeight.Bold });

            var quickPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
            var btnQ10 = new Button { Content = "10%", Width = 95, Height = 40, Tag = 10m, Background = new SolidColorBrush(Color.Parse("#4A4A4A")), Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeight.Bold };
            var btnQ20 = new Button { Content = "20%", Width = 95, Height = 40, Tag = 20m, Background = new SolidColorBrush(Color.Parse("#4A4A4A")), Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeight.Bold };
            var btnQ30 = new Button { Content = "30%", Width = 95, Height = 40, Tag = 30m, Background = new SolidColorBrush(Color.Parse("#4A4A4A")), Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeight.Bold };
            quickPanel.Children.Add(btnQ10);
            quickPanel.Children.Add(btnQ20);
            quickPanel.Children.Add(btnQ30);
            root.Children.Add(quickPanel);

            var valueInput = new TextBox
            {
                Text = "0",
                Width = 200,
                Height = 35,
                IsVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            valueInput.GotFocus += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => valueInput.SelectAll());
            root.Children.Add(valueInput);

            // --- Separador ---
            root.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#444")), Margin = new Thickness(0, 3) });

            // --- Preview ---
            var previewLabel = new TextBlock
            {
                Text = $"Total: {_viewModel.Total:C2}",
                Foreground = new SolidColorBrush(Color.Parse("#66BB6A")),
                FontSize = 20, FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3)
            };
            root.Children.Add(previewLabel);

            // --- Atajos ---
            root.Children.Add(new TextBlock
            {
                Text = "Enter = Aplicar  •  Esc = Cancelar",
                Foreground = new SolidColorBrush(Color.Parse("#777")),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2)
            });

            // --- Botones ---
            var btnsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) };
            var btnCancel = new Button { Content = "Cancelar", Width = 100, Height = 38, FontSize = 13, Background = new SolidColorBrush(Color.Parse("#555")), Foreground = Brushes.White };

            // Botón Limpiar (naranja) con ícono ↩✕
            var clearContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            clearContent.Children.Add(new TextBlock { Text = "⌫", FontSize = 16, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            clearContent.Children.Add(new TextBlock { Text = "Limpiar", FontSize = 13, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            var btnClear = new Button { Content = clearContent, Width = 110, Height = 38, Background = new SolidColorBrush(Color.Parse("#E65100")), Foreground = Brushes.White };

            var btnApply = new Button { Content = "Aplicar", Width = 100, Height = 38, FontSize = 13, FontWeight = FontWeight.Bold, Background = new SolidColorBrush(Color.Parse("#388E3C")), Foreground = Brushes.White };
            btnsPanel.Children.Add(btnCancel);
            btnsPanel.Children.Add(btnClear);
            btnsPanel.Children.Add(btnApply);
            root.Children.Add(btnsPanel);

            dialog.Content = root;

            // ===== Lógica =====
            decimal ParseInput()
            {
                if (decimal.TryParse(valueInput.Text, out var prs)) return Math.Max(0, prs);
                return 0;
            }

            void UpdatePreview()
            {
                var v = isPercentMode ? selectedValue : ParseInput();
                var disc = isPercentMode ? _viewModel.Total * (v / 100m) : Math.Min(v, _viewModel.Total);
                previewLabel.Text = $"Total: {(_viewModel.Total - disc):C2}";
            }

            void DoApply()
            {
                var v = isPercentMode ? selectedValue : ParseInput();
                if (v > 0)
                {
                    _viewModel.ApplyGeneralDiscountValue(v, isPercentMode);
                    dialog.Close();
                }
            }

            void SelectQuick(Button btn, decimal pct)
            {
                selectedValue = pct;
                btnQ10.Background = new SolidColorBrush(Color.Parse("#4A4A4A"));
                btnQ20.Background = new SolidColorBrush(Color.Parse("#4A4A4A"));
                btnQ30.Background = new SolidColorBrush(Color.Parse("#4A4A4A"));
                btn.Background = new SolidColorBrush(Color.Parse("#0078D4"));
                UpdatePreview();
            }

            btnQ10.Click += (_, _) => SelectQuick(btnQ10, 10);
            btnQ20.Click += (_, _) => SelectQuick(btnQ20, 20);
            btnQ30.Click += (_, _) => SelectQuick(btnQ30, 30);

            valueInput.AddHandler(TextInputEvent, (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Text))
                {
                    foreach (char c in e.Text)
                    {
                        if (char.IsDigit(c) || (c == '.' && valueInput.Text?.Contains('.') == false)) continue;
                        e.Handled = true;
                        return;
                    }
                }
            }, RoutingStrategies.Tunnel);

            valueInput.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "Text") UpdatePreview();
            };

            rbPercent.IsCheckedChanged += (_, _) =>
            {
                if (rbPercent.IsChecked == true)
                {
                    isPercentMode = true;
                    quickPanel.IsVisible = true;
                    valueInput.IsVisible = false;
                    selectedValue = 0;
                    btnQ10.Background = new SolidColorBrush(Color.Parse("#4A4A4A"));
                    btnQ20.Background = new SolidColorBrush(Color.Parse("#4A4A4A"));
                    btnQ30.Background = new SolidColorBrush(Color.Parse("#4A4A4A"));
                    UpdatePreview();
                }
            };

            rbFixed.IsCheckedChanged += (_, _) =>
            {
                if (rbFixed.IsChecked == true)
                {
                    isPercentMode = false;
                    quickPanel.IsVisible = false;
                    valueInput.IsVisible = true;
                    UpdatePreview();
                    valueInput.Focus();
                }
            };

            btnApply.Click += (_, _) => DoApply();
            btnClear.Click += (_, _) =>
            {
                _viewModel.ClearGeneralDiscount();
                dialog.Close();
            };
            btnCancel.Click += (_, _) => dialog.Close();

            // KeyDown en el DIALOG directamente - captura teclas aunque el focus esté en un hijo
            dialog.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    DoApply();
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    dialog.Close();
                    args.Handled = true;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel);

            dialog.Opened += (_, _) => root.Focus();

            _hasOpenDialog = true;
            await dialog.ShowDialog(this);
            _hasOpenDialog = false;
            TxtBarcode?.Focus();
        }

        // ========== COLOREADO DINÁMICO DE FILAS DEL DATAGRID ==========

        /// <summary>
        /// Handler del DataGrid: suscribe cambios de propiedades del CartItem.
        /// El color de fondo de descuento ahora se aplica solo en la celda
        /// CÓDIGO mediante binding XAML (RowBackgroundColor), no en toda la fila.
        /// </summary>
        private void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is CartItem item)
            {
                // Suscribir a cambios del item (por si se necesita en el futuro)
                item.PropertyChanged -= OnCartItemPropertyChanged;
                item.PropertyChanged += OnCartItemPropertyChanged;
            }
        }

        /// <summary>
        /// Handler para cuando cambian propiedades del CartItem.
        /// El color de la celda CÓDIGO se actualiza automáticamente con el binding XAML.
        /// </summary>
        private void OnCartItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // El color de la celda CÓDIGO se actualiza via binding RowBackgroundColor en XAML.
            // No es necesario actualizar el Row.Background manualmente.
        }
        private async void OnCashiersClick(object? sender, RoutedEventArgs e)
        {
            var userService = App.Current is App app ? app.GetUserService() : null;
            var authService = App.Current is App app2 ? app2.GetAuthService() : null;

            if (userService == null || authService == null)
            {
                await DialogHelper.ShowMessageDialog(this, "Error", "Servicios no inicializados.");
                return;
            }

            // Requiere verificación de administrador para ver el módulo de Cajeros
            var verified = await AdminVerificationHelper.VerifyAdminAsync(this, userService);
            if (!verified)
            {
                return; // Acción cancelada si no verifica admin
            }

            var vm = new UserManagementViewModel(userService, authService, isAdminMode: false);
            var view = new UserManagementView { DataContext = vm };
            await view.ShowDialog(this);
        }

        /// <summary>
        /// Solicita verificación de administrador antes de mostrar diálogo de descuento general.
        /// </summary>
        private async void OnRequestAdminVerification(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;

            var userService = App.Current is App app ? app.GetUserService() : null;
            
            if (userService == null)
            {
                await DialogHelper.ShowMessageDialog(this, "Error", "Servicio de usuarios no inicializado.");
                return;
            }

            // Mostrar diálogo de verificación
            var verified = await AdminVerificationHelper.VerifyAdminAsync(this, userService);

            if (verified)
            {
                // Si se verificó correctamente, mostrar el diálogo de descuento
                OnRequestShowGeneralDiscount(this, EventArgs.Empty);
            }
        }
    }
}

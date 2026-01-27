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
using CasaCejaRemake.Services;
using CasaCejaRemake.Models;
using static CasaCejaRemake.ViewModels.POS.CreditsLayawaysListViewModel;

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
            var menuView = new CreditsLayawaysMenuView();
            var menuViewModel = new CreditsLayawaysMenuViewModel();
            menuView.DataContext = menuViewModel;

            menuViewModel.OptionSelected += async (s, option) =>
            {
                menuView.Close();
                await HandleCreditsLayawaysOption(option);
            };

            menuViewModel.Cancelled += (s, args) =>
            {
                menuView.Close();
            };

            await menuView.ShowDialog(this);
            TxtBarcode.Focus();
        }

        private async Task HandleCreditsLayawaysOption(CreditsLayawaysOption option)
        {
            switch (option)
            {
                case CreditsLayawaysOption.List:
                    await ShowCreditsLayawaysList();
                    break;
                case CreditsLayawaysOption.NewOrPayment:
                    await ShowCustomerSearch(forNewOrPayment: true);
                    break;
                case CreditsLayawaysOption.CustomerList:
                    await ShowCustomerSearch(forNewOrPayment: false);
                    break;
            }
        }

        private async Task ShowCreditsLayawaysList()
        {
            var app = (App)Application.Current!;
            var creditService = app.GetCreditService();
            var layawayService = app.GetLayawayService();
            var customerService = app.GetCustomerService();

            if (creditService == null || layawayService == null || customerService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return;
            }

            var listView = new CreditsLayawaysListView();
            var listViewModel = new CreditsLayawaysListViewModel(
                creditService,
                layawayService,
                customerService,
                _viewModel?.BranchId ?? 1);

            await listViewModel.InitializeAsync();
            listView.DataContext = listViewModel;

            await listView.ShowDialog(this);

            // Manejar item seleccionado - abrir vista de detalle
            if (listView.Tag is ValueTuple<string, CreditLayawayListItemWrapper> result && result.Item1 == "ItemSelected")
            {
                var item = result.Item2;
                
                if (item.IsCredit)
                {
                    var credit = (Credit)item.Item;
                    await ShowCreditDetail(credit);
                }
                else
                {
                    var layaway = (Layaway)item.Item;
                    await ShowLayawayDetail(layaway);
                }
            }
        }

        private async Task ShowCustomerSearch(bool forNewOrPayment)
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
            await searchViewModel.InitializeAsync();
            searchView.DataContext = searchViewModel;

            await searchView.ShowDialog(this);

            // Manejar resultado de la búsqueda
            if (searchView.Tag is ValueTuple<string, Customer> result && result.Item1 == "CustomerSelected")
            {
                var customer = result.Item2;
                
                if (forNewOrPayment)
                {
                    await ShowCustomerActions(customer);
                }
                else
                {
                    await ShowCustomerCreditsLayaways(customer);
                }
            }
            else if (searchView.Tag is string tag && tag == "CreateNew")
            {
                await ShowQuickCustomer();
            }
        }

        private async Task ShowQuickCustomer()
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

            await quickView.ShowDialog(this);

            // Si se creó un cliente, regresar a búsqueda de clientes
            if (quickView.Tag is Customer newCustomer)
            {
                // Mostrar mensaje de éxito
                ShowMessageDialog("Éxito", $"Cliente {newCustomer.Name} creado correctamente");
            }
        }

        private async Task ShowCustomerActions(Customer customer)
        {
            bool shouldContinue = true;
            
            while (shouldContinue)
            {
                var actionView = new CustomerActionView();
                var actionViewModel = new CustomerActionViewModel();
                var hasItems = _viewModel?.Items != null && _viewModel.Items.Count > 0;
                
                actionViewModel.SetCustomer(customer, hasCartItems: hasItems);
                actionView.DataContext = actionViewModel;

                await actionView.ShowDialog(this);

                // Si el usuario canceló explícitamente, salir
                if (actionView.Tag == null || (actionView.Tag is string tag && tag == "Cancelled"))
                {
                    shouldContinue = false;
                    break;
                }

                // Manejar la acción seleccionada
                if (actionView.Tag is ValueTuple<string, CustomerActionOption> result && result.Item1 == "ActionSelected")
                {
                    var actionCompleted = await HandleCustomerAction(customer, result.Item2);
                    
                    // Si se completó exitosamente una creación de crédito/apartado, mostrar mensaje y salir
                    if (actionCompleted && (result.Item2 == CustomerActionOption.NewCredit || result.Item2 == CustomerActionOption.NewLayaway))
                    {
                        string type = result.Item2 == CustomerActionOption.NewCredit ? "Crédito" : "Apartado";
                        ShowMessageDialog("Éxito", $"{type} creado correctamente");
                        shouldContinue = false;
                    }
                    else
                    {
                        // Si la acción no se completó (usuario canceló) o fue otra acción, volver al menú
                        shouldContinue = true;
                    }
                }
                else
                {
                    shouldContinue = false;
                }
            }
        }

        private async Task<bool> HandleCustomerAction(Customer customer, CustomerActionOption action)
        {
            switch (action)
            {
                case CustomerActionOption.NewCredit:
                    return await ShowCreateCredit(customer);
                case CustomerActionOption.NewLayaway:
                    return await ShowCreateLayaway(customer);
                case CustomerActionOption.MyCredits:
                    return await ShowCustomerCreditsLayaways(customer, isCreditsMode: true);
                case CustomerActionOption.MyLayaways:
                    return await ShowCustomerCreditsLayaways(customer, isCreditsMode: false);
                default:
                    return false;
            }
        }

        private async Task<bool> ShowCreateCredit(Customer customer)
        {
            var app = (App)Application.Current!;
            var creditService = app.GetCreditService();
            var authService = app.GetAuthService();

            if (creditService == null || authService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return false;
            }

            var createView = new CreateCreditView();
            var createViewModel = new CreateCreditViewModel(
                creditService,
                authService,
                _viewModel?.BranchId ?? 1);

            createViewModel.Initialize(customer, _viewModel.Items.ToList());
            createView.DataContext = createViewModel;

            await createView.ShowDialog(this);

            // Si se creó el crédito, limpiar carrito y retornar true
            if (createView.Tag is Credit newCredit)
            {
                _viewModel?.ClearCartCommand.Execute(null);
                // El mensaje se mostrará después de cerrar todas las vistas
                return true;
            }
            
            // Si canceló, retornar false pero sin error
            return false;
        }

        private async Task<bool> ShowCreateLayaway(Customer customer)
        {
            var app = (App)Application.Current!;
            var layawayService = app.GetLayawayService();
            var authService = app.GetAuthService();

            if (layawayService == null || authService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return false;
            }

            var createView = new CreateLayawayView();
            var createViewModel = new CreateLayawayViewModel(
                layawayService,
                authService,
                _viewModel?.BranchId ?? 1);

            createViewModel.Initialize(customer, _viewModel.Items.ToList());
            createView.DataContext = createViewModel;

            await createView.ShowDialog(this);

            // Si se creó el apartado, limpiar carrito y retornar true
            if (createView.Tag is Layaway newLayaway)
            {
                _viewModel?.ClearCartCommand.Execute(null);
                // El mensaje se mostrará después de cerrar todas las vistas
                return true;
            }
            
            // Si canceló, retornar false pero sin error
            return false;
        }

        private async Task ShowCustomerCreditsLayaways(Customer customer)
        {
            await ShowCustomerCreditsLayaways(customer, isCreditsMode: true);
        }

        private async Task<bool> ShowCustomerCreditsLayaways(Customer customer, bool isCreditsMode)
        {
            var app = (App)Application.Current!;
            var creditService = app.GetCreditService();
            var layawayService = app.GetLayawayService();
            var authService = app.GetAuthService();

            if (creditService == null || layawayService == null || authService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return false;
            }

            bool shouldContinue = true;
            bool paymentWasMade = false;
            
            while (shouldContinue)
            {
                var customerView = new CustomerCreditsLayawaysView();
                var customerViewModel = new CustomerCreditsLayawaysViewModel(
                    creditService,
                    layawayService,
                    authService);

                customerViewModel.SetCustomerAndMode(customer, isCreditsMode);
                await customerViewModel.InitializeAsync();
                customerView.DataContext = customerViewModel;

                await customerView.ShowDialog(this);

                // Si el usuario cerró sin acción, salir
                if (customerView.Tag == null)
                {
                    shouldContinue = false;
                    break;
                }

                // Manejar acciones de la vista de créditos/apartados
                if (customerView.Tag is ValueTuple<string, object> result)
                {
                    bool actionHandled = false;
                    
                    switch (result.Item1)
                    {
                        case "AddPaymentCredit":
                            if (result.Item2 is Credit credit)
                            {
                                // Mostrar detalle del crédito en lugar de ir directo al pago
                                await ShowCreditDetail(credit);
                                actionHandled = true; // Siempre refrescar después de ver detalle
                            }
                            break;
                        case "AddPaymentLayaway":
                            if (result.Item2 is Layaway layaway)
                            {
                                // Mostrar detalle del apartado en lugar de ir directo al pago
                                await ShowLayawayDetail(layaway);
                                actionHandled = true; // Siempre refrescar después de ver detalle
                            }
                            break;
                        case "DeliverLayaway":
                            if (result.Item2 is Layaway deliverLayaway)
                            {
                                actionHandled = await ShowDeliverLayaway(deliverLayaway);
                            }
                            break;
                    }
                    
                    // Después de cualquier acción, volver a mostrar la vista actualizada
                    shouldContinue = actionHandled;
                }
                else
                {
                    shouldContinue = false;
                }
            }
            
            return true;
        }

        private async Task ShowCreditDetail(Credit credit)
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

            bool shouldContinue = true;
            bool paymentWasMade = false;
            
            while (shouldContinue)
            {
                var detailView = new CreditLayawayDetailView();
                var detailViewModel = new CreditLayawayDetailViewModel(
                    creditService,
                    layawayService,
                    customerService,
                    authService);

                await detailViewModel.InitializeForCreditAsync(credit.Id);
                detailView.DataContext = detailViewModel;

                await detailView.ShowDialog(this);

                // Si el usuario cerró sin acción, salir
                if (detailView.Tag == null)
                {
                    shouldContinue = false;
                    break;
                }

                // Manejar acciones
                if (detailView.Tag is ValueTuple<string, CreditLayawayDetailViewModel> result)
                {
                    bool actionHandled = false;
                    
                    switch (result.Item1)
                    {
                        case "AddPayment":
                            var creditFromVm = result.Item2.GetCredit();
                            if (creditFromVm != null)
                            {
                                actionHandled = await ShowAddPaymentToCredit(creditFromVm);
                                // Si el pago fue exitoso, salir del loop para cerrar la vista de detalle
                                if (actionHandled)
                                {
                                    paymentWasMade = true;
                                    shouldContinue = false;
                                }
                            }
                            break;
                        case "Print":
                            // TODO: Implement print
                            shouldContinue = false;
                            break;
                        default:
                            shouldContinue = false;
                            break;
                    }
                    
                    // Si se realizó una acción, continuar el loop para refrescar
                    if (!actionHandled)
                    {
                        shouldContinue = false;
                    }
                }
                else
                {
                    shouldContinue = false;
                }
            }
            
            // Mostrar mensaje de éxito DESPUÉS de cerrar todas las vistas
            if (paymentWasMade)
            {
                ShowMessageDialog("Éxito", "Pago registrado correctamente");
            }
        }

        private async Task ShowLayawayDetail(Layaway layaway)
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

            bool shouldContinue = true;
            bool paymentWasMade = false;
            bool layawayDelivered = false;
            
            while (shouldContinue)
            {
                var detailView = new CreditLayawayDetailView();
                var detailViewModel = new CreditLayawayDetailViewModel(
                    creditService,
                    layawayService,
                    customerService,
                    authService);

                await detailViewModel.InitializeForLayawayAsync(layaway.Id);
                detailView.DataContext = detailViewModel;

                await detailView.ShowDialog(this);

                // Si el usuario cerró sin acción, salir
                if (detailView.Tag == null)
                {
                    shouldContinue = false;
                    break;
                }

                // Manejar acciones
                if (detailView.Tag is ValueTuple<string, CreditLayawayDetailViewModel> result)
                {
                    bool actionHandled = false;
                    
                    switch (result.Item1)
                    {
                        case "AddPayment":
                            var layawayFromVm = result.Item2.GetLayaway();
                            if (layawayFromVm != null)
                            {
                                actionHandled = await ShowAddPaymentToLayaway(layawayFromVm);
                                // Si el pago fue exitoso, salir del loop para cerrar la vista de detalle
                                if (actionHandled)
                                {
                                    paymentWasMade = true;
                                    shouldContinue = false;
                                }
                            }
                            break;
                        case "Deliver":
                            var layawayToDeliver = result.Item2.GetLayaway();
                            if (layawayToDeliver != null)
                            {
                                actionHandled = await ShowDeliverLayaway(layawayToDeliver);
                                if (actionHandled)
                                {
                                    layawayDelivered = true;
                                    shouldContinue = false;
                                }
                            }
                            break;
                        case "Print":
                            // TODO: Implement print
                            shouldContinue = false;
                            break;
                        default:
                            shouldContinue = false;
                            break;
                    }
                    
                    // Si se realizó una acción, continuar el loop para refrescar
                    if (!actionHandled)
                    {
                        shouldContinue = false;
                    }
                }
                else
                {
                    shouldContinue = false;
                }
            }
            
            // Mostrar mensajes de éxito DESPUÉS de cerrar todas las vistas
            if (paymentWasMade)
            {
                ShowMessageDialog("Éxito", "Pago registrado correctamente");
            }
            else if (layawayDelivered)
            {
                ShowMessageDialog("Éxito", "Apartado entregado correctamente");
            }
        }

        private async Task<bool> ShowAddPaymentToCredit(Credit credit)
        {
            var app = (App)Application.Current!;
            var creditService = app.GetCreditService();
            var authService = app.GetAuthService();
            var customerService = app.GetCustomerService();

            if (creditService == null || authService == null || customerService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return false;
            }

            // Obtener el customer
            var customer = await customerService.GetByIdAsync(credit.CustomerId);
            if (customer == null)
            {
                ShowMessageDialog("Error", "No se encontró el cliente");
                return false;
            }

            var addPaymentView = new AddPaymentView();
            var addPaymentViewModel = new AddPaymentViewModel(
                creditService,
                null!,
                authService);

            await addPaymentViewModel.InitializeForCreditAsync(credit.Id, customer);
            addPaymentView.DataContext = addPaymentViewModel;

            await addPaymentView.ShowDialog(this);

            // Si se agregó el pago, retornar true (mensaje se mostrará después)
            if (addPaymentView.Tag is PaymentResult result && result.Success)
            {
                return true;
            }
            
            return false;
        }

        private async Task<bool> ShowAddPaymentToLayaway(Layaway layaway)
        {
            var app = (App)Application.Current!;
            var layawayService = app.GetLayawayService();
            var authService = app.GetAuthService();
            var customerService = app.GetCustomerService();

            if (layawayService == null || authService == null || customerService == null)
            {
                ShowMessageDialog("Error", "Servicios no disponibles");
                return false;
            }

            // Obtener el customer
            var customer = await customerService.GetByIdAsync(layaway.CustomerId);
            if (customer == null)
            {
                ShowMessageDialog("Error", "No se encontró el cliente");
                return false;
            }

            var addPaymentView = new AddPaymentView();
            var addPaymentViewModel = new AddPaymentViewModel(
                null!,
                layawayService,
                authService);

            await addPaymentViewModel.InitializeForLayawayAsync(layaway.Id, customer);
            addPaymentView.DataContext = addPaymentViewModel;

            await addPaymentView.ShowDialog(this);

            // Si se agregó el pago, retornar true (mensaje se mostrará después)
            if (addPaymentView.Tag is PaymentResult result && result.Success)
            {
                return true;
            }
            
            return false;
        }

        private async Task<bool> ShowDeliverLayaway(Layaway layaway)
        {
            var app = (App)Application.Current!;
            var layawayService = app.GetLayawayService();

            if (layawayService == null)
            {
                ShowMessageDialog("Error", "Servicio de apartados no disponible");
                return false;
            }

            // Verificar que el apartado esté completamente pagado
            if (layaway.RemainingBalance > 0)
            {
                ShowMessageDialog("Advertencia", 
                    $"El apartado debe estar completamente pagado para poder entregarse.\nFaltan ${layaway.RemainingBalance:N2}");
                return false;
            }

            // Confirmar entrega
            var confirmDialog = new Window
            {
                Title = "Confirmar Entrega",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.DimGray
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15
            };

            var textBlock = new TextBlock
            {
                Text = $"¿Confirmar entrega del apartado #{layaway.Folio}?",
                Foreground = Brushes.White,
                FontSize = 16,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10
            };

            var btnYes = new Button
            {
                Content = "Sí, Entregar",
                Width = 120
            };

            var btnNo = new Button
            {
                Content = "Cancelar",
                Width = 120
            };

            btnYes.Click += async (s, e) =>
            {
                try
                {
                    var app = (App)Application.Current!;
                    var authService = app.GetAuthService();
                    var userId = authService?.CurrentUser?.Id ?? 1;
                    
                    await layawayService.MarkAsDeliveredAsync(layaway.Id, userId);
                    confirmDialog.Tag = "delivered";
                    confirmDialog.Close();
                }
                catch (Exception ex)
                {
                    ShowMessageDialog("Error", $"Error al entregar apartado: {ex.Message}");
                }
            };

            btnNo.Click += (s, e) => confirmDialog.Close();

            buttonsPanel.Children.Add(btnYes);
            buttonsPanel.Children.Add(btnNo);

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(buttonsPanel);

            confirmDialog.Content = stackPanel;

            await confirmDialog.ShowDialog(this);

            if (confirmDialog.Tag is string tag && tag == "delivered")
            {
                // Retornar true sin mostrar mensaje aquí
                return true;
            }
            
            return false;
        }

        private void ShowMessageDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Avalonia.Media.Brushes.DimGray
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(15),
                Spacing = 10
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

            _ = dialog.ShowDialog(this);
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

        private async void OnSaleCompleted(object? sender, SaleResult result)
        {
            if (result.Success && result.TicketText != null)
            {
                var dialog = new Window
                {
                    Title = $"Ticket - {result.Sale?.Folio}",
                    Width = 400,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = Avalonia.Media.Brushes.White
                };

                var scrollViewer = new ScrollViewer
                {
                    Margin = new Thickness(10)
                };

                var textBlock = new TextBlock
                {
                    Text = result.TicketText,
                    FontFamily = new Avalonia.Media.FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.Black
                };

                scrollViewer.Content = textBlock;
                dialog.Content = scrollViewer;

                await dialog.ShowDialog(this);
            }

            TxtBarcode.Focus();
        }
    }
}
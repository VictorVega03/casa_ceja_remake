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
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;
using static CasaCejaRemake.ViewModels.POS.CreditsLayawaysListViewModel;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreditsLayawaysMenuView : Window
    {
        private readonly CreditService _creditService;
        private readonly LayawayService _layawayService;
        private readonly CustomerService _customerService;
        private readonly AuthService _authService;
        private readonly int _branchId;
        private readonly List<CartItem>? _cartItems;

        public CreditsLayawaysMenuView(
            CreditService creditService,
            LayawayService layawayService,
            CustomerService customerService,
            AuthService authService,
            int branchId,
            List<CartItem>? cartItems = null)
        {
            InitializeComponent();
            
            _creditService = creditService;
            _layawayService = layawayService;
            _customerService = customerService;
            _authService = authService;
            _branchId = branchId;
            _cartItems = cartItems;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CreditsLayawaysMenuViewModel vm)
            {
                vm.OptionSelected += OnOptionSelected;
                vm.Cancelled += OnCancelled;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is CreditsLayawaysMenuViewModel vm)
            {
                vm.HandleKeyPress(e.Key.ToString());
            }
        }

        private async void OnOptionSelected(object? sender, CreditsLayawaysOption option)
        {
            await HandleCreditsLayawaysOption(option);
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
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
            var listView = new CreditsLayawaysListView();
            var listViewModel = new CreditsLayawaysListViewModel(
                _creditService,
                _layawayService,
                _customerService,
                _branchId);

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
            var searchView = new CustomerSearchView();
            var searchViewModel = new CustomerSearchViewModel(_customerService);
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
            var quickView = new QuickCustomerView();
            var quickViewModel = new QuickCustomerViewModel(_customerService);
            quickView.DataContext = quickViewModel;

            await quickView.ShowDialog(this);

            // Si se creó un cliente, mostrar mensaje de éxito
            if (quickView.Tag is Customer newCustomer)
            {
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
                var hasItems = _cartItems != null && _cartItems.Count > 0;
                
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
                    
                    // Si se completó exitosamente una creación de crédito/apartado
                    if (actionCompleted && (result.Item2 == CustomerActionOption.NewCredit || result.Item2 == CustomerActionOption.NewLayaway))
                    {
                        string type = result.Item2 == CustomerActionOption.NewCredit ? "Crédito" : "Apartado";
                        
                        // Marcar que se creó algo para que SalesView limpie el carrito
                        Tag = "ItemCreated";
                        
                        // Mostrar mensaje y ESPERAR a que el usuario lo cierre
                        await ShowMessageDialogAsync("Éxito", $"{type} creado correctamente");
                        
                        Close();
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
            if (_cartItems == null || !_cartItems.Any())
            {
                ShowMessageDialog("Error", "Debe agregar productos al carrito para crear un crédito");
                return false;
            }

            var createView = new CreateCreditView();
            var createViewModel = new CreateCreditViewModel(
                _creditService,
                _authService,
                _branchId);

            createViewModel.Initialize(customer, _cartItems.ToList());
            createView.DataContext = createViewModel;

            await createView.ShowDialog(this);

            // Si se creó el crédito, retornar true
            if (createView.Tag is Credit newCredit)
            {
                return true;
            }
            
            return false;
        }

        private async Task<bool> ShowCreateLayaway(Customer customer)
        {
            if (_cartItems == null || !_cartItems.Any())
            {
                ShowMessageDialog("Error", "Debe agregar productos al carrito para crear un apartado");
                return false;
            }

            var createView = new CreateLayawayView();
            var createViewModel = new CreateLayawayViewModel(
                _layawayService,
                _authService,
                _branchId);

            createViewModel.Initialize(customer, _cartItems.ToList());
            createView.DataContext = createViewModel;

            await createView.ShowDialog(this);

            // Si se creó el apartado, retornar true
            if (createView.Tag is Layaway newLayaway)
            {
                return true;
            }
            
            return false;
        }

        private async Task ShowCustomerCreditsLayaways(Customer customer)
        {
            await ShowCustomerCreditsLayaways(customer, isCreditsMode: true);
        }

        private async Task<bool> ShowCustomerCreditsLayaways(Customer customer, bool isCreditsMode)
        {
            bool shouldContinue = true;
            
            while (shouldContinue)
            {
                var customerView = new CustomerCreditsLayawaysView();
                var customerViewModel = new CustomerCreditsLayawaysViewModel(
                    _creditService,
                    _layawayService,
                    _authService);

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
                                await ShowCreditDetail(credit);
                                actionHandled = true;
                            }
                            break;
                        case "AddPaymentLayaway":
                            if (result.Item2 is Layaway layaway)
                            {
                                await ShowLayawayDetail(layaway);
                                actionHandled = true;
                            }
                            break;
                        case "DeliverLayaway":
                            if (result.Item2 is Layaway deliverLayaway)
                            {
                                actionHandled = await ShowDeliverLayaway(deliverLayaway);
                            }
                            break;
                    }
                    
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
            bool shouldContinue = true;
            bool paymentWasMade = false;
            bool creditCompleted = false;
            
            while (shouldContinue)
            {
                var detailView = new CreditLayawayDetailView();
                var detailViewModel = new CreditLayawayDetailViewModel(
                    _creditService,
                    _layawayService,
                    _customerService,
                    _authService);

                await detailViewModel.InitializeForCreditAsync(credit.Id);
                detailView.DataContext = detailViewModel;

                await detailView.ShowDialog(this);

                if (detailView.Tag == null)
                {
                    shouldContinue = false;
                    break;
                }

                if (detailView.Tag is ValueTuple<string, CreditLayawayDetailViewModel> result)
                {
                    bool actionHandled = false;
                    
                    switch (result.Item1)
                    {
                        case "AddPayment":
                            var creditFromVm = result.Item2.GetCredit();
                            if (creditFromVm != null)
                            {
                                var balanceBefore = creditFromVm.RemainingBalance;
                                actionHandled = await ShowAddPaymentToCredit(creditFromVm);
                                if (actionHandled)
                                {
                                    paymentWasMade = true;
                                    shouldContinue = false;
                                    
                                    // Verificar si el crédito se completó
                                    var updatedCredit = await _creditService.GetByIdAsync(creditFromVm.Id);
                                    if (updatedCredit != null && updatedCredit.RemainingBalance <= 0 && balanceBefore > 0)
                                    {
                                        creditCompleted = true;
                                    }
                                }
                            }
                            break;
                        case "Print":
                            var creditTicket = await _creditService.RecoverTicketAsync(credit.Id);
                            if (creditTicket != null)
                            {
                                var ticketService = new TicketService();
                                var ticketText = ticketService.GenerateTicketText(creditTicket, TicketType.Credit);
                                await ShowTicketDialog(credit.Folio, ticketText);
                            }
                            else
                            {
                                ShowMessageDialog("Error", "No se pudo recuperar el ticket");
                            }
                            shouldContinue = false;
                            break;
                        default:
                            shouldContinue = false;
                            break;
                    }
                    
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
            
            if (creditCompleted)
            {
                ShowMessageDialog("¡Crédito completado!", "El crédito ha sido liquidado correctamente. Saldo: $0.00");
            }
            else if (paymentWasMade)
            {
                ShowMessageDialog("Éxito", "Pago registrado correctamente");
            }
        }

        private async Task ShowLayawayDetail(Layaway layaway)
        {
            bool shouldContinue = true;
            bool paymentWasMade = false;
            bool layawayDelivered = false;
            
            while (shouldContinue)
            {
                var detailView = new CreditLayawayDetailView();
                var detailViewModel = new CreditLayawayDetailViewModel(
                    _creditService,
                    _layawayService,
                    _customerService,
                    _authService);

                await detailViewModel.InitializeForLayawayAsync(layaway.Id);
                detailView.DataContext = detailViewModel;

                await detailView.ShowDialog(this);

                if (detailView.Tag == null)
                {
                    shouldContinue = false;
                    break;
                }

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
                            var layawayTicket = await _layawayService.RecoverTicketAsync(layaway.Id);
                            if (layawayTicket != null)
                            {
                                var ticketService = new TicketService();
                                var ticketText = ticketService.GenerateTicketText(layawayTicket, TicketType.Layaway);
                                await ShowTicketDialog(layaway.Folio, ticketText);
                            }
                            else
                            {
                                ShowMessageDialog("Error", "No se pudo recuperar el ticket");
                            }
                            shouldContinue = false;
                            break;
                        default:
                            shouldContinue = false;
                            break;
                    }
                    
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
            var customer = await _customerService.GetByIdAsync(credit.CustomerId);
            if (customer == null)
            {
                ShowMessageDialog("Error", "No se encontró el cliente");
                return false;
            }

            var addPaymentView = new AddPaymentView();
            var addPaymentViewModel = new AddPaymentViewModel(
                _creditService,
                null!,
                _authService);

            await addPaymentViewModel.InitializeForCreditAsync(credit.Id, customer);
            addPaymentView.DataContext = addPaymentViewModel;

            await addPaymentView.ShowDialog(this);

            if (addPaymentView.Tag is PaymentResult result && result.Success)
            {
                return true;
            }
            
            return false;
        }

        private async Task<bool> ShowAddPaymentToLayaway(Layaway layaway)
        {
            var customer = await _customerService.GetByIdAsync(layaway.CustomerId);
            if (customer == null)
            {
                ShowMessageDialog("Error", "No se encontró el cliente");
                return false;
            }

            var addPaymentView = new AddPaymentView();
            var addPaymentViewModel = new AddPaymentViewModel(
                null!,
                _layawayService,
                _authService);

            await addPaymentViewModel.InitializeForLayawayAsync(layaway.Id, customer);
            addPaymentView.DataContext = addPaymentViewModel;

            await addPaymentView.ShowDialog(this);

            if (addPaymentView.Tag is PaymentResult result && result.Success)
            {
                return true;
            }
            
            return false;
        }

        private async Task<bool> ShowDeliverLayaway(Layaway layaway)
        {
            if (layaway.RemainingBalance > 0)
            {
                ShowMessageDialog("Advertencia", 
                    $"El apartado debe estar completamente pagado para poder entregarse.\nFaltan ${layaway.RemainingBalance:N2}");
                return false;
            }

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
                    var userId = _authService?.CurrentUser?.Id ?? 1;
                    await _layawayService.MarkAsDeliveredAsync(layaway.Id, userId);
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

            return confirmDialog.Tag is string tag && tag == "delivered";
        }

        private async Task ShowMessageDialogAsync(string title, string message)
        {
            await DialogHelper.ShowMessageDialog(this, title, message);
        }
        
        private async void ShowMessageDialog(string title, string message)
        {
            await DialogHelper.ShowMessageDialog(this, title, message);
        }

        private async Task ShowTicketDialog(string folio, string ticketText)
        {
            await DialogHelper.ShowTicketDialog(this, folio, ticketText);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is CreditsLayawaysMenuViewModel vm)
            {
                vm.OptionSelected -= OnOptionSelected;
                vm.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Services;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CustomerCreditsLayawaysView : Window
    {
        private CustomerCreditsLayawaysViewModel? _viewModel;

        public CustomerCreditsLayawaysView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CustomerCreditsLayawaysViewModel;

            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.AddPaymentToCredit += OnAddPaymentToCredit;
                _viewModel.AddPaymentToLayaway += OnAddPaymentToLayaway;
                _viewModel.DeliverLayaway += OnDeliverLayaway;
                _viewModel.PrintCredit += OnPrintCredit;
                _viewModel.PrintLayaway += OnPrintLayaway;
            }

            // Configurar handler para Enter en ambos DataGrids
            var creditsGrid = this.FindControl<DataGrid>("CreditsDataGrid");
            if (creditsGrid != null)
            {
                creditsGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }

            var layawaysGrid = this.FindControl<DataGrid>("LayawaysDataGrid");
            if (layawaysGrid != null)
            {
                layawaysGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // PreviewKeyDown (Tunneling) se ejecuta ANTES de que el DataGrid procese la tecla
            if (e.Key == Key.Enter && _viewModel != null)
            {
                var hasSelection = _viewModel.IsCreditsMode 
                    ? _viewModel.SelectedCredit != null 
                    : _viewModel.SelectedLayaway != null;
                    
                if (hasSelection)
                {
                    _viewModel.AddPaymentCommand.Execute(null);
                    e.Handled = true; // Evitar que el DataGrid navegue a la siguiente fila
                }
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private async void OnAddPaymentToCredit(object? sender, Credit e)
        {
            await ShowCreditDetailDialog(e);
        }

        private async void OnAddPaymentToLayaway(object? sender, Layaway e)
        {
            await ShowLayawayDetailDialog(e);
        }

        private void OnDeliverLayaway(object? sender, Layaway e)
        {
            Tag = ("DeliverLayaway", e);
            Close();
        }

        private async void OnPrintCredit(object? sender, Credit e)
        {
            try
            {
                var creditService = new Services.CreditService(new Data.DatabaseService());
                var creditTicket = await creditService.RecoverTicketAsync(e.Id);
                
                if (creditTicket != null)
                {
                    var ticketService = new Services.TicketService();
                    var ticketText = ticketService.GenerateTicketText(creditTicket, Services.TicketType.Credit);
                    await DialogHelper.ShowTicketDialog(this, e.Folio, ticketText);
                }
            }
            catch { }
        }

        private async void OnPrintLayaway(object? sender, Layaway e)
        {
            try
            {
                var layawayService = new Services.LayawayService(new Data.DatabaseService());
                var layawayTicket = await layawayService.RecoverTicketAsync(e.Id);
                
                if (layawayTicket != null)
                {
                    var ticketService = new Services.TicketService();
                    var ticketText = ticketService.GenerateTicketText(layawayTicket, Services.TicketType.Layaway);
                    await DialogHelper.ShowTicketDialog(this, e.Folio, ticketText);
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task ShowCreditDetailDialog(Credit credit)
        {
            // Obtener servicios
            var app = (Avalonia.Application.Current as App);
            if (app == null) return;

            var creditService = app.GetCreditService();
            var layawayService = app.GetLayawayService();
            var customerService = app.GetCustomerService();
            var authService = app.GetAuthService();

            if (creditService == null || layawayService == null || customerService == null || authService == null)
                return;

            bool shouldContinue = true;
            
            while (shouldContinue)
            {
                // Crear vista de detalle
                var detailView = new CreditLayawayDetailView();
                var detailViewModel = new CreditLayawayDetailViewModel(
                    creditService,
                    layawayService,
                    customerService,
                    authService);

                await detailViewModel.InitializeForCreditAsync(credit.Id);
                detailView.DataContext = detailViewModel;

                // Mostrar como diálogo hijo
                await detailView.ShowDialog(this);

                // Manejar el resultado
                if (detailView.Tag == null)
                {
                    shouldContinue = false;
                    break;
                }

                if (detailView.Tag is ValueTuple<string, CreditLayawayDetailViewModel> result)
                {
                    switch (result.Item1)
                    {
                        case "AddPayment":
                            var creditFromVm = result.Item2.GetCredit();
                            if (creditFromVm != null)
                            {
                                var paymentAdded = await ShowAddPaymentToCreditDialog(creditFromVm);
                                if (paymentAdded)
                                {
                                    // Recargar el crédito actualizado
                                    credit = await creditService.GetByIdAsync(credit.Id) ?? credit;
                                    shouldContinue = true; // Volver a mostrar el detalle actualizado
                                }
                                else
                                {
                                    shouldContinue = true; // Volver al detalle
                                }
                            }
                            else
                            {
                                shouldContinue = false;
                            }
                            break;
                        case "Print":
                            shouldContinue = false;
                            break;
                        default:
                            shouldContinue = false;
                            break;
                    }
                }
                else
                {
                    shouldContinue = false;
                }
            }

            // Recargar datos de la lista después de cerrar el detalle
            if (_viewModel != null)
            {
                await _viewModel.LoadDataCommand.ExecuteAsync(null);
            }
        }

        private async System.Threading.Tasks.Task ShowLayawayDetailDialog(Layaway layaway)
        {
            // Obtener servicios
            var app = (Avalonia.Application.Current as App);
            if (app == null) return;

            var creditService = app.GetCreditService();
            var layawayService = app.GetLayawayService();
            var customerService = app.GetCustomerService();
            var authService = app.GetAuthService();

            if (creditService == null || layawayService == null || customerService == null || authService == null)
                return;

            bool shouldContinue = true;
            
            while (shouldContinue)
            {
                // Crear vista de detalle
                var detailView = new CreditLayawayDetailView();
                var detailViewModel = new CreditLayawayDetailViewModel(
                    creditService,
                    layawayService,
                    customerService,
                    authService);

                await detailViewModel.InitializeForLayawayAsync(layaway.Id);
                detailView.DataContext = detailViewModel;

                // Mostrar como diálogo hijo
                await detailView.ShowDialog(this);

                // Manejar el resultado
                if (detailView.Tag == null)
                {
                    shouldContinue = false;
                    break;
                }

                if (detailView.Tag is ValueTuple<string, CreditLayawayDetailViewModel> result)
                {
                    switch (result.Item1)
                    {
                        case "AddPayment":
                            var layawayFromVm = result.Item2.GetLayaway();
                            if (layawayFromVm != null)
                            {
                                var paymentAdded = await ShowAddPaymentToLayawayDialog(layawayFromVm);
                                if (paymentAdded)
                                {
                                    // Recargar el apartado actualizado
                                    layaway = await layawayService.GetByIdAsync(layaway.Id) ?? layaway;
                                    shouldContinue = true; // Volver a mostrar el detalle actualizado
                                }
                                else
                                {
                                    shouldContinue = true; // Volver al detalle
                                }
                            }
                            else
                            {
                                shouldContinue = false;
                            }
                            break;
                        case "Deliver":
                            var layawayToDeliver = result.Item2.GetLayaway();
                            if (layawayToDeliver != null)
                            {
                                var delivered = await ShowDeliverLayawayDialog(layawayToDeliver);
                                if (delivered)
                                {
                                    shouldContinue = false;
                                }
                                else
                                {
                                    shouldContinue = true;
                                }
                            }
                            else
                            {
                                shouldContinue = false;
                            }
                            break;
                        case "Print":
                            shouldContinue = false;
                            break;
                        default:
                            shouldContinue = false;
                            break;
                    }
                }
                else
                {
                    shouldContinue = false;
                }
            }

            // Recargar datos de la lista después de cerrar el detalle
            if (_viewModel != null)
            {
                await _viewModel.LoadDataCommand.ExecuteAsync(null);
            }
        }

        private async System.Threading.Tasks.Task<bool> ShowAddPaymentToCreditDialog(Credit credit)
        {
            var app = (Avalonia.Application.Current as App);
            if (app == null) return false;

            var creditService = app.GetCreditService();
            var customerService = app.GetCustomerService();
            var authService = app.GetAuthService();

            if (creditService == null || customerService == null || authService == null)
                return false;

            var customer = await customerService.GetByIdAsync(credit.CustomerId);
            if (customer == null)
            {
                await DialogHelper.ShowMessageDialog(this, "Error", "No se encontró el cliente");
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

            if (addPaymentView.Tag is PaymentResult result && result.Success)
            {
                return true;
            }
            
            return false;
        }

        private async System.Threading.Tasks.Task<bool> ShowAddPaymentToLayawayDialog(Layaway layaway)
        {
            var app = (Avalonia.Application.Current as App);
            if (app == null) return false;

            var layawayService = app.GetLayawayService();
            var customerService = app.GetCustomerService();
            var authService = app.GetAuthService();

            if (layawayService == null || customerService == null || authService == null)
                return false;

            var customer = await customerService.GetByIdAsync(layaway.CustomerId);
            if (customer == null)
            {
                await DialogHelper.ShowMessageDialog(this, "Error", "No se encontró el cliente");
                return false;
            }

            var balanceBefore = layaway.RemainingBalance;
            
            var addPaymentView = new AddPaymentView();
            var addPaymentViewModel = new AddPaymentViewModel(
                null!,
                layawayService,
                authService);

            await addPaymentViewModel.InitializeForLayawayAsync(layaway.Id, customer);
            addPaymentView.DataContext = addPaymentViewModel;

            await addPaymentView.ShowDialog(this);

            if (addPaymentView.Tag is PaymentResult result && result.Success)
            {
                // Verificar si el apartado se completó con este pago
                var updatedLayaway = await layawayService.GetByIdAsync(layaway.Id);
                if (updatedLayaway != null && updatedLayaway.RemainingBalance <= 0 && balanceBefore > 0)
                {
                    // Apartado completado, preguntar si desea entregar
                    var shouldDeliver = await ShowConfirmDeliverDialog();
                    if (shouldDeliver)
                    {
                        await ShowDeliverLayawayDialog(updatedLayaway);
                    }
                }
                
                return true;
            }
            
            return false;
        }

        private async System.Threading.Tasks.Task<bool> ShowDeliverLayawayDialog(Layaway layaway)
        {
            var app = (Avalonia.Application.Current as App);
            if (app == null) return false;

            var layawayService = app.GetLayawayService();
            var authService = app.GetAuthService();

            if (layawayService == null || authService == null)
                return false;

            if (layaway.RemainingBalance > 0)
            {
                await DialogHelper.ShowMessageDialog(this, "Advertencia", 
                    $"El apartado debe estar completamente pagado para poder entregarse.\\nFaltan ${layaway.RemainingBalance:N2}");
                return false;
            }

            // Confirmar entrega
            var result = await DialogHelper.ShowConfirmDialog(this, "Confirmar Entrega", 
                $"¿Confirmar entrega del apartado #{layaway.Folio}?");
            
            if (result)
            {
                try
                {
                    var userId = authService.CurrentUser?.Id ?? 1;
                    await layawayService.MarkAsDeliveredAsync(layaway.Id, userId);
                    await DialogHelper.ShowMessageDialog(this, "Éxito", "Apartado entregado correctamente");
                    return true;
                }
                catch (Exception ex)
                {
                    await DialogHelper.ShowMessageDialog(this, "Error", $"Error al entregar apartado: {ex.Message}");
                    return false;
                }
            }
            
            return false;
        }

        private async System.Threading.Tasks.Task<bool> ShowConfirmDeliverDialog()
        {
            var result = await DialogHelper.ShowConfirmDialog(this, "Apartado Completado", 
                "El apartado ha sido pagado.\\n¿Desea entregar la mercancía al cliente?");
            return result;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, Close },
                    { Key.Enter, () => _viewModel.AddPaymentCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                if (e.Key == Key.F6 && !_viewModel.IsCreditsMode)
                {
                    _viewModel.DeliverCommand.Execute(null);
                    e.Handled = true;
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
                _viewModel.AddPaymentToCredit -= OnAddPaymentToCredit;
                _viewModel.AddPaymentToLayaway -= OnAddPaymentToLayaway;
                _viewModel.DeliverLayaway -= OnDeliverLayaway;
                _viewModel.PrintCredit -= OnPrintCredit;
                _viewModel.PrintLayaway -= OnPrintLayaway;
            }
            base.OnClosed(e);
        }
    }
}

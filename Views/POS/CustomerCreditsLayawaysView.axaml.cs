using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
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
            // Asegurar que la ventana tenga focus para recibir eventos de teclado
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Asegurar que la ventana tenga focus para recibir eventos de teclado
            Focus();
            
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
            
            // Establecer focus en el DataGrid apropiado
            SetFocusToDataGrid();
        }

        private void SetFocusToDataGrid()
        {
            if (_viewModel == null) return;
            
            if (_viewModel.IsCreditsMode)
            {
                var dataGrid = this.FindControl<DataGrid>("DataGridCredits");
                dataGrid?.Focus();
            }
            else
            {
                var dataGrid = this.FindControl<DataGrid>("DataGridLayaways");
                dataGrid?.Focus();
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnAddPaymentToCredit(object? sender, Models.Credit e)
        {
            Tag = ("AddPaymentCredit", e);
            Close();
        }

        private void OnAddPaymentToLayaway(object? sender, Models.Layaway e)
        {
            Tag = ("AddPaymentLayaway", e);
            Close();
        }

        private void OnDeliverLayaway(object? sender, Models.Layaway e)
        {
            Tag = ("DeliverLayaway", e);
            Close();
        }

        private async void OnPrintCredit(object? sender, Models.Credit e)
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
            catch
            {
                // Silently fail - user can try again
            }
        }

        private async void OnPrintLayaway(object? sender, Models.Layaway e)
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
            catch
            {
                // Silently fail - user can try again
            }
        }

        private void DataGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            // Capturar Enter ANTES de que el DataGrid lo procese
            if (e.Key == Key.Enter)
            {
                _viewModel?.AddPaymentCommand.Execute(null);
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CloseCommand.Execute(null) },
                    { Key.F5, () => _viewModel.AddPaymentCommand.Execute(null) },
                    { Key.F7, () => _viewModel.PrintCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // F6 solo disponible para Layaways (no créditos)
                if (e.Key == Key.F6 && _viewModel.IsCreditsMode == false)
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

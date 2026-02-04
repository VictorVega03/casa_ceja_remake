using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CasaCejaRemake.Models;
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
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnAddPaymentToCredit(object? sender, Credit e)
        {
            Tag = ("AddPaymentCredit", e);
            Close();
        }

        private void OnAddPaymentToLayaway(object? sender, Layaway e)
        {
            Tag = ("AddPaymentLayaway", e);
            Close();
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, Close },
                    { Key.F5, () => _viewModel.AddPaymentCommand.Execute(null) },
                    { Key.F7, () => _viewModel.PrintCommand.Execute(null) }
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

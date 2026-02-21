using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreateCreditView : Window
    {
        private CreateCreditViewModel? _viewModel;

        public CreateCreditView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CreateCreditViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CreditCreated += OnCreditCreated;
                _viewModel.Cancelled += OnCancelled;
            }
        }

        private async void OnCreditCreated(object? sender, Credit e)
        {
            // Generar y mostrar ticket de crédito recién creado
            try
            {
                var app = Avalonia.Application.Current as CasaCejaRemake.App;
                var creditService = app?.GetCreditService();
                if (creditService != null)
                {
                    var ticketData = await creditService.RecoverTicketAsync(e.Id);
                    if (ticketData != null)
                    {
                        var ticketService = new CasaCejaRemake.Services.TicketService();
                        var rfc = app?.GetConfigService()?.PosTerminalConfig.Rfc ?? string.Empty;
                        var ticketText = ticketService.GenerateTicketText(ticketData, CasaCejaRemake.Services.TicketType.Credit);
                        await DialogHelper.ShowTicketDialog(this, e.Folio, ticketText);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateCreditView] Error mostrando ticket: {ex.Message}");
            }

            Tag = e;
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is CreateCreditViewModel vm)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Enter, () => vm.ConfirmCommand.Execute(null) },
                    { Key.Escape, () => vm.CancelCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // Delegar otros atajos al ViewModel
                vm.HandleKeyPress(e.Key.ToString());
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CreditCreated -= OnCreditCreated;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

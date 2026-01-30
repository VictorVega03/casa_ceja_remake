using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreditLayawayDetailView : Window
    {
        private CreditLayawayDetailViewModel? _viewModel;

        public CreditLayawayDetailView()
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
            _viewModel = DataContext as CreditLayawayDetailViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.AddPaymentRequested += OnAddPaymentRequested;
                _viewModel.DeliverRequested += OnDeliverRequested;
                _viewModel.PrintRequested += OnPrintRequested;
                _viewModel.CloseRequested += OnCloseRequested;
            }
        }

        private void OnAddPaymentRequested(object? sender, EventArgs e)
        {
            Tag = ("AddPayment", _viewModel);
            Close();
        }

        private void OnDeliverRequested(object? sender, EventArgs e)
        {
            Tag = ("Deliver", _viewModel);
            Close();
        }

        private void OnPrintRequested(object? sender, EventArgs e)
        {
            // TODO: Implement print
            Tag = ("Print", _viewModel);
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
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
                if (e.Key == Key.F6 && _viewModel.IsCredit == false)
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
                _viewModel.AddPaymentRequested -= OnAddPaymentRequested;
                _viewModel.DeliverRequested -= OnDeliverRequested;
                _viewModel.PrintRequested -= OnPrintRequested;
                _viewModel.CloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}

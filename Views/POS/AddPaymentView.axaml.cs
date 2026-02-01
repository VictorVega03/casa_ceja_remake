using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class AddPaymentView : Window
    {
        private AddPaymentViewModel? _viewModel;

        public AddPaymentView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            // Asegurar que la ventana tenga focus
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as AddPaymentViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.PaymentCompleted += OnPaymentCompleted;
                _viewModel.Cancelled += OnCancelled;
            }

            // Focus y seleccionar todo el texto en el input
            var txtAmount = this.FindControl<TextBox>("TxtCurrentAmount");
            if (txtAmount != null)
            {
                txtAmount.Focus();
                txtAmount.SelectAll();
            }
        }

        private void OnPaymentCompleted(object? sender, PaymentResult e)
        {
            Tag = e;
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel == null)
            {
                base.OnKeyDown(e);
                return;
            }

            // ESC para cancelar
            if (e.Key == Key.Escape)
            {
                _viewModel.CancelCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // F5 para confirmar
            if (e.Key == Key.F5)
            {
                _viewModel.ConfirmCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // F4 para pagar restante
            if (e.Key == Key.F4)
            {
                _viewModel.PayRemainingCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Enter para agregar pago si hay monto, o confirmar si ya se cubrió
            if (e.Key == Key.Enter)
            {
                if (_viewModel.CanConfirm)
                {
                    _viewModel.ConfirmCommand.Execute(null);
                }
                else
                {
                    _viewModel.AddPaymentCommand.Execute(null);
                }
                e.Handled = true;
                return;
            }

            // Teclas 1-4 para seleccionar método de pago
            if (e.Key >= Key.D1 && e.Key <= Key.D4)
            {
                string method = e.Key switch
                {
                    Key.D1 => "Efectivo",
                    Key.D2 => "Debito",
                    Key.D3 => "Credito",
                    Key.D4 => "Transferencia",
                    _ => "Efectivo"
                };
                _viewModel.SelectMethodCommand.Execute(method);
                e.Handled = true;
                return;
            }

            // Flechas para ajustar monto
            if (e.Key == Key.Left)
            {
                _viewModel.AdjustAmount(-50);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right)
            {
                _viewModel.AdjustAmount(50);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Up)
            {
                _viewModel.AdjustAmount(100);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Down)
            {
                _viewModel.AdjustAmount(-100);
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PaymentCompleted -= OnPaymentCompleted;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

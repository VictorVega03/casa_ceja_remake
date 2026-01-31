using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class PaymentView : Window
    {
        private PaymentViewModel? _viewModel;

        public PaymentView()
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
            _viewModel = DataContext as PaymentViewModel;
            TxtCurrentAmount.Focus();
            TxtCurrentAmount.SelectAll();
            
            // SelectAll al recibir focus
            TxtCurrentAmount.GotFocus += (s, args) => TxtCurrentAmount.SelectAll();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CancelCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // Enter con validación - confirma venta si ya está completo
                if (e.Key == Key.Enter && _viewModel.CanConfirm)
                {
                    _viewModel.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                
                // Enter - agregar pago si aún falta por pagar
                if (e.Key == Key.Enter && !_viewModel.CanConfirm && _viewModel.CurrentAmount > 0)
                {
                    _viewModel.AddPaymentCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // Flechas para ajustar monto: ← -50, → +50, ↑ +100, ↓ -100
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

                // Teclas numéricas para selección de método de pago
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectMethodCommand.Execute("Efectivo"), Key.D1, Key.NumPad1))
                {
                    return;
                }
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectMethodCommand.Execute("Debito"), Key.D2, Key.NumPad2))
                {
                    return;
                }
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectMethodCommand.Execute("Credito"), Key.D3, Key.NumPad3))
                {
                    return;
                }
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectMethodCommand.Execute("Transferencia"), Key.D4, Key.NumPad4))
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
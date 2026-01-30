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
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as PaymentViewModel;
            TxtAmountPaid.Focus();
            TxtAmountPaid.SelectAll();
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

                // Enter con validación
                if (e.Key == Key.Enter && _viewModel.CanConfirm)
                {
                    _viewModel.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // Teclas numéricas para selección de método de pago
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectCashCommand.Execute(null), Key.D1, Key.NumPad1))
                {
                    return;
                }
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectDebitCommand.Execute(null), Key.D2, Key.NumPad2))
                {
                    return;
                }
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectCreditCommand.Execute(null), Key.D3, Key.NumPad3))
                {
                    return;
                }
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.SelectTransferCommand.Execute(null), Key.D4, Key.NumPad4))
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
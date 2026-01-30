using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class OpenCashView : Window
    {
        private OpenCashViewModel? _viewModel;

        public OpenCashView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as OpenCashViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CashOpened += OnCashOpened;
                _viewModel.Cancelled += OnCancelled;
            }

            // Validación numérica para el campo de monto
            TxtOpeningAmount.AddHandler(TextInputEvent, OnAmountTextInput, RoutingStrategies.Tunnel);

            // Enfocar el campo de monto
            TxtOpeningAmount.Focus();
            TxtOpeningAmount.SelectAll();
        }

        private void OnAmountTextInput(object? sender, TextInputEventArgs e)
        {
            // Permitir solo números y un punto decimal
            if (string.IsNullOrEmpty(e.Text))
                return;

            foreach (char c in e.Text)
            {
                // Permitir números
                if (char.IsDigit(c))
                    continue;

                // Permitir un solo punto decimal
                if (c == '.' && TxtOpeningAmount.Text?.Contains('.') == false)
                    continue;

                // Rechazar cualquier otro carácter
                e.Handled = true;
                return;
            }
        }

        private void OnCashOpened(object? sender, CashClose cashClose)
        {
            Tag = cashClose;
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Tag = null;
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                // F5 y Enter ejecutan la misma acción
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.OpenCashCommand.Execute(null), Key.F5, Key.Enter))
                {
                    return;
                }

                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CancelCommand.Execute(null) },
                    { Key.F1, () => { TxtOpeningAmount.Focus(); TxtOpeningAmount.SelectAll(); } }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CashOpened -= OnCashOpened;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

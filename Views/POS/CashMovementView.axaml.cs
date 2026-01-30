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
    public partial class CashMovementView : Window
    {
        private CashMovementViewModel? _viewModel;

        public CashMovementView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CashMovementViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.MovementAdded += OnMovementAdded;
                _viewModel.Cancelled += OnCancelled;
            }

            // Validación numérica para el campo de monto
            TxtAmount.AddHandler(TextInputEvent, OnAmountTextInput, RoutingStrategies.Tunnel);

            // Enfocar el campo de concepto
            TxtConcept.Focus();
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
                if (c == '.' && TxtAmount.Text?.Contains('.') == false)
                    continue;

                // Rechazar cualquier otro carácter
                e.Handled = true;
                return;
            }
        }

        private void OnMovementAdded(object? sender, CashMovement movement)
        {
            Tag = movement;
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
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.ConfirmCommand.Execute(null), Key.F5, Key.Enter))
                {
                    return;
                }

                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CancelCommand.Execute(null) },
                    { Key.F1, () => TxtConcept.Focus() },
                    { Key.F2, () => { TxtAmount.Focus(); TxtAmount.SelectAll(); } }
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
                _viewModel.MovementAdded -= OnMovementAdded;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

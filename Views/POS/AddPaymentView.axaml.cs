using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

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
            // Asegurar que la ventana tenga focus para recibir eventos de teclado
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
            if (_viewModel != null)
            {
                // Enter y F5 ejecutan la misma acción
                if (KeyboardShortcutHelper.HandleShortcuts(e, () => _viewModel.ConfirmCommand.Execute(null), Key.Enter, Key.F5))
                {
                    return;
                }

                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CancelCommand.Execute(null) }
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
                _viewModel.PaymentCompleted -= OnPaymentCompleted;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

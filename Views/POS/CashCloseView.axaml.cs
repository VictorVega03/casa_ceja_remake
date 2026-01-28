using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CashCloseView : Window
    {
        private CashCloseViewModel? _viewModel;

        public CashCloseView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CashCloseViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CloseCompleted += OnCloseCompleted;
                _viewModel.Cancelled += OnCancelled;
                
                // Cargar datos automáticamente
                await _viewModel.LoadDataCommand.ExecuteAsync(null);
            }

            // Validación numérica para el campo de monto declarado
            TxtDeclaredAmount.AddHandler(TextInputEvent, OnAmountTextInput, RoutingStrategies.Tunnel);

            // Enfocar el campo de monto declarado
            TxtDeclaredAmount.Focus();
            TxtDeclaredAmount.SelectAll();
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
                if (c == '.' && TxtDeclaredAmount.Text?.Contains('.') == false)
                    continue;

                // Rechazar cualquier otro carácter
                e.Handled = true;
                return;
            }
        }

        private void OnCloseCompleted(object? sender, CashClose cashClose)
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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.F5:
                    _viewModel?.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseCompleted -= OnCloseCompleted;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

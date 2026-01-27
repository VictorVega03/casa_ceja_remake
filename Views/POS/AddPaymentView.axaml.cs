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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F4:
                    _viewModel?.PayAllCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F5:
                    _viewModel?.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    _viewModel?.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
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

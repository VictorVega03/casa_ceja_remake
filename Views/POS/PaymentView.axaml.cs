using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (_viewModel?.CanConfirm == true)
                    {
                        _viewModel.ConfirmCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.D1:
                case Key.NumPad1:
                    _viewModel?.SelectCashCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.D2:
                case Key.NumPad2:
                    _viewModel?.SelectDebitCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.D3:
                case Key.NumPad3:
                    _viewModel?.SelectCreditCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.D4:
                case Key.NumPad4:
                    _viewModel?.SelectTransferCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
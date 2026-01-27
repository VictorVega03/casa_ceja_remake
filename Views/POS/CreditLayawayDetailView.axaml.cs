using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreditLayawayDetailView : Window
    {
        private CreditLayawayDetailViewModel? _viewModel;

        public CreditLayawayDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    _viewModel?.CloseCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F5:
                    _viewModel?.AddPaymentCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F6:
                    if (_viewModel?.IsCredit == false)
                    {
                        _viewModel?.DeliverCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.F7:
                    _viewModel?.PrintCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
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

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CustomerCreditsLayawaysView : Window
    {
        private CustomerCreditsLayawaysViewModel? _viewModel;

        public CustomerCreditsLayawaysView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CustomerCreditsLayawaysViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.AddPaymentToCredit += OnAddPaymentToCredit;
                _viewModel.AddPaymentToLayaway += OnAddPaymentToLayaway;
                _viewModel.DeliverLayaway += OnDeliverLayaway;
                _viewModel.PrintCredit += OnPrintCredit;
                _viewModel.PrintLayaway += OnPrintLayaway;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnAddPaymentToCredit(object? sender, Models.Credit e)
        {
            Tag = ("AddPaymentCredit", e);
            Close();
        }

        private void OnAddPaymentToLayaway(object? sender, Models.Layaway e)
        {
            Tag = ("AddPaymentLayaway", e);
            Close();
        }

        private void OnDeliverLayaway(object? sender, Models.Layaway e)
        {
            Tag = ("DeliverLayaway", e);
            Close();
        }

        private void OnPrintCredit(object? sender, Models.Credit e)
        {
            // TODO: Print credit ticket
        }

        private void OnPrintLayaway(object? sender, Models.Layaway e)
        {
            // TODO: Print layaway ticket
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
                    if (_viewModel?.IsCreditsMode == false)
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
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.AddPaymentToCredit -= OnAddPaymentToCredit;
                _viewModel.AddPaymentToLayaway -= OnAddPaymentToLayaway;
                _viewModel.DeliverLayaway -= OnDeliverLayaway;
                _viewModel.PrintCredit -= OnPrintCredit;
                _viewModel.PrintLayaway -= OnPrintLayaway;
            }
            base.OnClosed(e);
        }
    }
}

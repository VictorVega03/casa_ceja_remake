using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;

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

            // Enfocar el campo de monto
            TxtOpeningAmount.Focus();
            TxtOpeningAmount.SelectAll();
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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.F5:
                case Key.Enter:
                    _viewModel?.OpenCashCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F1:
                    TxtOpeningAmount.Focus();
                    TxtOpeningAmount.SelectAll();
                    e.Handled = true;
                    break;
            }
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

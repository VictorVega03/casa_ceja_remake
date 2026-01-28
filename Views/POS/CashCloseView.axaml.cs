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

            // Enfocar el campo de monto declarado
            TxtDeclaredAmount.Focus();
            TxtDeclaredAmount.SelectAll();
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

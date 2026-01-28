using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;

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

            // Enfocar el campo de concepto
            TxtConcept.Focus();
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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.F5:
                case Key.Enter:
                    _viewModel?.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F1:
                    TxtConcept.Focus();
                    e.Handled = true;
                    break;

                case Key.F2:
                    TxtAmount.Focus();
                    TxtAmount.SelectAll();
                    e.Handled = true;
                    break;
            }
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

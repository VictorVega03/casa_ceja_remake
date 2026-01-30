using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreateCreditView : Window
    {
        private CreateCreditViewModel? _viewModel;

        public CreateCreditView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CreateCreditViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CreditCreated += OnCreditCreated;
                _viewModel.Cancelled += OnCancelled;
            }
        }

        private void OnCreditCreated(object? sender, Credit e)
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
                case Key.Enter:
                    _viewModel?.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                default:
                    if (DataContext is CreateCreditViewModel vm)
                    {
                        vm.HandleKeyPress(e.Key.ToString());
                    }
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CreditCreated -= OnCreditCreated;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

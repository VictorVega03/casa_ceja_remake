using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CustomerActionView : Window
    {
        private CustomerActionViewModel? _viewModel;

        public CustomerActionView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CustomerActionViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.ActionSelected += OnActionSelected;
                _viewModel.Cancelled += OnCancelled;
            }
        }

        private void OnActionSelected(object? sender, CustomerActionOption e)
        {
            Tag = ("ActionSelected", e);
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is CustomerActionViewModel vm)
            {
                vm.HandleKeyPress(e.Key.ToString());
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ActionSelected -= OnActionSelected;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Views.POS
{
    public partial class QuickCustomerView : Window
    {
        private QuickCustomerViewModel? _viewModel;

        public QuickCustomerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as QuickCustomerViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CustomerCreated += OnCustomerCreated;
                _viewModel.Cancelled += OnCancelled;
            }
            
            NameBox?.Focus();
        }

        private void OnCustomerCreated(object? sender, Customer e)
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
            if (DataContext is QuickCustomerViewModel vm)
            {
                vm.HandleKeyPress(e.Key.ToString());
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CustomerCreated -= OnCustomerCreated;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

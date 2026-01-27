using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Views.POS
{
    public partial class CustomerSearchView : Window
    {
        private CustomerSearchViewModel? _viewModel;

        public CustomerSearchView()
        {
            InitializeComponent();

            // Focus search box when loaded
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CustomerSearchViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CustomerSelected += OnCustomerSelected;
                _viewModel.CreateNewRequested += OnCreateNewRequested;
                _viewModel.Cancelled += OnCancelled;
            }
            
            SearchBox?.Focus();
        }

        private void OnCustomerSelected(object? sender, Customer e)
        {
            Tag = ("CustomerSelected", e);
            Close();
        }

        private void OnCreateNewRequested(object? sender, EventArgs e)
        {
            Tag = "CreateNew";
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is CustomerSearchViewModel vm)
            {
                if (e.Key == Key.Enter)
                {
                    if (vm.SelectedCustomer != null)
                    {
                        vm.SelectCustomerCommand.Execute(null);
                    }
                    else
                    {
                        vm.SearchCommand.Execute(null);
                    }
                    e.Handled = true;
                }
                else
                {
                    vm.HandleKeyPress(e.Key.ToString());
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CustomerSelected -= OnCustomerSelected;
                _viewModel.CreateNewRequested -= OnCreateNewRequested;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

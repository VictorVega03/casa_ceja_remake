using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.ViewModels.Shared;
using System;
using System.Collections.Generic;

namespace CasaCejaRemake.Views.Shared
{
    public partial class UserDetailsView : Window
    {
        private UserFormViewModel? _viewModel;

        public UserDetailsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is UserFormViewModel vm)
            {
                _viewModel = vm;
                _viewModel.CloseRequested += OnCloseRequested;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && DataContext is UserFormViewModel vm)
            {
                vm.CancelCommand.Execute(null);
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CustomerDetailView : Window
    {
        private CustomerDetailViewModel? _viewModel;

        public CustomerDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CustomerDetailViewModel;

            if (_viewModel != null)
            {
                _viewModel.ViewCreditsRequested += OnViewCreditsRequested;
                _viewModel.ViewLayawaysRequested += OnViewLayawaysRequested;
                _viewModel.CloseRequested += OnCloseRequested;
            }

            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.HandleKeyPress(e.Key.ToString());
            }
            base.OnKeyDown(e);
        }

        private void OnViewCreditsRequested(object? sender, EventArgs e)
        {
            Tag = "ViewCredits";
            Close();
        }

        private void OnViewLayawaysRequested(object? sender, EventArgs e)
        {
            Tag = "ViewLayaways";
            Close();
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ViewCreditsRequested -= OnViewCreditsRequested;
                _viewModel.ViewLayawaysRequested -= OnViewLayawaysRequested;
                _viewModel.CloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}

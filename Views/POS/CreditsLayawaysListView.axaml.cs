using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreditsLayawaysListView : Window
    {
        private CreditsLayawaysListViewModel? _viewModel;

        public CreditsLayawaysListView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CreditsLayawaysListViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.ItemSelected += OnItemSelected;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnItemSelected(object? sender, ViewModels.POS.CreditLayawayListItemWrapper e)
        {
            Tag = ("ItemSelected", e);
            Close();
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

                case Key.Enter:
                    _viewModel?.SelectItemCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F1:
                    _viewModel?.SetFilterCommand.Execute("type");
                    e.Handled = true;
                    break;

                case Key.F2:
                    _viewModel?.SetFilterCommand.Execute("status");
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.ItemSelected -= OnItemSelected;
            }
            base.OnClosed(e);
        }
    }
}

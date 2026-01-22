using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class SearchProductView : Window
    {
        private SearchProductViewModel? _viewModel;

        public SearchProductView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as SearchProductViewModel;
            TxtSearch.Focus();

            if (GridResults != null)
                GridResults.DoubleTapped += GridResults_DoubleTapped;
        }

        private void GridResults_DoubleTapped(object? sender, TappedEventArgs e)
        {
            _viewModel?.SelectCurrentProduct();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (TxtSearch.IsFocused)
                    {
                        _ = _viewModel?.SearchCommand.ExecuteAsync(null);
                    }
                    else if (_viewModel?.SelectedProduct != null)
                    {
                        _viewModel.ConfirmCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.Down:
                    if (TxtSearch.IsFocused && _viewModel?.SearchResults.Count > 0)
                    {
                        if (GridResults != null)
                        {
                            GridResults.Focus();
                            _viewModel.SelectedProductIndex = 0;
                        }
                    }
                    break;

                case Key.Up:
                    if (GridResults?.IsFocused == true && _viewModel?.SelectedProductIndex == 0)
                    {
                        TxtSearch.Focus();
                    }
                    break;

                case Key.F3:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

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
            if (DataContext is SearchProductViewModel vm)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => vm.CancelCommand.Execute(null) },
                    { Key.F3, () => TxtSearch.Focus() } // Assuming TxtQuery should be TxtSearch based on original code
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // Enter con lógica condicional
                if (e.Key == Key.Enter)
                {
                    if (vm.SelectedProduct != null)
                    {
                        vm.ConfirmCommand.Execute(null); // Assuming AddProductCommand should be ConfirmCommand based on original code
                    }
                    else
                    {
                        vm.SearchCommand.Execute(null);
                    }
                    e.Handled = true;
                    return;
                }

                // Handle navigation for Down/Up keys
                if (e.Key == Key.Down)
                {
                    if (TxtSearch.IsFocused && vm.SearchResults.Count > 0)
                    {
                        if (GridResults != null)
                        {
                            GridResults.Focus();
                            vm.SelectedProductIndex = 0;
                        }
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Up)
                {
                    if (GridResults?.IsFocused == true && vm.SelectedProductIndex == 0)
                    {
                        TxtSearch.Focus();
                    }
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
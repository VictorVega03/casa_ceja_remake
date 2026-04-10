using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class OutputView : Window
    {
        private OutputsViewModel? _viewModel;

        public OutputView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as OutputsViewModel;

            if (_viewModel != null)
            {
                _viewModel.ShowMessageRequested += async (s, msg) =>
                {
                    await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(this, "Aviso", msg);
                };
            }

            SearchBox?.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel == null)
            {
                base.OnKeyDown(e);
                return;
            }

            var shortcuts = new Dictionary<Key, Action>
            {
                { Key.Escape, () => _viewModel.CancelCommand.Execute(null) },
                { Key.F5, () => _viewModel.SaveOutputCommand.Execute(null) }
            };

            if (casa_ceja_remake.Helpers.KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel?.SearchProductCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}

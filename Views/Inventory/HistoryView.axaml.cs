using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;
using System.Collections.Generic;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class HistoryView : Window
    {
        private HistoryViewModel? _viewModel;
        internal bool IsDetailOpen { get; set; }

        public HistoryView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, System.EventArgs e)
        {
            _viewModel = DataContext as HistoryViewModel;

            if (HistoryDataGrid != null)
            {
                HistoryDataGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel?.SelectedItem != null)
            {
                _viewModel.RequestDetail(_viewModel.SelectedItem);
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsDetailOpen) return;

            if (_viewModel == null)
            {
                base.OnKeyDown(e);
                return;
            }

            var shortcuts = new Dictionary<Key, System.Action>
            {
                { Key.Escape, () => _viewModel.GoBackCommand.Execute(null) },
                { Key.Enter, () => {
                    if (_viewModel.SelectedItem != null)
                    {
                        _viewModel.RequestDetail(_viewModel.SelectedItem);
                    }
                }}
            };

            if (casa_ceja_remake.Helpers.KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
            {
                return;
            }

            base.OnKeyDown(e);
        }
    }
}

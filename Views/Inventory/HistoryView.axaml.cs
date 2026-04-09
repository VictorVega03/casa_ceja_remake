using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class HistoryView : Window
    {
        public HistoryView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is HistoryViewModel vm)
            {
                if (e.Key == Key.Escape)
                {
                    vm.GoBackCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    if (vm.SelectedItem != null && HistoryDataGrid.IsFocused)
                    {
                        vm.RequestDetail(vm.SelectedItem);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}

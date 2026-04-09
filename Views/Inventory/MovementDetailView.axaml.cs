using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class MovementDetailView : Window
    {
        public MovementDetailView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is MovementDetailViewModel vm)
                {
                    vm.CloseCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}

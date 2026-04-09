using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class ProductDetailView : Window
    {
        public ProductDetailView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            
            // Allow native escape without asking
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    if (DataContext is ProductDetailViewModel vm)
                    {
                        vm.CloseCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            };
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is ProductDetailViewModel vm)
                {
                    vm.CloseCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Inventory;
using System;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class MovementDetailView : Window
    {
        public MovementDetailView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            ProductsGrid?.Focus();
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && DataContext is MovementDetailViewModel vm)
            {
                vm.CloseCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}

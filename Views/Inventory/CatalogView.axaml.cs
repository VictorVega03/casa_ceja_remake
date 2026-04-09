using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;
using System.Collections.Generic;
using System;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class CatalogView : Window
    {
        public CatalogView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is CatalogViewModel vm)
            {
                if (e.Key == Key.Escape)
                {
                    vm.GoBackCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                
                if (e.Key == Key.F2)
                {
                    vm.CreateProductCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F4)
                {
                    vm.ClearSearchCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F5)
                {
                    vm.SearchCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Enter)
                {
                    // Si el foco está en el TextBox de búsqueda, no queremos interceptar el Enter
                    // porque el KeyBinding del TextBox ya desencadena la búsqueda.
                    if (e.Source is TextBox) 
                    {
                        return; // Dejar que el TextBox lo maneje.
                    }

                    if (vm.SelectedProduct != null)
                    {
                        // Open Dialog mode
                        vm.RequestProductDetail(vm.SelectedProduct);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}

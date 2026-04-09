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
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is CatalogViewModel vm)
            {
                if (e.Key == Key.Escape)
                {
                    vm.GoBackCommand.Execute(null);
                    return;
                }
                
                if (e.Key == Key.F2)
                {
                    vm.CreateProductCommand.Execute(null);
                    return;
                }

                if (e.Key == Key.Enter)
                {
                    if (vm.SelectedProduct != null)
                    {
                        // Open ReadOnly mode
                        vm.ProductFormRequested?.Invoke(vm, vm.SelectedProduct);
                        e.Handled = true;
                    }
                }
            }
            base.OnKeyDown(e);
        }
    }
}

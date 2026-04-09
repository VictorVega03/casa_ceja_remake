using CasaCejaRemake.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class ProductDetailViewModel : ViewModelBase
    {
        [ObservableProperty]
        private Product _product;

        public event EventHandler? CloseRequested;

        public ProductDetailViewModel(Product product)
        {
            _product = product;
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

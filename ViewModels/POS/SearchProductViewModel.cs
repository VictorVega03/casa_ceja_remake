using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class SearchProductViewModel : ViewModelBase
    {
        private readonly SalesService _salesService;

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private int _selectedCategoryId;

        [ObservableProperty]
        private int _quantity = 1;

        [ObservableProperty]
        private Product? _selectedProduct;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _selectedProductIndex = -1;

        public ObservableCollection<Product> SearchResults { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        public event EventHandler<(Product, int)>? ProductSelected;
        public event EventHandler? Cancelled;

        public SearchProductViewModel(SalesService salesService)
        {
            _salesService = salesService;
        }

        public async Task InitializeAsync()
        {
            var categories = await _salesService.GetCategoriesAsync();
            
            // Agregar opcion "Todas"
            Categories.Add(new Category { Id = 0, Name = "Todas las categorias" });
            
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            // Cargar todos los productos al iniciar (modo catálogo)
            await SearchAsync();
        }

        partial void OnSearchTermChanged(string value)
        {
            // Busqueda automatica al escribir
            if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
            {
                _ = SearchAsync();
            }
        }

        partial void OnSelectedCategoryIdChanged(int value)
        {
            // Buscar al cambiar categoria
            _ = SearchAsync();
        }

        partial void OnSelectedProductChanged(Product? value)
        {
            if (value != null)
            {
                // TODO: Mostrar stock desde tabla de inventario
                // StatusMessage = $"Stock disponible: {value.Stock}";
            }
            else
            {
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            IsSearching = true;
            StatusMessage = "Buscando...";

            try
            {
                int? categoryId = SelectedCategoryId > 0 ? SelectedCategoryId : null;
                var results = await _salesService.SearchProductsAsync(SearchTerm, categoryId);

                SearchResults.Clear();
                foreach (var product in results)
                {
                    SearchResults.Add(product);
                }

                StatusMessage = $"{SearchResults.Count} productos encontrados";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSearching = false;
            }
        }

        [RelayCommand]
        private void IncrementQuantity()
        {
            // TODO: Implement stock validation from inventory table
            // if (SelectedProduct != null && Quantity < SelectedProduct.Stock)
            // {
                Quantity++;
            // }
        }

        [RelayCommand]
        private void DecrementQuantity()
        {
            if (Quantity > 1)
            {
                Quantity--;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            if (SelectedProduct == null)
            {
                StatusMessage = "Seleccione un producto";
                return;
            }

            if (Quantity <= 0)
            {
                StatusMessage = "La cantidad debe ser mayor a 0";
                return;
            }

            // TODO: Implement stock validation from inventory table
            // if (SelectedProduct.Stock < Quantity)
            // {
            //     StatusMessage = $"Stock insuficiente. Disponible: {SelectedProduct.Stock}";
            //     return;
            // }

            ProductSelected?.Invoke(this, (SelectedProduct, Quantity));
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Clear()
        {
            SearchTerm = string.Empty;
            SelectedCategoryId = 0;
            SearchResults.Clear();
            SelectedProduct = null;
            Quantity = 1;
            StatusMessage = string.Empty;
        }

        public void SelectCurrentProduct()
        {
            if (SelectedProduct != null)
            {
                Confirm();
            }
        }
    }
}
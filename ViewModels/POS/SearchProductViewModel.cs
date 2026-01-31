using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using System.Linq;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class SearchProductViewModel : ViewModelBase
    {
        private readonly SalesService _salesService;

        // Paginación
        private const int PageSize = 50;
        private System.Collections.Generic.List<Product> _allResults = new();

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private int _selectedCategoryId;

        [ObservableProperty]
        private int _selectedUnitId;

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

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalResults;

        [ObservableProperty]
        private bool _canGoPrevious;

        [ObservableProperty]
        private bool _canGoNext;

        [ObservableProperty]
        private string _paginationInfo = "Página 1 de 1";

        public ObservableCollection<Product> SearchResults { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Unit> Units { get; } = new();

        public event EventHandler<(Product, int)>? ProductSelected;
        public event EventHandler? Cancelled;

        public SearchProductViewModel(SalesService salesService)
        {
            _salesService = salesService;
        }

        public async Task InitializeAsync()
        {
            // Cargar categorías
            var categories = await _salesService.GetCategoriesAsync();
            Categories.Add(new Category { Id = 0, Name = "Todas las categorías" });
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            // Cargar unidades
            var units = await _salesService.GetUnitsAsync();
            Units.Add(new Unit { Id = 0, Name = "Todas las medidas" });
            foreach (var unit in units)
            {
                Units.Add(unit);
            }

            // Cargar todos los productos al iniciar (modo catálogo) - con paginación
            await SearchAsync();
        }

        // YA NO buscamos automáticamente al escribir, solo cuando presiona Enter
        // partial void OnSearchTermChanged(string value) { ... }

        partial void OnSelectedCategoryIdChanged(int value)
        {
            // Buscar al cambiar categoría
            _ = SearchAsync();
        }

        partial void OnSelectedUnitIdChanged(int value)
        {
            // Buscar al cambiar unidad
            _ = SearchAsync();
        }

        partial void OnSelectedProductChanged(Product? value)
        {
            if (value != null)
            {
                StatusMessage = $"Seleccionado: {value.Name}";
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
            CurrentPage = 1;

            try
            {
                int? categoryId = SelectedCategoryId > 0 ? SelectedCategoryId : null;
                int? unitId = SelectedUnitId > 0 ? SelectedUnitId : null;
                
                _allResults = await _salesService.SearchProductsWithUnitAsync(SearchTerm, categoryId, unitId);
                TotalResults = _allResults.Count;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalResults / PageSize));
                
                LoadCurrentPage();
                
                StatusMessage = $"{TotalResults} productos encontrados";
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

        private void LoadCurrentPage()
        {
            SearchResults.Clear();
            
            var pageItems = _allResults
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);
            
            foreach (var product in pageItems)
            {
                SearchResults.Add(product);
            }

            CanGoPrevious = CurrentPage > 1;
            CanGoNext = CurrentPage < TotalPages;
            PaginationInfo = $"Página {CurrentPage} de {TotalPages}";
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadCurrentPage();
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadCurrentPage();
            }
        }

        [RelayCommand]
        private void IncrementQuantity()
        {
            Quantity++;
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
            SelectedUnitId = 0;
            _allResults.Clear();
            SearchResults.Clear();
            SelectedProduct = null;
            Quantity = 1;
            CurrentPage = 1;
            TotalPages = 1;
            TotalResults = 0;
            PaginationInfo = "Página 1 de 1";
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
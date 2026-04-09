using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using System.Linq;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class CatalogViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly int _currentBranchId;

        private const int PageSize = 50;
        private System.Collections.Generic.List<Product> _allResults = new();

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private int _selectedCategoryId;

        [ObservableProperty]
        private int _selectedUnitId;

        [ObservableProperty]
        private Product? _selectedProduct;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

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

        public event EventHandler? GoBackRequested;
        public event EventHandler<Product?>? ProductFormRequested;
        public event EventHandler<Product>? ProductDetailRequested;

        public int CurrentBranchId { get; }

        public CatalogViewModel(InventoryService inventoryService, int branchId)
        {
            _inventoryService = inventoryService;
            CurrentBranchId = branchId;
            _currentBranchId = branchId;
            
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            IsSearching = true;
            try
            {
                // Truco para forzar el OnPropertyChanged cuando los items ya estén cargados
                SelectedCategoryId = -1;
                SelectedUnitId = -1;

                // Cargar categorías
                var categories = await _inventoryService.GetCategoriesAsync();
                Categories.Add(new Category { Id = 0, Name = "Todas las categorías" });
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Cargar unidades
                var units = await _inventoryService.GetUnitsAsync();
                Units.Add(new Unit { Id = 0, Name = "Todas las medidas" });
                foreach (var unit in units)
                {
                    Units.Add(unit);
                }

                // Seleccionar "Todas" por defecto, ahora sí dispara PropertyChanged
                SelectedCategoryId = 0;
                SelectedUnitId = 0;

                await SearchAsync();
            }
            finally
            {
                IsSearching = false;
            }
        }

        partial void OnSelectedCategoryIdChanged(int value)
        {
            _ = SearchAsync();
        }

        partial void OnSelectedUnitIdChanged(int value)
        {
            _ = SearchAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (_inventoryService == null) return;
            
            IsSearching = true;
            StatusMessage = "Buscando...";
            CurrentPage = 1;

            try
            {
                int? categoryId = SelectedCategoryId > 0 ? SelectedCategoryId : null;
                int? unitId = SelectedUnitId > 0 ? SelectedUnitId : null;
                
                _allResults = await _inventoryService.SearchProductsAsync(SearchTerm, categoryId, unitId);
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
        private void ClearSearch()
        {
            SearchTerm = string.Empty;
            SelectedCategoryId = 0;
            SelectedUnitId = 0;
            _ = SearchAsync();
        }

        [RelayCommand]
        private void CreateProduct()
        {
            ProductFormRequested?.Invoke(this, null);
        }

        public void RequestProductForm(Product? product)
        {
            ProductFormRequested?.Invoke(this, product);
        }

        public void RequestProductDetail(Product product)
        {
            ProductDetailRequested?.Invoke(this, product);
        }

        [RelayCommand]
        private void GoBack()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshData()
        {
            _ = SearchAsync();
        }
    }
}

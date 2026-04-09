using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class ProductFormViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly Product _product;

        [ObservableProperty]
        private string _barcode = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _categoryId;

        [ObservableProperty]
        private int _unitId;

        [ObservableProperty]
        private string _presentation = string.Empty;

        [ObservableProperty]
        private string _iva = "0";

        [ObservableProperty]
        private string _priceRetail = "0";

        [ObservableProperty]
        private string _priceWholesale = "0";

        [ObservableProperty]
        private string _wholesaleQuantity = "1";

        [ObservableProperty]
        private string _priceSpecial = "0";

        [ObservableProperty]
        private string _priceDealer = "0";

        [ObservableProperty]
        private bool _active = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _title = "Nuevo Producto";

        [ObservableProperty]
        private bool _isMultipleMode = false;

        [ObservableProperty]
        private bool _isReadOnlyView = false;

        [ObservableProperty]
        private string _initialQuantity = "0";

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Unit> Units { get; } = new();
        public ObservableCollection<Product> PendingProducts { get; } = new();

        public event EventHandler? SaveCompleted;
        public event EventHandler? CancelRequested;
        public event EventHandler? StartSaveConfirmation;

        private readonly int _currentBranchId;

        public ProductFormViewModel(InventoryService inventoryService, int branchId, Product? product = null)
        {
            _inventoryService = inventoryService;
            _currentBranchId = branchId;
            
            if (product != null)
            {
                _product = product;
                Title = "Detalles del Producto";
                IsReadOnlyView = true;
                
                // Copy properties
                Barcode = _product.Barcode;
                Name = _product.Name;
                CategoryId = _product.CategoryId;
                UnitId = _product.UnitId;
                Presentation = _product.Presentation;
                Iva = _product.Iva.ToString("0.##");
                PriceRetail = _product.PriceRetail.ToString("0.##");
                PriceWholesale = _product.PriceWholesale.ToString("0.##");
                WholesaleQuantity = _product.WholesaleQuantity.ToString();
                PriceSpecial = _product.PriceSpecial.ToString("0.##");
                PriceDealer = _product.PriceDealer.ToString("0.##");
                Active = _product.Active;
            }
            else
            {
                _product = new Product();
                Title = "Nuevo Producto";
            }
            
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Load Categories and Units
            var categories = await _inventoryService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);

            var units = await _inventoryService.GetUnitsAsync();
            Units.Clear();
            foreach (var u in units) Units.Add(u);
        }

        [RelayCommand]
        private async Task AddToListAsync()
        {
            if (!await ValidateCurrentProductAsync()) return;

            var p = CreateProductFromForm();
            PendingProducts.Add(p);
            ClearForm();
            StatusMessage = $"Añadido a la lista de pendientes ({PendingProducts.Count} productos).";
        }

        private async Task<bool> ValidateCurrentProductAsync()
        {
            if (string.IsNullOrWhiteSpace(Barcode) || string.IsNullOrWhiteSpace(Name))
            {
                StatusMessage = "Código de barras y Nombre son requeridos.";
                return false;
            }

            if (CategoryId == 0 || UnitId == 0)
            {
                StatusMessage = "Seleccione Categoría y Universo.";
                return false;
            }

            if (!decimal.TryParse(PriceRetail, out _) || !decimal.TryParse(PriceWholesale, out _) || !decimal.TryParse(PriceSpecial, out _) || !decimal.TryParse(PriceDealer, out _) || !int.TryParse(WholesaleQuantity, out _) || !int.TryParse(InitialQuantity, out _))
            {
                StatusMessage = "Los precios y cantidades deben ser números válidos y sin caracteres especiales.";
                return false;
            }

            // Mismo código en la DB
            bool isUnique = await _inventoryService.IsBarcodeUniqueAsync(Barcode, _product.Id);
            if (!isUnique)
            {
                StatusMessage = "Ya existe un producto en base de datos con el mismo código de barras.";
                return false;
            }

            // Validar que el código no exista en la lista actual de pendientes
            foreach (var existing in PendingProducts)
            {
                if (existing.Barcode == Barcode)
                {
                    StatusMessage = "Este código ya está en la lista de pendientes.";
                    return false;
                }
            }

            return true;
        }

        private Product CreateProductFromForm()
        {
            return new Product
            {
                Id = _product.Id,
                Barcode = Barcode,
                Name = Name.ToUpper(),
                CategoryId = CategoryId,
                UnitId = UnitId,
                Presentation = Presentation,
                Iva = 16.0m, // IVA FIJO 16%
                PriceRetail = decimal.TryParse(PriceRetail, out var pr) ? pr : 0,
                PriceWholesale = decimal.TryParse(PriceWholesale, out var pw) ? pw : 0,
                WholesaleQuantity = int.TryParse(WholesaleQuantity, out var wq) ? wq : 1,
                PriceSpecial = decimal.TryParse(PriceSpecial, out var ps) ? ps : 0,
                PriceDealer = decimal.TryParse(PriceDealer, out var pd) ? pd : 0,
                Active = true // SIEMPRE ACTIVO AUNQUE SE ELIMINE SEGÚN REQUERIMIENTOS
            };
        }

        private void ClearForm()
        {
            Barcode = string.Empty;
            Name = string.Empty;
            Presentation = string.Empty;
            PriceRetail = "0";
            PriceWholesale = "0";
            WholesaleQuantity = "1";
            PriceSpecial = "0";
            PriceDealer = "0";
            InitialQuantity = "0";
            // Opcional: limpiar catego y medida, o dejarlos para que se hagan capturas más rápidas
        }

        [RelayCommand]
        private void RemoveFromList(Product product)
        {
            if (product != null)
            {
                PendingProducts.Remove(product);
                StatusMessage = $"Producto removido. ({PendingProducts.Count} productos).";
            }
        }

        [RelayCommand]
        private void Save()
        {
            StartSaveConfirmation?.Invoke(this, EventArgs.Empty);
        }

        public async Task ConfirmSaveAsync()
        {
            try
            {
                if (IsMultipleMode)
                {
                    if (PendingProducts.Count == 0)
                    {
                        StatusMessage = "No hay productos en la lista para agregar.";
                        return;
                    }

                    // Guardar todos los pendientes
                    foreach (var pending in PendingProducts)
                    {
                        int newId = await _inventoryService.SaveProductAsync(pending);
                        if (int.TryParse(InitialQuantity, out var qty) && qty > 0)
                        {
                            await _inventoryService.SetProductStockAsync(newId > 0 ? newId : pending.Id, _currentBranchId, qty);
                        }
                    }
                    StatusMessage = $"{PendingProducts.Count} productos guardados.";
                    PendingProducts.Clear();
                    SaveCompleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    if (!await ValidateCurrentProductAsync()) return;

                    var singleProduct = CreateProductFromForm();
                    int savedId = await _inventoryService.SaveProductAsync(singleProduct);
                    if (int.TryParse(InitialQuantity, out var qty) && qty > 0)
                    {
                        await _inventoryService.SetProductStockAsync(savedId, _currentBranchId, qty);
                    }

                    StatusMessage = "Guardado correctamente.";
                    SaveCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al guardar: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

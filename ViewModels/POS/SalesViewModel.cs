using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using PaymentMethodEnum = CasaCejaRemake.Models.PaymentMethod; // Keep for backward compatibility

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class SalesViewModel : ViewModelBase
    {
        private readonly CartService _cartService;
        private readonly SalesService _salesService;
        private readonly AuthService _authService;
        private readonly int _branchId;

        public int BranchId => _branchId;

        [ObservableProperty]
        private string _barcode = string.Empty;

        [ObservableProperty]
        private char _currentCollection = 'A';

        [ObservableProperty]
        private string _folio = string.Empty;

        [ObservableProperty]
        private string _fechaHora = string.Empty;

        [ObservableProperty]
        private string _branch = string.Empty;

        [ObservableProperty]
        private string _user = string.Empty;

        [ObservableProperty]
        private decimal _total;

        [ObservableProperty]
        private decimal _totalDiscount;

        [ObservableProperty]
        private int _totalItems;

        [ObservableProperty]
        private int _selectedItemIndex = -1;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isProcessing;

        public ObservableCollection<CartItem> Items => _cartService.Items;

        public event EventHandler? RequestFocusBarcode;
        public event EventHandler? RequestShowSearchProduct;
        public event EventHandler? RequestShowPayment;
        public event EventHandler? RequestShowModifyQuantity;
        public event EventHandler? RequestShowCreditsLayaways;
        public event EventHandler<string>? ShowMessage;
        public event EventHandler? RequestExit;
        public event EventHandler<SaleResult>? SaleCompleted;
        public event EventHandler? RequestClearCartConfirmation;
        public event EventHandler? RequestExitConfirmation;
        public event EventHandler? CollectionIndicatorsChanged;

        public SalesViewModel(
            CartService cartService,
            SalesService salesService,
            AuthService authService,
            int branchId,
            string branchName)
        {
            _cartService = cartService;
            _salesService = salesService;
            _authService = authService;
            _branchId = branchId;

            Branch = branchName;
            User = authService.CurrentUser?.Name ?? "Usuario";

            // Suscribirse a cambios del carrito
            _cartService.CartChanged += OnCartChanged;
            _cartService.CollectionChanged += OnCollectionChanged;

            // Actualizar reloj
            UpdateDateTime();

            // Generar folio temporal
            GenerateTemporaryFolio();
        }

        private void OnCartChanged(object? sender, EventArgs e)
        {
            UpdateTotals();
            CollectionIndicatorsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnCollectionChanged(object? sender, char e)
        {
            CurrentCollection = e;
            OnPropertyChanged(nameof(Items));
            UpdateTotals();
            CollectionIndicatorsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTotals()
        {
            Total = _cartService.Total;
            TotalDiscount = _cartService.TotalDiscount;
            TotalItems = _cartService.TotalItems;
        }

        private void UpdateDateTime()
        {
            FechaHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void GenerateTemporaryFolio()
        {
            var now = DateTime.Now;
            Folio = $"{now:MMddyyyy}{_branchId:D2}----";
        }

        /// <summary>
        /// F1 - Enfocar campo de codigo de barras.
        /// </summary>
        [RelayCommand]
        private void FocusBarcode()
        {
            RequestFocusBarcode?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task SearchByCode()
        {
            if (string.IsNullOrWhiteSpace(Barcode))
            {
                return;
            }

            IsProcessing = true;
            StatusMessage = "Buscando producto...";

            try
            {
                var product = await _salesService.GetProductByCodeAsync(Barcode);

                if (product == null)
                {
                    ShowMessage?.Invoke(this, $"Producto no encontrado: {Barcode}");
                    Barcode = string.Empty;
                    return;
                }

                // TODO: Implement stock validation from inventory table
                // if (product.Stock <= 0)
                // {
                //     ShowMessage?.Invoke(this, $"Sin existencias: {product.Name}");
                //     Barcode = string.Empty;
                //     return;
                // }

                // Crear item de carrito con precio calculado
                var cartItem = await _salesService.CreateCartItemAsync(
                    product.Id, 
                    1, 
                    _authService.CurrentUser?.Id ?? 0);

                if (cartItem != null)
                {
                    _cartService.AddProduct(cartItem);
                    StatusMessage = $"Agregado: {product.Name}";
                }

                Barcode = string.Empty;
            }
            catch (Exception ex)
            {
                ShowMessage?.Invoke(this, $"Error: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void ModifyQuantity()
        {
            if (SelectedItemIndex < 0 || SelectedItemIndex >= Items.Count)
            {
                ShowMessage?.Invoke(this, "Seleccione un producto para modificar.");
                return;
            }

            RequestShowModifyQuantity?.Invoke(this, EventArgs.Empty);
        }

        public async Task ApplyNewQuantityAsync(int newQuantity)
        {
            if (SelectedItemIndex < 0 || SelectedItemIndex >= Items.Count) return;

            var item = Items[SelectedItemIndex];

            // Validar stock
            var product = await _salesService.GetProductByCodeAsync(item.Barcode);
            // TODO: Validar stock desde tabla de inventario
            // if (product != null && newQuantity > product.Stock)
            // {
            //     ShowMessage?.Invoke(this, $"Stock insuficiente. Disponible: {product.Stock}");
            //     return;
            // }

            // Recalcular precio si aplica mayoreo
            if (newQuantity != item.Quantity && product != null)
            {
                var newItem = await _salesService.CreateCartItemAsync(
                    product.Id, 
                    newQuantity, 
                    _authService.CurrentUser?.Id ?? 0);

                if (newItem != null)
                {
                    item.Quantity = newItem.Quantity;
                    item.FinalUnitPrice = newItem.FinalUnitPrice;
                    item.TotalDiscount = newItem.TotalDiscount;
                    item.PriceType = newItem.PriceType;
                    item.DiscountInfo = newItem.DiscountInfo;
                }
            }

            _cartService.ModifyQuantityByIndex(SelectedItemIndex, newQuantity);
        }

        [RelayCommand]
        private void SearchProduct()
        {
            RequestShowSearchProduct?.Invoke(this, EventArgs.Empty);
        }

        public async Task AddProductFromSearchAsync(Product product, int quantity)
        {
            if (product == null) return;

            // TODO: Implement stock validation from inventory table
            // if (product.Stock < quantity)
            // {
            //     ShowMessage?.Invoke(this, $"Stock insuficiente. Disponible: {product.Stock}");
            //     return;
            // }

            var cartItem = await _salesService.CreateCartItemAsync(
                product.Id, 
                quantity, 
                _authService.CurrentUser?.Id ?? 0);

            if (cartItem != null)
            {
                _cartService.AddProduct(cartItem);
                StatusMessage = $"Agregado: {product.Name} x {quantity}";
            }
        }

        [RelayCommand]
        private void ChangeCollection()
        {
            _cartService.ChangeCollection();
            StatusMessage = $"Cobranza {CurrentCollection}";
        }

        /// <summary>
        /// F5 - Retroceder a la cobranza anterior
        /// </summary>
        [RelayCommand]
        private void ChangeCollectionPrevious()
        {
            _cartService.ChangeCollectionPrevious();
            StatusMessage = $"Cobranza {CurrentCollection}";
        }

        /// <summary>
        /// Cambiar a una cobranza específica
        /// </summary>
        [RelayCommand]
        private void SelectCollection(char identifier)
        {
            _cartService.ChangeCollection(identifier);
            StatusMessage = $"Cobranza {CurrentCollection}";
        }

        /// <summary>
        /// Obtiene información de todas las cobranzas para mostrar en la UI
        /// </summary>
        public List<(char Id, int Items, decimal Total, bool IsActive)> GetAllCollectionsInfo()
        {
            return _cartService.GetAllCollectionsInfo();
        }

        /// <summary>
        /// Verifica si una cobranza específica tiene items
        /// </summary>
        public bool CollectionHasItems(char identifier)
        {
            return _cartService.CollectionHasItems(identifier);
        }

        [RelayCommand]
        private void RemoveProduct()
        {
            if (SelectedItemIndex < 0 || SelectedItemIndex >= Items.Count)
            {
                ShowMessage?.Invoke(this, "Seleccione un producto para quitar.");
                return;
            }

            var item = Items[SelectedItemIndex];
            _cartService.RemoveProductByIndex(SelectedItemIndex);
            StatusMessage = $"Eliminado: {item.ProductName}";
            SelectedItemIndex = -1;
        }

        [RelayCommand]
        private void ClearCart()
        {
            if (_cartService.IsEmpty)
            {
                ShowMessage?.Invoke(this, "El carrito ya esta vacio.");
                return;
            }

            RequestClearCartConfirmation?.Invoke(this, EventArgs.Empty);
        }

        public void ConfirmClearCart()
        {
            _cartService.ClearCart();
            StatusMessage = "Carrito vaciado";
        }

        [RelayCommand]
        private void Pay()
        {
            if (_cartService.IsEmpty)
            {
                ShowMessage?.Invoke(this, "No hay productos en el carrito.");
                return;
            }

            RequestShowPayment?.Invoke(this, EventArgs.Empty);
        }

        public async Task<SaleResult> ProcessPaymentAsync(string paymentJson, decimal totalPaid, decimal changeGiven)
        {
            IsProcessing = true;
            StatusMessage = "Procesando venta...";

            try
            {
                var result = await _salesService.ProcessSaleWithMixedPaymentAsync(
                    _cartService.GetItemsForSale(),
                    paymentJson,
                    totalPaid,
                    changeGiven,
                    _authService.CurrentUser?.Id ?? 0,
                    _authService.CurrentUser?.Name ?? "Usuario",
                    _branchId);

                if (result.Success)
                {
                    // Limpiar carrito
                    _cartService.ClearCart();
                    StatusMessage = $"Venta completada: {result.Sale?.Folio}";
                    
                    // Generar nuevo folio temporal
                    GenerateTemporaryFolio();
                    
                    SaleCompleted?.Invoke(this, result);
                }
                else
                {
                    ShowMessage?.Invoke(this, result.ErrorMessage ?? "Error desconocido");
                }

                return result;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void CreditsLayaways()
        {
            RequestShowCreditsLayaways?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Exit()
        {
            if (_cartService.HasPendingCollections())
            {
                ShowMessage?.Invoke(this, "Hay cobranzas pendientes. Vacia los carritos antes de salir.");
                return;
            }

            RequestExitConfirmation?.Invoke(this, EventArgs.Empty);
        }

        public void ConfirmExit()
        {
            RequestExit?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateClock()
        {
            UpdateDateTime();
        }

        public void Cleanup()
        {
            _cartService.CartChanged -= OnCartChanged;
            _cartService.CollectionChanged -= OnCollectionChanged;
        }
    }
}
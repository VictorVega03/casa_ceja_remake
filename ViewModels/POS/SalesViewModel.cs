using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.Results;
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
        private string _cashCloseFolio = string.Empty;

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

        /// <summary>
        /// Item seleccionado actualmente en el DataGrid.
        /// Importante usar esta propiedad en lugar del índice cuando el DataGrid puede estar ordenado.
        /// </summary>
        [ObservableProperty]
        private CartItem? _selectedItem;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isProcessing;

        // Propiedades de descuento general
        [ObservableProperty]
        private decimal _generalDiscountPercent;

        [ObservableProperty]
        private decimal _calculatedGeneralDiscount;

        [ObservableProperty]
        private decimal _finalTotal;

        [ObservableProperty]
        private bool _hasGeneralDiscount;

        /// <summary>
        /// Descuento total para display: suma de descuentos de items + descuento general
        /// </summary>
        [ObservableProperty]
        private decimal _totalDiscountDisplay;

        public ObservableCollection<CartItem> Items => _cartService.Items;

        public event EventHandler? RequestFocusBarcode;
        public event EventHandler? RequestShowSearchProduct;
        public event EventHandler? RequestShowPayment;
        public event EventHandler? RequestShowModifyQuantity;
        public event EventHandler? RequestShowCreditsLayaways;
        public event EventHandler<string>? ShowMessage;
        public event EventHandler? RequestExit;
        public event EventHandler? RequestLogout;
        public event EventHandler<SaleResult>? SaleCompleted;
        public event EventHandler? RequestClearCartConfirmation;
        public event EventHandler? RequestExitConfirmation;
        public event EventHandler? RequestLogoutConfirmation;
        public event EventHandler? CollectionIndicatorsChanged;
        public event EventHandler? ProductAddedToCart;
        
        // Eventos para descuentos
        public event EventHandler<string>? ShowDiscountApplied;     // Muestra diálogo de confirmación de descuento
        public event EventHandler<string>? ShowDiscountBlocked;     // Muestra diálogo de bloqueo de descuento
        public event EventHandler? RequestShowGeneralDiscount;       // Muestra diálogo de descuento general (F6)
        public event EventHandler? RequestAdminVerification;         // Solicita verificación de administrador

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

            // Cargar folio del corte actual
            LoadCashCloseFolio();
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
            
            // Propiedades de descuento general
            GeneralDiscountPercent = _cartService.GeneralDiscountPercent;
            CalculatedGeneralDiscount = _cartService.CalculatedGeneralDiscount;
            FinalTotal = _cartService.FinalTotal;
            HasGeneralDiscount = _cartService.HasGeneralDiscount;
            
            // Descuento total para display (items + general)
            TotalDiscountDisplay = TotalDiscount + CalculatedGeneralDiscount;
        }

        private void UpdateDateTime()
        {
            FechaHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private async void LoadCashCloseFolio()
        {
            try
            {
                var cashCloseService = (Avalonia.Application.Current as App)?.GetCashCloseService();
                if (cashCloseService == null) return;
                var openCash = await cashCloseService.GetOpenCashAsync(_branchId);
                
                if (openCash != null)
                {
                    CashCloseFolio = openCash.Folio;
                }
                else
                {
                    CashCloseFolio = "SIN CAJA";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesViewModel] Error cargando folio: {ex.Message}");
                CashCloseFolio = "ERROR";
            }
        }

        /// <summary>
        /// Recarga el folio del corte actual. Usar después de abrir/cerrar caja.
        /// </summary>
        public void RefreshCashCloseFolio()
        {
            LoadCashCloseFolio();
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

                // Verificar si el producto ya existe en el carrito
                var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
                
                if (existingItem != null)
                {
                    // Producto ya existe: recalcular con cantidad total para que mayoreo aplique
                    var newTotalQuantity = existingItem.Quantity + 1;
                    var oldPriceType = existingItem.PriceType;
                    var oldDiscountInfo = existingItem.DiscountInfo;
                    
                    // Si tiene precio aislado (special/dealer), solo incrementar cantidad
                    if (existingItem.PriceType == "special" || existingItem.PriceType == "dealer")
                    {
                        existingItem.Quantity = newTotalQuantity;
                        _cartService.NotifyCartChanged();
                    }
                    else
                    {
                        // Recalcular precio con la cantidad total
                        var recalcItem = await _salesService.CreateCartItemAsync(
                            product.Id, newTotalQuantity, _authService.CurrentUser?.Id ?? 0);
                        
                        if (recalcItem != null)
                        {
                            existingItem.Quantity = recalcItem.Quantity;
                            existingItem.FinalUnitPrice = recalcItem.FinalUnitPrice;
                            existingItem.TotalDiscount = recalcItem.TotalDiscount;
                            existingItem.PriceType = recalcItem.PriceType;
                            existingItem.DiscountInfo = recalcItem.DiscountInfo;
                            _cartService.NotifyCartChanged();
                            
                            // Detectar cambio a/desde mayoreo
                            bool oldHasWholesale = oldPriceType == "wholesale" || 
                                                  (oldDiscountInfo?.Contains("Mayoreo") == true);
                            bool newHasWholesale = recalcItem.PriceType == "wholesale" || 
                                                  (recalcItem.DiscountInfo?.Contains("Mayoreo") == true);
                            
                            if (!oldHasWholesale && newHasWholesale)
                                NotifyWholesalePrice(recalcItem);
                            else if (oldHasWholesale && !newHasWholesale)
                            {
                                var message = $"✓ Producto salió de Precio de Mayoreo: \"{recalcItem.ProductName}\"\n\n" +
                                    $"Precio actual: ${recalcItem.FinalUnitPrice:N2}";
                                ShowDiscountApplied?.Invoke(this, message);
                            }
                        }
                    }
                    
                    StatusMessage = $"Agregado: {product.Name}";
                    ProductAddedToCart?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Producto nuevo: crear y agregar normalmente
                    var cartItem = await _salesService.CreateCartItemAsync(
                        product.Id, 1, _authService.CurrentUser?.Id ?? 0);

                    if (cartItem != null)
                    {
                        _cartService.AddProduct(cartItem);
                        StatusMessage = $"Agregado: {product.Name}";
                        ProductAddedToCart?.Invoke(this, EventArgs.Empty);
                        
                        bool hasWholesale = cartItem.PriceType == "wholesale" || 
                                           (cartItem.DiscountInfo?.Contains("Mayoreo") == true);
                        
                        if (hasWholesale)
                            NotifyWholesalePrice(cartItem);
                        else if (cartItem.TotalDiscount > 0 && cartItem.PriceType == "category")
                            NotifyCategoryDiscount(cartItem);
                    }
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

            // Recalcular precio si aplica mayoreo
            if (newQuantity != item.Quantity && product != null)
            {
                // Si tiene precio aislado (special/dealer), solo cambiar cantidad sin recalcular
                if (item.PriceType == "special" || item.PriceType == "dealer")
                {
                    // No recalcular: el precio aislado se mantiene
                }
                else
                {
                    var oldPriceType = item.PriceType;
                    var oldDiscountInfo = item.DiscountInfo;
                    
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
                        
                        // Detectar si el nuevo item tiene mayoreo
                        bool oldHasWholesale = oldPriceType == "wholesale" || 
                                              (oldDiscountInfo?.Contains("Mayoreo") == true);
                        bool newHasWholesale = newItem.PriceType == "wholesale" || 
                                              (newItem.DiscountInfo?.Contains("Mayoreo") == true);
                        
                        if (!oldHasWholesale && newHasWholesale)
                            NotifyWholesalePrice(newItem);
                        else if (oldHasWholesale && !newHasWholesale)
                        {
                            var message = $"✓ Producto salió de Precio de Mayoreo: \"{newItem.ProductName}\"\n\n" +
                                $"Precio mayoreo: ${item.ListPrice - item.TotalDiscount:N2}\n" +
                                $"Precio actual: ${newItem.FinalUnitPrice:N2}";
                            ShowDiscountApplied?.Invoke(this, message);
                        }
                    }
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

            // Verificar si el producto ya existe en el carrito
            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
            
            if (existingItem != null)
            {
                // Producto ya existe: recalcular con cantidad total
                var newTotalQuantity = existingItem.Quantity + quantity;
                
                // Si tiene precio aislado (special/dealer), solo incrementar cantidad
                if (existingItem.PriceType == "special" || existingItem.PriceType == "dealer")
                {
                    existingItem.Quantity = newTotalQuantity;
                    _cartService.NotifyCartChanged();
                }
                else
                {
                    var recalcItem = await _salesService.CreateCartItemAsync(
                        product.Id, newTotalQuantity, _authService.CurrentUser?.Id ?? 0);
                    
                    if (recalcItem != null)
                    {
                        existingItem.Quantity = recalcItem.Quantity;
                        existingItem.FinalUnitPrice = recalcItem.FinalUnitPrice;
                        existingItem.TotalDiscount = recalcItem.TotalDiscount;
                        existingItem.PriceType = recalcItem.PriceType;
                        existingItem.DiscountInfo = recalcItem.DiscountInfo;
                        _cartService.NotifyCartChanged();
                    }
                }
                
                StatusMessage = $"Agregado: {product.Name} x {quantity}";
                ProductAddedToCart?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var cartItem = await _salesService.CreateCartItemAsync(
                    product.Id, quantity, _authService.CurrentUser?.Id ?? 0);

                if (cartItem != null)
                {
                    _cartService.AddProduct(cartItem);
                    StatusMessage = $"Agregado: {product.Name} x {quantity}";
                    ProductAddedToCart?.Invoke(this, EventArgs.Empty);
                    
                    if (cartItem.TotalDiscount > 0)
                        NotifyCategoryDiscount(cartItem);
                }
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
                    _branchId,
                    _cartService.CalculatedGeneralDiscount,
                    _cartService.GeneralDiscountPercent,
                    _cartService.IsGeneralDiscountPercentage);

                if (result.Success)
                {
                    // Limpiar carrito
                    _cartService.ClearCart();
                    StatusMessage = $"Venta completada: {result.Sale?.Folio}";
                    
                    // Recargar folio del corte (por si acaso)
                    LoadCashCloseFolio();
                    
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
            RequestExitConfirmation?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Logout()
        {
            RequestLogoutConfirmation?.Invoke(this, EventArgs.Empty);
        }

        public void ConfirmExit()
        {
            RequestExit?.Invoke(this, EventArgs.Empty);
        }

        public void ConfirmLogout()
        {
            RequestLogout?.Invoke(this, EventArgs.Empty);
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

        // ========== COMANDOS DE DESCUENTOS (F2, F3, F6) ==========

        /// <summary>
        /// Ctrl+F2 - Aplicar/Quitar precio especial al producto seleccionado.
        /// El precio especial es AISLADO: no se combina con otros descuentos.
        /// Si el producto ya tiene precio especial, se revierte al precio original.
        /// </summary>
        [RelayCommand]
        private async Task ApplySpecialPrice()
        {
            // Usar SelectedItem en lugar del índice para evitar bugs cuando el DataGrid está ordenado
            if (SelectedItem == null)
            {
                ShowMessage?.Invoke(this, "Seleccione un producto para aplicar/quitar precio especial.");
                return;
            }

            var item = SelectedItem;
            
            // Toggle: si ya tiene precio especial, revertir
            if (item.PriceType == "special")
            {
                var revertResult = await _salesService.RevertToRetailPriceAsync(item);
                if (revertResult.Success)
                {
                    _cartService.NotifyCartChanged();
                    StatusMessage = $"Precio especial removido: {item.ProductName}";
                    ShowDiscountApplied?.Invoke(this, revertResult.Message);
                }
                else
                {
                    ShowDiscountBlocked?.Invoke(this, revertResult.Message);
                }
                return;
            }
            
            // Aplicar precio especial
            var result = await _salesService.ApplySpecialPriceAsync(item);
            
            if (result.Success)
            {
                _cartService.NotifyCartChanged();
                StatusMessage = $"Precio especial aplicado: {item.ProductName}";
                ShowDiscountApplied?.Invoke(this, result.Message);
            }
            else
            {
                ShowDiscountBlocked?.Invoke(this, result.Message);
            }
        }

        /// <summary>
        /// Ctrl+F3 - Aplicar/Quitar precio vendedor al producto seleccionado.
        /// El precio vendedor es AISLADO: no se combina con otros descuentos.
        /// Si el producto ya tiene precio vendedor, se revierte al precio original.
        /// </summary>
        [RelayCommand]
        private async Task ApplyDealerPrice()
        {
            // Usar SelectedItem en lugar del índice para evitar bugs cuando el DataGrid está ordenado
            if (SelectedItem == null)
            {
                ShowMessage?.Invoke(this, "Seleccione un producto para aplicar/quitar precio vendedor.");
                return;
            }

            var item = SelectedItem;
            
            // Toggle: si ya tiene precio vendedor, revertir
            if (item.PriceType == "dealer")
            {
                var revertResult = await _salesService.RevertToRetailPriceAsync(item);
                if (revertResult.Success)
                {
                    _cartService.NotifyCartChanged();
                    StatusMessage = $"Precio vendedor removido: {item.ProductName}";
                    ShowDiscountApplied?.Invoke(this, revertResult.Message);
                }
                else
                {
                    ShowDiscountBlocked?.Invoke(this, revertResult.Message);
                }
                return;
            }
            
            // Aplicar precio vendedor
            var result = await _salesService.ApplyDealerPriceAsync(item);
            
            if (result.Success)
            {
                _cartService.NotifyCartChanged();
                StatusMessage = $"Precio vendedor aplicado: {item.ProductName}";
                ShowDiscountApplied?.Invoke(this, result.Message);
            }
            else
            {
                ShowDiscountBlocked?.Invoke(this, result.Message);
            }
        }

        /// <summary>
        /// F6 - Mostrar diálogo para aplicar descuento general sobre la venta
        /// </summary>
        [RelayCommand]
        private void ShowGeneralDiscount()
        {
            if (_cartService.IsEmpty)
            {
                ShowMessage?.Invoke(this, "No hay productos en el carrito.");
                return;
            }

            // La solicitud de Descuento General siempre exige autorización del Administrador en Pantalla, 
            // incluso si el usuario actual ya posee el rol (por motivos de auditoria y seguridad).
            RequestAdminVerification?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Aplica un descuento general sobre el total de la venta.
        /// Llamado desde el diálogo de descuento general.
        /// </summary>
        /// <param name="value">Valor del descuento</param>
        /// <param name="isPercentage">true = porcentaje, false = monto fijo</param>
        public void ApplyGeneralDiscountValue(decimal value, bool isPercentage)
        {
            _cartService.ApplyGeneralDiscount(value, isPercentage);
            StatusMessage = isPercentage 
                ? $"Descuento general {value}% aplicado"
                : $"Descuento general ${value:N2} aplicado";
        }

        /// <summary>
        /// Limpia el descuento general
        /// </summary>
        public void ClearGeneralDiscount()
        {
            _cartService.ClearGeneralDiscount();
            StatusMessage = "Descuento general eliminado";
        }

        /// <summary>
        /// Notifica cuando se aplica un descuento de categoría automáticamente.
        /// Llamado después de agregar un producto con descuento de categoría.
        /// </summary>
        public void NotifyCategoryDiscount(CartItem item)
        {
            if (!string.IsNullOrEmpty(item.DiscountInfo) && item.TotalDiscount > 0)
            {
                var message = $"✓ Descuento aplicado a \"{item.ProductName}\"\n\n" +
                    $"• {item.DiscountInfo}\n\n" +
                    $"Precio original: ${item.ListPrice:N2}\n" +
                    $"Precio final: ${item.FinalUnitPrice:N2}\n" +
                    $"Ahorro: ${item.TotalDiscount:N2} por unidad";
                
                ShowDiscountApplied?.Invoke(this, message);
            }
        }
        
        public void NotifyWholesalePrice(CartItem item)
        {
            if (!string.IsNullOrEmpty(item.DiscountInfo))
            {
                var message = $"✓ Precio de Mayoreo aplicado a \"{item.ProductName}\"\n\n" +
                    $"• {item.DiscountInfo}\n\n" +
                    $"Precio menudeo: ${item.ListPrice:N2}\n" +
                    $"Precio mayoreo: ${item.FinalUnitPrice:N2}\n" +
                    $"Ahorro: ${item.TotalDiscount:N2} por unidad";
                
                ShowDiscountApplied?.Invoke(this, message);
            }
        }
    }
}
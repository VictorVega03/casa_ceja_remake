using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Inventory
{
    /// <summary>
    /// Línea de producto dentro de la salida en edición.
    /// </summary>
    public partial class OutputLineItem : ObservableObject
    {
        public int ProductId { get; init; }
        public string Barcode { get; init; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineTotal))]
        private string _productName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineTotal))]
        private int _quantity = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineTotal))]
        private decimal _unitCost;

        public int CurrentStock { get; set; }

        public decimal LineTotal => Quantity * UnitCost;
    }

    public partial class OutputsViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly FolioService _folioService;
        private readonly ApiClient _apiClient;
        private readonly int _branchId;
        private readonly int _userId;
        private string _userName = string.Empty;

        // ── Eventos de navegación ──────────────────────────────────────────
        public event EventHandler? GoBackRequested;
        public event EventHandler<string>? ShowMessageRequested;
        public event EventHandler<(string Message, OutputRemissionData PdfData)>? ShowSuccessRequested;
        public event EventHandler<string>? ShowErrorRequested;
        public event EventHandler<string>? OpenPosCatalogRequested;
        public event EventHandler<OutputLineItem>? ProductAddedOrUpdated;
        public event EventHandler<(string DestinationName, int ProductCount, decimal TotalAmount)>? RequestConfirmSave;

        // ── Colecciones ───────────────────────────────────────────────────
        public ObservableCollection<Branch> Branches { get; } = new();
        public ObservableCollection<OutputLineItem> Lines { get; } = new();

        // ── Propiedades del encabezado ────────────────────────────────────
        [ObservableProperty] private string _branchName = string.Empty;
        [ObservableProperty] private DateTimeOffset? _outputDate = DateTimeOffset.Now;
        [ObservableProperty] private Branch? _selectedDestination;
        [ObservableProperty] private string _notes = string.Empty;

        // ── Búsqueda ─────────────────────────────────────────────────────
        [ObservableProperty] private string _searchTerm = string.Empty;
        [ObservableProperty] private string _searchError = string.Empty;
        [ObservableProperty] private bool _isSearching;

        // ── Estado ───────────────────────────────────────────────────────
        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private decimal _totalAmount;
        [ObservableProperty] private OutputLineItem? _selectedLine;

        public bool HasLines => Lines.Count > 0;

        public OutputsViewModel(
            InventoryService inventoryService,
            FolioService folioService,
            ApiClient apiClient,
            int branchId,
            string branchName,
            int userId,
            string userName = "")
        {
            _inventoryService = inventoryService;
            _folioService = folioService;
            _apiClient = apiClient;
            _branchId = branchId;
            _userId = userId;
            _userName = userName;
            BranchName = branchName;

            Lines.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (OutputLineItem item in e.NewItems)
                        item.PropertyChanged += OnLineItemChanged;

                if (e.OldItems != null)
                    foreach (OutputLineItem item in e.OldItems)
                        item.PropertyChanged -= OnLineItemChanged;

                UpdateTotal();
                OnPropertyChanged(nameof(HasLines));
            };

            _ = LoadBranchesAsync();
        }

        private void OnLineItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OutputLineItem.LineTotal))
                UpdateTotal();
        }

        private void UpdateTotal()
        {
            TotalAmount = Lines.Sum(l => l.LineTotal);
        }

        private async Task LoadBranchesAsync()
        {
            try
            {
                var list = await _inventoryService.GetBranchesAsync();
                Branches.Clear();
                foreach (var b in list.Where(b => b.Id != _branchId).OrderBy(b => b.Name))
                    Branches.Add(b);
                if (Branches.Count > 0)
                    SelectedDestination = null;
            }
            catch (Exception ex)
            {
                Branches.Clear();
                SelectedDestination = null;
                ShowMessageRequested?.Invoke(this, $"Modo sin conexión: no se pudo cargar sucursales destino ({ex.Message}).");
            }
        }

        // ── Búsqueda ──────────────────────────────────────────────────────

        [RelayCommand]
        private async Task SearchProductAsync()
        {
            var term = SearchTerm?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(term)) return;

            IsSearching = true;
            SearchError = string.Empty;

            try
            {
                var results = await _inventoryService.SearchProductsAsync(term);

                if (results.Count == 0)
                {
                    SearchError = $"No se encontró ningún producto con: \"{term}\"";
                    OpenPosCatalogRequested?.Invoke(this, term);
                    return;
                }

                var exact = results.FirstOrDefault(p =>
                    p.Barcode.Equals(term, StringComparison.OrdinalIgnoreCase));
                var product = exact ?? results[0];

                var (stockItems, _) = await _inventoryService.GetStockByProductAsync(product.Id, product.Barcode);
                var stock = stockItems.FirstOrDefault(s => s.BranchId == _branchId)?.Quantity ?? 0;

                AddOrIncrementLine(product, stock);
                SearchTerm = string.Empty;
            }
            catch (Exception ex)
            {
                SearchError = $"Error al buscar: {ex.Message}";
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void AddOrIncrementLine(Product product, int stock)
        {
            var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
                SelectedLine = existing;
                ProductAddedOrUpdated?.Invoke(this, existing);
            }
            else
            {
                var newLine = new OutputLineItem
                {
                    ProductId = product.Id,
                    Barcode = product.Barcode,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitCost = product.PriceRetail,
                    CurrentStock = stock
                };

                Lines.Add(newLine);
                SelectedLine = newLine;
                ProductAddedOrUpdated?.Invoke(this, newLine);
            }
        }

        [RelayCommand]
        private void OpenCatalog()
        {
            OpenPosCatalogRequested?.Invoke(this, SearchTerm?.Trim() ?? string.Empty);
        }

        public async Task AddProductFromPosCatalogAsync(Product product, int quantity)
        {
            var (stockItems, _) = await _inventoryService.GetStockByProductAsync(product.Id, product.Barcode);
            var stock = stockItems.FirstOrDefault(s => s.BranchId == _branchId)?.Quantity ?? 0;
            var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);

            if (existing != null)
            {
                existing.Quantity += Math.Max(1, quantity);
                SelectedLine = existing;
                ProductAddedOrUpdated?.Invoke(this, existing);
                return;
            }

            var newLine = new OutputLineItem
            {
                ProductId = product.Id,
                Barcode = product.Barcode,
                ProductName = product.Name,
                Quantity = Math.Max(1, quantity),
                UnitCost = product.PriceRetail,
                CurrentStock = stock
            };

            Lines.Add(newLine);
            SelectedLine = newLine;
            ProductAddedOrUpdated?.Invoke(this, newLine);
        }

        // ── Comandos de líneas ────────────────────────────────────────────

        [RelayCommand]
        private void RemoveLine(OutputLineItem? line)
        {
            if (line != null)
                Lines.Remove(line);
        }

        [RelayCommand]
        private void IncrementQty(OutputLineItem? line)
        {
            if (line == null) return;
            line.Quantity++;
        }

        [RelayCommand]
        private void DecrementQty(OutputLineItem? line)
        {
            if (line != null && line.Quantity > 1) line.Quantity--;
        }

        // ── Guardar ───────────────────────────────────────────────────────

        [RelayCommand]
        private void SaveOutput()
        {
            if (SelectedDestination == null)
            {
                ShowMessageRequested?.Invoke(this, "Selecciona una sucursal destino antes de guardar.");
                return;
            }

            if (Lines.Count == 0)
            {
                ShowMessageRequested?.Invoke(this, "Agrega al menos un producto a la salida antes de guardar.");
                return;
            }

            RequestConfirmSave?.Invoke(this, (SelectedDestination.Name, Lines.Count, TotalAmount));
        }

        public async Task DoSaveOutputAsync()
        {
            if (SelectedDestination == null || Lines.Count == 0) return;

            IsSaving = true;
            try
            {
                var folio = await _folioService.GenerarFolioSalidaAsync(_branchId);

                // 1. SERVIDOR PRIMERO — el servidor crea la entrada pendiente para la sucursal destino
                var request = new Models.DTOs.StockOutputRequest
                {
                    BranchId = _branchId,
                    Folio = folio,
                    DestinationBranchId = SelectedDestination.Id,
                    UserId = _userId,
                    TotalAmount = TotalAmount,
                    OutputDate = OutputDate?.DateTime ?? DateTime.Now,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                    Products = Lines.Select(l => new Models.DTOs.StockOutputProductRequest
                    {
                        ProductId = l.ProductId,
                        Barcode = l.Barcode,
                        ProductName = l.ProductName,
                        Quantity = l.Quantity,
                        UnitCost = l.UnitCost,
                        LineTotal = l.LineTotal
                    }).ToList()
                };

                var response = await _apiClient.PostAsync<Models.DTOs.FolioResponse>(
                    "/api/v1/inventory/outputs", request);

                if (response?.IsSuccess != true)
                {
                    ShowErrorRequested?.Invoke(this,
                        "No se pudo registrar la salida en el servidor.\n" +
                        "Verifica tu conexión a internet e intenta de nuevo.");
                    return;
                }

                // 2. Servidor aceptó — ahora guardar localmente y descontar stock
                var output = new StockOutput
                {
                    Folio = folio,
                    OriginBranchId = _branchId,
                    DestinationBranchId = SelectedDestination.Id,
                    UserId = _userId,
                    TotalAmount = TotalAmount,
                    OutputDate = OutputDate?.DateTime ?? DateTime.Now,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                    Status = "PENDING",
                    SyncStatus = 2, // Ya sincronizado con servidor
                    LastSync = DateTime.Now
                };

                var products = Lines.Select(l => new OutputProduct
                {
                    ProductId = l.ProductId,
                    Barcode = l.Barcode,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost,
                    LineTotal = l.LineTotal
                }).ToList();

                await _inventoryService.CreateOutputAsync(output, products);

                var pdfData = new OutputRemissionData
                {
                    Folio               = folio,
                    OriginBranchName    = BranchName,
                    DestinationBranchName = SelectedDestination.Name,
                    OutputDate          = output.OutputDate,
                    UserName            = _userName,
                    TotalAmount         = TotalAmount,
                    Notes               = output.Notes,
                    Lines               = Lines.Select(l => new OutputProductInfo
                    {
                        Barcode     = l.Barcode,
                        ProductName = l.ProductName,
                        Quantity    = l.Quantity,
                        UnitCost    = l.UnitCost,
                        LineTotal   = l.LineTotal
                    }).ToList()
                };

                ShowSuccessRequested?.Invoke(this, (
                    $"Folio: {folio}\nStock descontado en esta sucursal.\nLa sucursal destino recibirá la entrada pendiente.",
                    pdfData));
            }
            catch (Exception ex)
            {
                ShowErrorRequested?.Invoke(this,
                    $"No se pudo guardar la salida.\n{ex.Message}\nVerifica tu conexión e intenta de nuevo.");
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

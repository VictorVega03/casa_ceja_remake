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
        private readonly int _branchId;
        private readonly int _userId;

        // ── Eventos de navegación ──────────────────────────────────────────
        public event EventHandler? GoBackRequested;
        public event EventHandler<string>? ShowMessageRequested;

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
            int branchId,
            string branchName,
            int userId)
        {
            _inventoryService = inventoryService;
            _folioService = folioService;
            _branchId = branchId;
            _userId = userId;
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
                    SelectedDestination = Branches[0];
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
                    return;
                }

                var exact = results.FirstOrDefault(p =>
                    p.Barcode.Equals(term, StringComparison.OrdinalIgnoreCase));
                var product = exact ?? results[0];

                var stock = await _inventoryService.GetProductStockAsync(product.Id, _branchId);

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
            }
            else
            {
                Lines.Add(new OutputLineItem
                {
                    ProductId = product.Id,
                    Barcode = product.Barcode,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitCost = 0m,
                    CurrentStock = stock
                });
            }
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
        private async Task SaveOutputAsync()
        {
            if (Lines.Count == 0)
            {
                ShowMessageRequested?.Invoke(this, "Agrega al menos un producto a la salida antes de guardar.");
                return;
            }

            if (SelectedDestination == null)
            {
                ShowMessageRequested?.Invoke(this, "Selecciona una sucursal destino antes de guardar.");
                return;
            }

            IsSaving = true;
            try
            {
                var folio = await _folioService.GenerarFolioSalidaAsync(_branchId);

                var output = new StockOutput
                {
                    Folio = folio,
                    OriginBranchId = _branchId,
                    DestinationBranchId = SelectedDestination.Id,
                    UserId = _userId,
                    TotalAmount = TotalAmount,
                    OutputDate = OutputDate?.DateTime ?? DateTime.Now,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                    Status = "PENDING"
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
                GoBackRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ShowMessageRequested?.Invoke(this, $"Error al guardar la salida: {ex.Message}");
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

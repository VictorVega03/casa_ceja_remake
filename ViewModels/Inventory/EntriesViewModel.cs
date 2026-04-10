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
    /// Representa una línea de producto dentro de la entrada en edición.
    /// </summary>
    public partial class EntryLineItem : ObservableObject
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

        public decimal LineTotal => Quantity * UnitCost;
    }

    public partial class EntriesViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly FolioService _folioService;
        private readonly int _branchId;
        private readonly int _userId;

        // ── Eventos de navegación ──────────────────────────────────────────
        public event EventHandler? GoBackRequested;
        public event EventHandler<string>? ShowMessageRequested;
        public event EventHandler<string>? OpenPosCatalogRequested;
        public event EventHandler<EntryLineItem>? ProductAddedOrUpdated;

        // ── Colecciones ───────────────────────────────────────────────────
        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<EntryLineItem> Lines { get; } = new();

        // ── Propiedades del encabezado ────────────────────────────────────
        [ObservableProperty] private string _branchName = string.Empty;
        [ObservableProperty] private DateTimeOffset? _entryDate = DateTimeOffset.Now;
        [ObservableProperty] private Supplier? _selectedSupplier;
        [ObservableProperty] private string _notes = string.Empty;

        // ── Propiedades de búsqueda ───────────────────────────────────────
        [ObservableProperty] private string _searchTerm = string.Empty;
        [ObservableProperty] private string _searchError = string.Empty;
        [ObservableProperty] private bool _isSearching;

        // ── Estado ───────────────────────────────────────────────────────
        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private decimal _totalAmount;
        [ObservableProperty] private EntryLineItem? _selectedLine;

        public bool HasLines => Lines.Count > 0;

        public EntriesViewModel(
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
                    foreach (EntryLineItem item in e.NewItems)
                        item.PropertyChanged += OnLineItemChanged;

                if (e.OldItems != null)
                    foreach (EntryLineItem item in e.OldItems)
                        item.PropertyChanged -= OnLineItemChanged;

                UpdateTotal();
                OnPropertyChanged(nameof(HasLines));
            };

            _ = LoadSuppliersAsync();
        }

        private void OnLineItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EntryLineItem.LineTotal))
                UpdateTotal();
        }

        private void UpdateTotal()
        {
            TotalAmount = Lines.Sum(l => l.LineTotal);
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var list = await _inventoryService.GetSuppliersAsync();
                Suppliers.Clear();
                foreach (var s in list.OrderBy(x => x.Name))
                    Suppliers.Add(s);

                if (Suppliers.Count > 0)
                {
                    SelectedSupplier = Suppliers[0];
                }
                else
                {
                    SelectedSupplier = null;
                    ShowMessageRequested?.Invoke(this, "No hay proveedores disponibles. Debes crear al menos uno para registrar entradas.");
                }
            }
            catch (Exception ex)
            {
                Suppliers.Clear();
                SelectedSupplier = null;
                ShowMessageRequested?.Invoke(this, $"No se pudieron cargar proveedores ({ex.Message}). El proveedor es obligatorio para guardar entradas.");
            }
        }

        // ── Comandos de búsqueda ──────────────────────────────────────────

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

                // Exact barcode match has priority
                var exact = results.FirstOrDefault(p =>
                    p.Barcode.Equals(term, StringComparison.OrdinalIgnoreCase));
                var product = exact ?? results[0];

                AddOrIncrementLine(product);
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

        private void AddOrIncrementLine(Product product)
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
                var newLine = new EntryLineItem
                {
                    ProductId = product.Id,
                    Barcode = product.Barcode,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitCost = 0m   // El usuario captura el costo de compra
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

        public void AddProductFromPosCatalog(Product product, int quantity)
        {
            var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += Math.Max(1, quantity);
                SelectedLine = existing;
                ProductAddedOrUpdated?.Invoke(this, existing);
                return;
            }

            var newLine = new EntryLineItem
            {
                ProductId = product.Id,
                Barcode = product.Barcode,
                ProductName = product.Name,
                Quantity = Math.Max(1, quantity),
                UnitCost = 0m
            };

            Lines.Add(newLine);
            SelectedLine = newLine;
            ProductAddedOrUpdated?.Invoke(this, newLine);
        }

        // ── Comandos de líneas ────────────────────────────────────────────

        [RelayCommand]
        private void RemoveLine(EntryLineItem? line)
        {
            if (line != null)
                Lines.Remove(line);
        }

        [RelayCommand]
        private void IncrementQty(EntryLineItem? line)
        {
            if (line != null) line.Quantity++;
        }

        [RelayCommand]
        private void DecrementQty(EntryLineItem? line)
        {
            if (line != null && line.Quantity > 1) line.Quantity--;
        }

        // ── Guardar ───────────────────────────────────────────────────────

        [RelayCommand]
        private async Task SaveEntryAsync()
        {
            if (SelectedSupplier == null)
            {
                ShowMessageRequested?.Invoke(this, "Selecciona un proveedor para guardar la entrada.");
                return;
            }

            if (Lines.Count == 0)
            {
                ShowMessageRequested?.Invoke(this, "Agrega al menos un producto a la entrada antes de guardar.");
                return;
            }

            IsSaving = true;
            try
            {
                var folio = await _folioService.GenerarFolioEntradaAsync(_branchId);

                var entry = new StockEntry
                {
                    Folio = folio,
                    BranchId = _branchId,
                    SupplierId = SelectedSupplier.Id,
                    UserId = _userId,
                    EntryType = StockEntryType.Purchase,
                    TotalAmount = TotalAmount,
                    EntryDate = EntryDate?.DateTime ?? DateTime.Now,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
                };

                var products = Lines.Select(l => new EntryProduct
                {
                    ProductId = l.ProductId,
                    Barcode = l.Barcode,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost,
                    LineTotal = l.LineTotal
                }).ToList();

                await _inventoryService.CreateEntryAsync(entry, products);
                GoBackRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ShowMessageRequested?.Invoke(this, $"Error al guardar la entrada: {ex.Message}");
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

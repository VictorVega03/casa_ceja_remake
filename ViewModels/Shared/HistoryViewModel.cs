using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.ViewModels.Shared
{
    public class HistoryItem
    {
        public string Folio { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // "ENTRADA" o "SALIDA"
        public DateTime Fecha { get; set; }
        public string DestinoOrigen { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;

        public StockEntry? Entry { get; set; }
        public StockOutput? Output { get; set; }

        public bool IsEntry => Tipo == "ENTRADA";
    }

    public partial class HistoryViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly int _currentBranchId;
        private readonly CasaCejaRemake.Services.ApiClient? _apiClient;

        public event EventHandler? GoBackRequested;
        public event EventHandler<HistoryItem>? MovementDetailRequested;

        [ObservableProperty]
        private DateTimeOffset? _startDate = DateTimeOffset.Now.AddDays(-30);

        [ObservableProperty]
        private DateTimeOffset? _endDate = DateTimeOffset.Now;

        [ObservableProperty]
        private int _selectedFilterIndex = 0; // 0=Todos, 1=Entradas, 2=Salidas

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private HistoryItem? _selectedItem;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private string _paginationInfo = "Página 1 de 1";

        [ObservableProperty]
        private bool _canGoPrevious;

        [ObservableProperty]
        private bool _canGoNext;

        [ObservableProperty]
        private int _totalEntries;

        [ObservableProperty]
        private int _totalOutputs;

        [ObservableProperty]
        private bool _isAdminMode;

        [ObservableProperty]
        private ObservableCollection<Branch> _availableBranches = new();

        [ObservableProperty]
        private Branch? _selectedBranch;

        [ObservableProperty]
        private bool _isOffline;

        [ObservableProperty]
        private string _offlineMessage = string.Empty;

        private const int PageSize = 50;
        private bool _isUpdatingFilters;
        private List<HistoryItem> _allItems = new();

        public ObservableCollection<HistoryItem> Items { get; } = new();

        public HistoryViewModel(
            InventoryService inventoryService,
            int branchId,
            bool isAdminMode = false,
            List<Branch>? allBranches = null,
            CasaCejaRemake.Services.ApiClient? apiClient = null)
        {
            _inventoryService = inventoryService;
            _currentBranchId = branchId;
            _apiClient = apiClient;
            _isAdminMode = isAdminMode;

            if (isAdminMode)
            {
                AvailableBranches.Add(new Branch { Id = 0, Name = "Todas las sucursales" });
                foreach (var branch in allBranches ?? new List<Branch>())
                {
                    AvailableBranches.Add(branch);
                }

                _selectedBranch = AvailableBranches.FirstOrDefault();
                OnPropertyChanged(nameof(SelectedBranch));
            }

            _ = SearchAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            IsSearching = true;
            StatusMessage = "Cargando registros";

            // Check de conectividad una sola vez al cargar/refrescar
            if (_apiClient != null)
            {
                var online = await _apiClient.IsServerAvailableAsync();
                IsOffline = !online;
                OfflineMessage = online
                    ? string.Empty
                    : "Sin conexión al servidor";
            }

            try
            {
                var start = StartDate?.DateTime ?? DateTime.Today.AddDays(-30);
                var end = EndDate?.DateTime ?? DateTime.Today;

                _allItems.Clear();

                // Un solo query por catálogo — evita N+1
                var supplierMap = await _inventoryService.GetSupplierNameMapAsync();
                var branchMap = await _inventoryService.GetBranchNameMapAsync();

                if (SelectedFilterIndex == 0 || SelectedFilterIndex == 1)
                {
                    var entries = await GetEntriesForCurrentSelectionAsync(start, end);
                    foreach (var e in entries)
                    {
                        var supplierName = e.SupplierId > 0
                            ? (supplierMap.TryGetValue(e.SupplierId, out var name) ? name : "Desconocido")
                            : "Sin proveedor";
                        var estado = GetEntryStatus(e);

                        _allItems.Add(new HistoryItem
                        {
                            Folio = e.Folio,
                            Tipo = "ENTRADA",
                            Fecha = e.EntryDate,
                            DestinoOrigen = e.EntryType == StockEntryType.Transfer
                                ? $"Traspaso: {(supplierMap.TryGetValue(e.SupplierId, out var sName) ? sName : "Sin proveedor")}" 
                                : $"Proveedor: {supplierName}",
                            Total = e.TotalAmount,
                            Estado = estado,
                            Entry = e
                        });
                    }
                }

                if (SelectedFilterIndex == 0 || SelectedFilterIndex == 2)
                {
                    var outputs = await GetOutputsForCurrentSelectionAsync(start, end);
                    foreach (var o in outputs)
                    {
                        var branchName = branchMap.TryGetValue(o.DestinationBranchId, out var bName) ? bName : "Desconocido";
                        _allItems.Add(new HistoryItem
                        {
                            Folio = o.Folio,
                            Tipo = "SALIDA",
                            Fecha = o.OutputDate,
                            DestinoOrigen = $"Sucursal: {branchName}",
                            Total = o.TotalAmount,
                            Estado = NormalizeMovementStatus(o.Status),
                            Output = o
                        });
                    }
                }

                _allItems = _allItems.OrderByDescending(x => x.Fecha).ToList();

                TotalEntries = _allItems.Count(x => x.IsEntry);
                TotalOutputs = _allItems.Count(x => !x.IsEntry);

                CurrentPage = 1;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)_allItems.Count / PageSize));
                
                LoadCurrentPage();
                
                StatusMessage = $"{_allItems.Count} movimientos encontrados";
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
        private async Task RefreshAsync()
        {
            await SearchAsync();
        }

        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            try
            {
                _isUpdatingFilters = true;
                StartDate = DateTimeOffset.Now.AddDays(-30);
                EndDate = DateTimeOffset.Now;
                SelectedFilterIndex = 0;
            }
            finally
            {
                _isUpdatingFilters = false;
            }

            await SearchAsync();
        }

        private Task<List<StockEntry>> GetEntriesForCurrentSelectionAsync(DateTime start, DateTime end)
        {
            if (!IsAdminMode)
                return _inventoryService.GetEntriesFromServerAsync(_currentBranchId, start, end);

            var branchId = SelectedBranch?.Id ?? 0;
            return branchId <= 0
                ? GetAllEntriesFromServerAsync(start, end)
                : _inventoryService.GetEntriesFromServerAsync(branchId, start, end);
        }

        private Task<List<StockOutput>> GetOutputsForCurrentSelectionAsync(DateTime start, DateTime end)
        {
            if (!IsAdminMode)
                return _inventoryService.GetOutputsFromServerAsync(_currentBranchId, start, end);

            var branchId = SelectedBranch?.Id ?? 0;
            return branchId <= 0
                ? GetAllOutputsFromServerAsync(start, end)
                : GetOutputsInvolvingBranchFromServerAsync(branchId, start, end);
        }

        private async Task<List<StockEntry>> GetAllEntriesFromServerAsync(DateTime start, DateTime end)
        {
            var branchIds = AvailableBranches.Where(b => b.Id > 0).Select(b => b.Id).Distinct().ToList();
            if (branchIds.Count == 0)
                return await _inventoryService.GetAllEntriesAsync(start, end);

            var result = new Dictionary<string, StockEntry>();
            foreach (var branchId in branchIds)
            {
                var entries = await _inventoryService.GetEntriesFromServerAsync(branchId, start, end);
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Folio))
                        result[entry.Folio] = entry;
                }
            }

            return result.Values.OrderByDescending(x => x.EntryDate).ToList();
        }

        private async Task<List<StockOutput>> GetAllOutputsFromServerAsync(DateTime start, DateTime end)
        {
            var branchIds = AvailableBranches.Where(b => b.Id > 0).Select(b => b.Id).Distinct().ToList();
            if (branchIds.Count == 0)
                return await _inventoryService.GetAllOutputsAsync(start, end);

            var result = new Dictionary<string, StockOutput>();
            foreach (var branchId in branchIds)
            {
                var outputs = await _inventoryService.GetOutputsFromServerAsync(branchId, start, end);
                foreach (var output in outputs)
                {
                    if (!string.IsNullOrWhiteSpace(output.Folio))
                        result[output.Folio] = output;
                }
            }

            return result.Values.OrderByDescending(x => x.OutputDate).ToList();
        }

        private async Task<List<StockOutput>> GetOutputsInvolvingBranchFromServerAsync(int branchId, DateTime start, DateTime end)
        {
            var outputs = await GetAllOutputsFromServerAsync(start, end);
            return outputs
                .Where(o => o.OriginBranchId == branchId || o.DestinationBranchId == branchId)
                .OrderByDescending(o => o.OutputDate)
                .ToList();
        }

        private static string GetEntryStatus(StockEntry entry)
        {
            if (entry.EntryType == StockEntryType.Transfer)
                return entry.ConfirmedAt != null ? "CONFIRMADO" : "PENDIENTE";

            return entry.SyncStatus == 2 ? "SINCRONIZADO" : "PENDIENTE";
        }

        private static string NormalizeMovementStatus(string? status)
        {
            return status?.Trim().ToUpperInvariant() switch
            {
                "PENDING" or "PENDIENTE" or "PENDIENTE SYNC" => "PENDIENTE",
                "CONFIRMED" or "CONFIRMADO" => "CONFIRMADO",
                "SYNCED" or "SINCRONIZADO" => "SINCRONIZADO",
                "CANCELLED" or "CANCELADO" => "CANCELADO",
                "" or null => "PENDIENTE",
                var value => value
            };
        }

        partial void OnSelectedBranchChanged(Branch? value)
        {
            if (!_isUpdatingFilters)
                _ = SearchAsync();
        }

        partial void OnSelectedFilterIndexChanged(int value)
        {
            if (!_isUpdatingFilters)
                _ = SearchAsync();
        }

        partial void OnStartDateChanged(DateTimeOffset? value)
        {
            if (!_isUpdatingFilters && value != null && EndDate != null)
                _ = SearchAsync();
        }

        partial void OnEndDateChanged(DateTimeOffset? value)
        {
            if (!_isUpdatingFilters && value != null && StartDate != null)
                _ = SearchAsync();
        }

        private void LoadCurrentPage()
        {
            Items.Clear();
            var pageItems = _allItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
            foreach (var item in pageItems)
            {
                Items.Add(item);
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

        public void RequestDetail(HistoryItem item)
        {
            MovementDetailRequested?.Invoke(this, item);
        }

        [RelayCommand]
        private void GoBack()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }

        // ── Exportación a Excel ────────────────────────────────────────────────

        private record EntryExportRow(
            string Folio, DateTime Fecha, string Proveedor, string Tipo,
            decimal Total, string Estado, string Notas);

        private record OutputExportRow(
            string Folio, DateTime Fecha, string Destino,
            decimal Total, string Estado, string Notas);

        public async Task<List<ExportSheetData>> PrepareExportAsync(
            ExportService exportService, bool includeEntries, bool includeOutputs)
        {
            var sheets = new List<ExportSheetData>();
            var start = StartDate?.DateTime.Date ?? DateTime.Today.AddDays(-30);
            var end   = EndDate?.DateTime.Date   ?? DateTime.Today;
            var dateRange = $"{start:dd/MM/yyyy} al {end:dd/MM/yyyy}";

            var supplierMap = await _inventoryService.GetSupplierNameMapAsync();
            var branchMap   = await _inventoryService.GetBranchNameMapAsync();

            if (includeEntries)
            {
                var entries = await GetEntriesForCurrentSelectionAsync(start, end);

                var entryRows = entries.Select(e =>
                {
                    var proveedor = e.EntryType == StockEntryType.Transfer
                        ? (supplierMap.TryGetValue(e.SupplierId, out var sn) ? $"Traspaso desde: {sn}" : "Traspaso")
                        : (supplierMap.TryGetValue(e.SupplierId, out var sn2) ? sn2 : "-");
                    var estado = GetEntryStatus(e);
                    return new EntryExportRow(
                        e.Folio, e.EntryDate, proveedor,
                        e.EntryType == StockEntryType.Transfer ? "TRASPASO" : "COMPRA",
                        e.TotalAmount, estado, e.Notes ?? "");
                }).ToList();

                sheets.Add(exportService.CreateSheetData(
                    entryRows,
                    new List<ExportColumn<EntryExportRow>>
                    {
                        new() { Header = "Folio",     ValueSelector = r => r.Folio,    Width = 24 },
                        new() { Header = "Fecha",     ValueSelector = r => r.Fecha,    Width = 18, Format = "dd/MM/yyyy HH:mm" },
                        new() { Header = "Proveedor/Origen", ValueSelector = r => r.Proveedor, Width = 30 },
                        new() { Header = "Tipo",      ValueSelector = r => r.Tipo,     Width = 12 },
                        new() { Header = "Total",     ValueSelector = r => r.Total,    Width = 14, Format = "$#,##0.00" },
                        new() { Header = "Estado",    ValueSelector = r => r.Estado,   Width = 16 },
                        new() { Header = "Notas",     ValueSelector = r => r.Notas,    Width = 30 },
                    },
                    "Entradas",
                    $"Historial de Entradas — {dateRange}"));

                // Hoja de productos agrupada por entrada (patrón igual al reporte de ventas)
                var entryDetailRows = new List<(string Label, object Value)>();
                var sep = new string('═', 51);
                foreach (var entry in entries)
                {
                    var proveedor = entry.EntryType == StockEntryType.Transfer
                        ? (supplierMap.TryGetValue(entry.SupplierId, out var sn) ? $"Traspaso desde: {sn}" : "Traspaso")
                        : (supplierMap.TryGetValue(entry.SupplierId, out var sn2) ? sn2 : "-");
                    var estado = GetEntryStatus(entry);

                    entryDetailRows.Add((sep, ""));
                    entryDetailRows.Add(($"ENTRADA: {entry.Folio}", ""));
                    entryDetailRows.Add((sep, ""));
                    entryDetailRows.Add(("", ""));
                    entryDetailRows.Add(("Fecha",     entry.EntryDate.ToString("dd/MM/yyyy HH:mm")));
                    entryDetailRows.Add(("Proveedor/Origen", proveedor));
                    entryDetailRows.Add(("Tipo",      entry.EntryType == StockEntryType.Transfer ? "TRASPASO" : "COMPRA"));
                    entryDetailRows.Add(("Total",     entry.TotalAmount.ToString("C2")));
                    entryDetailRows.Add(("Estado",    estado));
                    if (!string.IsNullOrWhiteSpace(entry.Notes))
                        entryDetailRows.Add(("Notas", entry.Notes));
                    entryDetailRows.Add(("", ""));

                    var prods = await _inventoryService.GetEntryProductsAsync(entry.Id);
                    if (prods.Any())
                    {
                        entryDetailRows.Add(("--- PRODUCTOS ---", ""));
                        foreach (var p in prods)
                        {
                            entryDetailRows.Add(($"  • {p.ProductName}", ""));
                            entryDetailRows.Add(($"    Código",           p.Barcode));
                            entryDetailRows.Add(($"    Cantidad",         p.Quantity.ToString()));
                            entryDetailRows.Add(($"    Costo Unitario",   p.UnitCost.ToString("C2")));
                            entryDetailRows.Add(($"    Subtotal",         p.LineTotal.ToString("C2")));
                            entryDetailRows.Add(("", ""));
                        }
                    }
                    entryDetailRows.Add(("", ""));
                    entryDetailRows.Add(("", ""));
                }

                var detailCols = new List<ExportColumn<(string Label, object Value)>>
                {
                    new() { Header = "Campo", ValueSelector = d => d.Label, Width = 42 },
                    new() { Header = "Valor", ValueSelector = d => d.Value, Width = 28 }
                };

                sheets.Add(exportService.CreateSheetData(
                    entryDetailRows, detailCols,
                    "Productos Entradas",
                    $"Detalle de Productos por Entrada — {dateRange}"));
            }

            if (includeOutputs)
            {
                var outputs = await GetOutputsForCurrentSelectionAsync(start, end);

                var outputRows = outputs.Select(o =>
                {
                    var destino = branchMap.TryGetValue(o.DestinationBranchId, out var bn) ? bn : "Desconocido";
                    return new OutputExportRow(
                        o.Folio, o.OutputDate, destino,
                        o.TotalAmount, NormalizeMovementStatus(o.Status), o.Notes ?? "");
                }).ToList();

                sheets.Add(exportService.CreateSheetData(
                    outputRows,
                    new List<ExportColumn<OutputExportRow>>
                    {
                        new() { Header = "Folio",         ValueSelector = r => r.Folio,   Width = 24 },
                        new() { Header = "Fecha",         ValueSelector = r => r.Fecha,   Width = 18, Format = "dd/MM/yyyy HH:mm" },
                        new() { Header = "Sucursal Destino", ValueSelector = r => r.Destino, Width = 28 },
                        new() { Header = "Total",         ValueSelector = r => r.Total,   Width = 14, Format = "$#,##0.00" },
                        new() { Header = "Estado",        ValueSelector = r => r.Estado,  Width = 14 },
                        new() { Header = "Notas",         ValueSelector = r => r.Notas,   Width = 30 },
                    },
                    "Salidas",
                    $"Historial de Salidas — {dateRange}"));

                // Hoja de productos agrupada por salida
                var outputDetailRows = new List<(string Label, object Value)>();
                var sepO = new string('═', 51);
                foreach (var output in outputs)
                {
                    var destino = branchMap.TryGetValue(output.DestinationBranchId, out var bn) ? bn : "Desconocido";

                    outputDetailRows.Add((sepO, ""));
                    outputDetailRows.Add(($"SALIDA: {output.Folio}", ""));
                    outputDetailRows.Add((sepO, ""));
                    outputDetailRows.Add(("", ""));
                    outputDetailRows.Add(("Fecha",            output.OutputDate.ToString("dd/MM/yyyy HH:mm")));
                    outputDetailRows.Add(("Sucursal Destino", destino));
                    outputDetailRows.Add(("Total",            output.TotalAmount.ToString("C2")));
                    outputDetailRows.Add(("Estado",           NormalizeMovementStatus(output.Status)));
                    if (!string.IsNullOrWhiteSpace(output.Notes))
                        outputDetailRows.Add(("Notas", output.Notes));
                    outputDetailRows.Add(("", ""));

                    var prods = await _inventoryService.GetOutputProductsAsync(output.Id);
                    if (prods.Any())
                    {
                        outputDetailRows.Add(("--- PRODUCTOS ---", ""));
                        foreach (var p in prods)
                        {
                            outputDetailRows.Add(($"  • {p.ProductName}", ""));
                            outputDetailRows.Add(($"    Código",           p.Barcode));
                            outputDetailRows.Add(($"    Cantidad",         p.Quantity.ToString()));
                            outputDetailRows.Add(($"    Costo Unitario",   p.UnitCost.ToString("C2")));
                            outputDetailRows.Add(($"    Subtotal",         p.LineTotal.ToString("C2")));
                            outputDetailRows.Add(("", ""));
                        }
                    }
                    outputDetailRows.Add(("", ""));
                    outputDetailRows.Add(("", ""));
                }

                var detailColsO = new List<ExportColumn<(string Label, object Value)>>
                {
                    new() { Header = "Campo", ValueSelector = d => d.Label, Width = 42 },
                    new() { Header = "Valor", ValueSelector = d => d.Value, Width = 28 }
                };

                sheets.Add(exportService.CreateSheetData(
                    outputDetailRows, detailColsO,
                    "Productos Salidas",
                    $"Detalle de Productos por Salida — {dateRange}"));
            }

            return sheets;
        }
    }
}

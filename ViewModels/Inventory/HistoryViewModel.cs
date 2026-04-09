using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class HistoryItem
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

        private const int PageSize = 50;
        private List<HistoryItem> _allItems = new();

        public ObservableCollection<HistoryItem> Items { get; } = new();

        public HistoryViewModel(InventoryService inventoryService, int branchId)
        {
            _inventoryService = inventoryService;
            _currentBranchId = branchId;

            _ = SearchAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            IsSearching = true;
            StatusMessage = "Cargando historial...";
            try
            {
                var start = StartDate?.DateTime ?? DateTime.Today.AddDays(-30);
                var end = EndDate?.DateTime ?? DateTime.Today;

                _allItems.Clear();

                if (SelectedFilterIndex == 0 || SelectedFilterIndex == 1)
                {
                    var entries = await _inventoryService.GetEntriesAsync(_currentBranchId, start, end);
                    foreach (var e in entries)
                    {
                        var supplierName = e.SupplierId > 0 ? await _inventoryService.GetSupplierNameAsync(e.SupplierId) : "Desconocido";
                        _allItems.Add(new HistoryItem
                        {
                            Folio = e.Folio,
                            Tipo = "ENTRADA",
                            Fecha = e.EntryDate,
                            DestinoOrigen = $"Proveedor: {supplierName}",
                            Total = e.TotalAmount,
                            Estado = "CONFIRMADO", 
                            Entry = e
                        });
                    }
                }

                if (SelectedFilterIndex == 0 || SelectedFilterIndex == 2)
                {
                    var outputs = await _inventoryService.GetOutputsAsync(_currentBranchId, start, end);
                    foreach (var o in outputs)
                    {
                        var branchName = await _inventoryService.GetBranchNameAsync(o.DestinationBranchId);
                        _allItems.Add(new HistoryItem
                        {
                            Folio = o.Folio,
                            Tipo = "SALIDA",
                            Fecha = o.OutputDate,
                            DestinoOrigen = $"Sucursal: {branchName}",
                            Total = o.TotalAmount,
                            Estado = o.Status,
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

        partial void OnSelectedFilterIndexChanged(int value)
        {
            _ = SearchAsync();
        }

        partial void OnStartDateChanged(DateTimeOffset? value)
        {
            if (value != null && EndDate != null) _ = SearchAsync();
        }

        partial void OnEndDateChanged(DateTimeOffset? value)
        {
            if (value != null && StartDate != null) _ = SearchAsync();
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
    }
}

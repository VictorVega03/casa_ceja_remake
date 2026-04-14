using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.DTOs;
using CasaCejaRemake.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class ConfirmLineItem : ObservableObject
    {
        public int ProductId { get; init; }
        public string Barcode { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public int OriginalQuantity { get; init; }
        public decimal UnitCost { get; init; }

        [ObservableProperty]
        private int _receivedQuantity;

        public bool HasDiscrepancy => ReceivedQuantity != OriginalQuantity;
        public int Discrepancy => OriginalQuantity - ReceivedQuantity;

        partial void OnReceivedQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(HasDiscrepancy));
            OnPropertyChanged(nameof(Discrepancy));
        }
    }

    public class PendingEntryItem
    {
        public int Id { get; init; }
        public string Folio { get; init; } = string.Empty;
        public string FolioOutput { get; init; } = string.Empty;
        public string OriginBranchName { get; init; } = string.Empty;
        public DateTime EntryDate { get; init; }
        public decimal TotalAmount { get; init; }
        public string? Notes { get; set; }
        public ObservableCollection<ConfirmLineItem> Lines { get; init; } = new();

        // Propiedades calculadas para la vista
        public bool IsTransfer => !string.IsNullOrEmpty(FolioOutput);
        public string TypeLabel => IsTransfer ? "TRASPASO" : "COMPRA";
        public string SupplierName => IsTransfer ? OriginBranchName : "—";
        public string DateDisplay => EntryDate.ToString("dd/MM/yyyy HH:mm");
        public int ProductCount => Lines.Count;
        public bool HasDiscrepancies => Lines.Any(l => l.HasDiscrepancy);
    }

    public partial class ConfirmEntryViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly ApiClient _apiClient;
        private readonly int _branchId;
        private readonly int _userId;

        public event EventHandler? GoBackRequested;
        public event EventHandler<string>? ShowMessageRequested;
        public event EventHandler<PendingEntryItem>? ConfirmRequested;

        public ObservableCollection<PendingEntryItem> PendingEntries { get; } = new();

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isConfirming;
        [ObservableProperty] private string _branchName = string.Empty;
        [ObservableProperty] private PendingEntryItem? _selectedEntry;

        public bool HasPending => PendingEntries.Count > 0;

        public ConfirmEntryViewModel(
            InventoryService inventoryService,
            ApiClient apiClient,
            int branchId,
            string branchName,
            int userId)
        {
            _inventoryService = inventoryService;
            _apiClient = apiClient;
            _branchId = branchId;
            _userId = userId;
            BranchName = branchName;

            PendingEntries.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasPending));

            _ = LoadPendingAsync();
        }

        [RelayCommand]
        private async Task LoadPendingAsync()
        {
            IsLoading = true;
            try
            {
                var response = await _apiClient.GetAsync<List<PendingTransferDto>>(
                    $"/api/v1/inventory/pending-transfers?branch_id={_branchId}");

                if (response?.IsSuccess != true || response.Data == null)
                {
                    ShowMessageRequested?.Invoke(this,
                        "No se pudieron cargar los traspasos pendientes. Verifica la conexión.");
                    return;
                }

                PendingEntries.Clear();
                foreach (var dto in response.Data)
                {
                    var item = new PendingEntryItem
                    {
                        Id = dto.Id,
                        Folio = dto.Folio,
                        FolioOutput = dto.FolioOutput,
                        OriginBranchName = dto.OriginBranchName,
                        EntryDate = dto.EntryDate,
                        TotalAmount = dto.TotalAmount,
                        Notes = dto.Notes,
                    };

                    foreach (var p in dto.Products)
                    {
                        item.Lines.Add(new ConfirmLineItem
                        {
                            ProductId = p.ProductId,
                            Barcode = p.Barcode,
                            ProductName = p.ProductName,
                            OriginalQuantity = p.Quantity,
                            ReceivedQuantity = p.Quantity,
                            UnitCost = p.UnitCost,
                        });
                    }

                    PendingEntries.Add(item);
                }
            }
            catch (Exception ex)
            {
                ShowMessageRequested?.Invoke(this, $"Error al cargar traspasos: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void SelectEntry(PendingEntryItem? entry)
        {
            SelectedEntry = entry;
        }

        [RelayCommand]
        private void RequestConfirm(PendingEntryItem? entry)
        {
            if (entry == null) return;
            ConfirmRequested?.Invoke(this, entry);
        }

        [RelayCommand]
        private void ConfirmEntry(PendingEntryItem? entry)
        {
            if (entry == null) return;
            ConfirmRequested?.Invoke(this, entry);
        }

        public async Task DoConfirmEntryAsync(PendingEntryItem entry)
        {
            IsConfirming = true;
            try
            {
                var request = new ConfirmTransferRequest
                {
                    ConfirmedByUserId = _userId,
                    Products = entry.Lines.Select(l => new ConfirmTransferProductRequest
                    {
                        ProductId = l.ProductId,
                        Quantity = l.ReceivedQuantity
                    }).ToList()
                };

                var response = await _apiClient.PostAsync<FolioResponse>(
                    $"/api/v1/inventory/confirm-transfer/{entry.Id}", request);

                if (response?.IsSuccess == true)
                {
                    // Servidor confirmó — persistir localmente: crear entrada + actualizar stock
                    await PersistConfirmedEntryLocallyAsync(entry);

                    PendingEntries.Remove(entry);
                    if (SelectedEntry == entry) SelectedEntry = null;

                    var msg = entry.HasDiscrepancies
                        ? $"Entrada {entry.Folio} confirmada con diferencias.\nLas unidades faltantes quedan registradas como merma."
                        : $"Entrada {entry.Folio} confirmada correctamente.\nStock actualizado en esta sucursal.";

                    ShowMessageRequested?.Invoke(this, msg);
                }
                else
                {
                    ShowMessageRequested?.Invoke(this,
                        "Error al confirmar en el servidor. Verifica la conexión e intenta de nuevo.");
                }
            }
            catch (Exception ex)
            {
                ShowMessageRequested?.Invoke(this, $"Error al confirmar: {ex.Message}");
            }
            finally
            {
                IsConfirming = false;
            }
        }

        /// <summary>
        /// Persiste la entrada confirmada en la BD local: crea StockEntry + EntryProducts
        /// con las cantidades realmente recibidas y actualiza el stock de la sucursal.
        /// </summary>
        private async Task PersistConfirmedEntryLocallyAsync(PendingEntryItem entry)
        {
            try
            {
                var localEntry = new StockEntry
                {
                    Folio = entry.Folio,
                    FolioOutput = entry.FolioOutput,
                    BranchId = _branchId,
                    SupplierId = 0, // Traspaso no tiene proveedor
                    UserId = _userId,
                    EntryType = StockEntryType.Transfer,
                    TotalAmount = entry.Lines.Sum(l => l.ReceivedQuantity * l.UnitCost),
                    EntryDate = entry.EntryDate,
                    Notes = entry.Notes,
                    ConfirmedByUserId = _userId,
                    ConfirmedAt = DateTime.Now,
                    SyncStatus = 2, // Ya viene del servidor
                    LastSync = DateTime.Now
                };

                var products = entry.Lines.Select(l => new EntryProduct
                {
                    ProductId = l.ProductId,
                    Barcode = l.Barcode,
                    ProductName = l.ProductName,
                    Quantity = l.ReceivedQuantity,
                    UnitCost = l.UnitCost,
                    LineTotal = l.ReceivedQuantity * l.UnitCost
                }).ToList();

                // CreateEntryAsync guarda la entrada + productos + actualiza stock
                // El SyncStatus ya viene como 2 para que no se reintente subir
                await _inventoryService.CreateEntryAsync(localEntry, products);

                Console.WriteLine($"[ConfirmEntry] Entrada {entry.Folio} persistida localmente con {products.Count} productos");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfirmEntry] Error al persistir localmente: {ex.Message}");
                // No lanzamos — el servidor ya confirmó, la persistencia local es best-effort
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

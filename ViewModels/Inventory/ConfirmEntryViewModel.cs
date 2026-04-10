using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Inventory
{
    /// <summary>
    /// Item de entrada pendiente mostrado en la lista.
    /// </summary>
    public class PendingEntryItem
    {
        public int Id { get; init; }
        public string Folio { get; init; } = string.Empty;
        public DateTime EntryDate { get; init; }
        public decimal TotalAmount { get; init; }
        public string? Notes { get; init; }
        public string SupplierName { get; init; } = string.Empty;
        public string FolioOutput { get; init; } = string.Empty;

        public string DateDisplay => EntryDate.ToString("dd/MM/yyyy");
        public string TypeLabel => string.IsNullOrEmpty(FolioOutput) ? "Compra" : "Traspaso";
        public bool IsTransfer => !string.IsNullOrEmpty(FolioOutput);
    }

    public partial class ConfirmEntryViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly int _branchId;
        private readonly int _userId;

        // ── Eventos ──────────────────────────────────────────────────────
        public event EventHandler? GoBackRequested;
        public event EventHandler<string>? ShowMessageRequested;
        public event EventHandler<PendingEntryItem>? ConfirmRequested;

        // ── Colecciones ───────────────────────────────────────────────────
        public ObservableCollection<PendingEntryItem> PendingEntries { get; } = new();

        // ── Estado ───────────────────────────────────────────────────────
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isConfirming;
        [ObservableProperty] private string _branchName = string.Empty;
        [ObservableProperty] private PendingEntryItem? _selectedEntry;

        public bool HasPending => PendingEntries.Count > 0;

        public ConfirmEntryViewModel(InventoryService inventoryService, int branchId, string branchName, int userId)
        {
            _inventoryService = inventoryService;
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
                var entries = await _inventoryService.GetPendingEntriesAsync(_branchId);
                
                // Un solo query para todos los proveedores — evita N+1
                var supplierMap = await _inventoryService.GetSupplierNameMapAsync();

                PendingEntries.Clear();
                foreach (var e in entries.OrderByDescending(x => x.EntryDate))
                {
                    var supplierName = e.SupplierId > 0
                        ? (supplierMap.TryGetValue(e.SupplierId, out var name) ? name : "Desconocido")
                        : "Sin proveedor";

                    PendingEntries.Add(new PendingEntryItem
                    {
                        Id = e.Id,
                        Folio = e.Folio,
                        EntryDate = e.EntryDate,
                        TotalAmount = e.TotalAmount,
                        Notes = e.Notes,
                        SupplierName = supplierName,
                        FolioOutput = e.FolioOutput ?? string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessageRequested?.Invoke(this, $"Error al cargar entradas: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ConfirmEntry(PendingEntryItem? entry)
        {
            if (entry == null) return;
            ConfirmRequested?.Invoke(this, entry);
        }

        /// <summary>
        /// Called from the view after user confirms the dialog.
        /// </summary>
        public async Task DoConfirmEntryAsync(PendingEntryItem entry)
        {
            // FASE 3: La confirmación de traspasos requiere integración con el servidor Laravel.
            // El servidor valida las cantidades y aplica el stock en ambas sucursales.
            ShowMessageRequested?.Invoke(this,
                "Confirmar traspasos requiere conexión al servidor.\n" +
                "Esta funcionalidad estará disponible próximamente.");

            await Task.CompletedTask;
        }

        [RelayCommand]
        private void GoBack()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

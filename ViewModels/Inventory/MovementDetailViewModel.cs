using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class MovementProductItem
    {
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Total => Quantity * UnitCost;
    }

    public partial class MovementDetailViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _subtitle = string.Empty;

        [ObservableProperty]
        private decimal _totalAmount;

        public ObservableCollection<MovementProductItem> Products { get; } = new();

        public event EventHandler? CloseRequested;
        
        public MovementDetailViewModel(InventoryService inventoryService, HistoryItem historyItem)
        {
            _inventoryService = inventoryService;
            TotalAmount = historyItem.Total;

            if (historyItem.Tipo == "ENTRADA")
            {
                Title = $"Detalle de Entrada - {historyItem.Folio}";
                Subtitle = $"Fecha: {historyItem.Fecha:dd/MM/yyyy HH:mm} | Origen: {historyItem.DestinoOrigen}";
                _ = LoadEntryProductsAsync(historyItem.Entry!.Id);
            }
            else
            {
                Title = $"Detalle de Salida - {historyItem.Folio}";
                Subtitle = $"Fecha: {historyItem.Fecha:dd/MM/yyyy HH:mm} | Destino: {historyItem.DestinoOrigen}";
                _ = LoadOutputProductsAsync(historyItem.Output!.Id);
            }
        }

        private async Task LoadEntryProductsAsync(int entryId)
        {
            var e_products = await _inventoryService.GetEntryProductsAsync(entryId);
            foreach (var p in e_products)
            {
                Products.Add(new MovementProductItem
                {
                    Barcode = p.Barcode,
                    Name = p.ProductName ?? "Producto",
                    Quantity = p.Quantity,
                    UnitCost = p.UnitCost
                });
            }
        }

        private async Task LoadOutputProductsAsync(int outputId)
        {
            var o_products = await _inventoryService.GetOutputProductsAsync(outputId);
            foreach (var p in o_products)
            {
                Products.Add(new MovementProductItem
                {
                    Barcode = p.Barcode ?? "",
                    Name = p.ProductName ?? "Producto",
                    Quantity = p.Quantity,
                    UnitCost = p.UnitCost
                });
            }
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

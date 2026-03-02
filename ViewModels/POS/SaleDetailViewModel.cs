using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    /// <summary>
    /// ViewModel para mostrar los detalles de una venta.
    /// </summary>
    public partial class SaleDetailViewModel : ViewModelBase
    {
        private readonly SalesService _salesService;
        private readonly TicketService _ticketService;

        [ObservableProperty]
        private Sale? _sale;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _branchName = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _ticketText = string.Empty;

        public ObservableCollection<SaleProduct> Products { get; } = new();

        public string Folio => Sale?.Folio ?? "---";
        public string FormattedDate => Sale?.SaleDate.ToString("dd/MM/yyyy HH:mm:ss") ?? "---";
        public decimal Subtotal => Sale?.Subtotal ?? 0;
        public decimal Discount => Sale?.Discount ?? 0;
        public decimal Total => Sale?.Total ?? 0;
        public decimal AmountPaid => Sale?.AmountPaid ?? 0;
        public decimal ChangeGiven => Sale?.ChangeGiven ?? 0;
        public string PaymentSummary => Sale?.PaymentSummary ?? "---";

        public event EventHandler<string>? ReprintRequested;
        public event EventHandler? CloseRequested;

        public SaleDetailViewModel(SalesService salesService, TicketService ticketService)
        {
            _salesService = salesService;
            _ticketService = ticketService;
        }

        public async Task InitializeAsync(int saleId)
        {
            try
            {
                IsLoading = true;
                Products.Clear();

                // Obtener la venta
                var sales = await _salesService.GetSalesHistoryPagedAsync(
                    branchId: 0, // No filter by branch, we're looking by ID
                    page: 1,
                    pageSize: 1);

                // Necesitamos un método para obtener por ID - usamos el repositorio indirectamente
                // Por ahora recuperamos el ticket que tiene toda la info
                var ticketData = await _salesService.RecoverTicketAsync(saleId);
                
                if (ticketData != null)
                {
                    // Generar texto del ticket
                    TicketText = _ticketService.GenerateTicketText(ticketData);
                    
                    // Usar los datos del ticket para mostrar info
                    UserName = ticketData.Sale?.UserName ?? "---";
                    BranchName = ticketData.Branch?.Name ?? "---";
                }

                // Obtener los productos de la venta
                var products = await _salesService.GetSaleProductsAsync(saleId);
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
            catch (Exception)
            {
                // Se maneja silenciosamente en la UI
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void SetSale(Sale sale, string userName)
        {
            Sale = sale;
            UserName = userName;
            OnPropertyChanged(nameof(Folio));
            OnPropertyChanged(nameof(FormattedDate));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Discount));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(AmountPaid));
            OnPropertyChanged(nameof(ChangeGiven));
            OnPropertyChanged(nameof(PaymentSummary));
        }

        [RelayCommand]
        private async Task LoadProducts()
        {
            if (Sale == null) return;

            try
            {
                IsLoading = true;
                Products.Clear();

                Console.WriteLine($"[SaleDetailViewModel] Cargando productos para venta ID: {Sale.Id}");
                var products = await _salesService.GetSaleProductsAsync(Sale.Id);
                Console.WriteLine($"[SaleDetailViewModel] Productos recuperados: {products.Count}");
                
                foreach (var product in products)
                {
                    Console.WriteLine($"[SaleDetailViewModel] Agregando producto: {product.ProductName} (Cantidad: {product.Quantity})");
                    Products.Add(product);
                }

                Console.WriteLine($"[SaleDetailViewModel] Total productos en colección: {Products.Count}");
                
                // Cargar ticket para reimpresión
                var ticketData = await _salesService.RecoverTicketAsync(Sale.Id);
                if (ticketData != null)
                {
                    TicketText = _ticketService.GenerateTicketText(ticketData);
                    Console.WriteLine($"[SaleDetailViewModel] Ticket generado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaleDetailViewModel] ERROR al cargar productos: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Reprint()
        {
            if (!string.IsNullOrEmpty(TicketText))
            {
                ReprintRequested?.Invoke(this, TicketText);
            }
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

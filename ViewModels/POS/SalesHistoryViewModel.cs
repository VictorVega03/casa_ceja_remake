using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    /// <summary>
    /// Wrapper para mostrar datos de ventas en la lista.
    /// </summary>
    public class SaleListItemWrapper
    {
        public Sale Sale { get; set; } = null!;
        public string UserName { get; set; } = string.Empty;

        public int Id => Sale.Id;
        public string Folio => Sale.Folio;
        public DateTime SaleDate => Sale.SaleDate;
        public decimal Total => Sale.Total;
        public decimal Discount => Sale.Discount;
        public string PaymentSummary => Sale.PaymentSummary;
        
        public string FormattedDate => Sale.SaleDate.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// DTO para exportar resumen de ventas con desglose de descuentos a Excel.
    /// </summary>
    public class SaleExportSummary
    {
        public string Folio { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PaymentSummary { get; set; } = string.Empty;
        public decimal Total { get; set; }
        
        // Descuentos desglosados
        public decimal WholesaleDiscount { get; set; }
        public decimal CategoryDiscount { get; set; }
        public decimal SpecialDiscount { get; set; }
        public decimal DealerDiscount { get; set; }
        public decimal GeneralDiscount { get; set; }
        public decimal TotalDiscount { get; set; }
    }

    /// <summary>
    /// ViewModel para la vista de historial de ventas.
    /// </summary>
    public partial class SalesHistoryViewModel : ViewModelBase
    {
        private readonly SalesService _salesService;
        private readonly TicketService _ticketService;
        private readonly int _branchId;

        private const int PageSize = 50;

        /// <summary>
        /// Expone el servicio de ventas para uso en vistas hijas.
        /// </summary>
        public SalesService SalesService => _salesService;

        [ObservableProperty]
        private SaleListItemWrapper? _selectedItem;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private DateTime? _filterDateFrom;

        [ObservableProperty]
        private DateTime? _filterDateTo;

        public ObservableCollection<SaleListItemWrapper> Items { get; } = new();

        public bool HasSelectedItem => SelectedItem != null;
        public bool CanGoBack => CurrentPage > 1;
        public bool CanGoForward => CurrentPage < TotalPages;

        public event EventHandler<SaleListItemWrapper>? ItemSelected;
        public event EventHandler<(Sale Sale, string TicketText)>? ReprintRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler? ExportRequested;

        public SalesHistoryViewModel(
            SalesService salesService,
            int branchId)
        {
            _salesService = salesService;
            _ticketService = new TicketService();
            _branchId = branchId;

            // Filtros por defecto: hoy
            _filterDateTo = DateTime.Today;
            _filterDateFrom = DateTime.Today.AddDays(-30);
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                Items.Clear();

                // Obtener conteo total para paginación
                TotalCount = await _salesService.GetSalesCountAsync(
                    _branchId,
                    FilterDateFrom,
                    FilterDateTo);

                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
                
                // Ajustar página actual si es necesario
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }

                // Obtener ventas de la página actual
                var sales = await _salesService.GetSalesHistoryPagedAsync(
                    _branchId,
                    CurrentPage,
                    PageSize,
                    FilterDateFrom,
                    FilterDateTo);

                foreach (var sale in sales)
                {
                    // Obtener nombre del usuario
                    var userName = await _salesService.GetUserNameAsync(sale.UserId);

                    Items.Add(new SaleListItemWrapper
                    {
                        Sale = sale,
                        UserName = userName
                    });
                }

                UpdateStatus();
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateStatus()
        {
            var showing = Items.Count;
            StatusMessage = $"Mostrando {showing} de {TotalCount} ventas | Página {CurrentPage} de {TotalPages}";
        }

        [RelayCommand]
        private async Task NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task ApplyFilters()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            FilterDateFrom = DateTime.Today.AddDays(-30);
            FilterDateTo = DateTime.Today;
            SearchText = string.Empty;
            CurrentPage = 1;
            await LoadDataAsync();
        }

        [RelayCommand]
        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return;
            }

            // Buscar por folio en los items cargados
            var searchTerm = SearchText.Trim().ToLower();
            var found = Items.FirstOrDefault(i => 
                i.Folio.ToLower().Contains(searchTerm));

            if (found != null)
            {
                SelectedItem = found;
            }
            else
            {
                StatusMessage = $"No se encontró venta con folio: {SearchText}";
            }
        }

        partial void OnSelectedItemChanged(SaleListItemWrapper? value)
        {
            OnPropertyChanged(nameof(HasSelectedItem));
        }

        partial void OnCurrentPageChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        [RelayCommand]
        private void ViewDetail()
        {
            if (SelectedItem != null)
            {
                ItemSelected?.Invoke(this, SelectedItem);
            }
        }

        [RelayCommand]
        private async Task ReprintTicket()
        {
            if (SelectedItem == null) return;

            try
            {
                IsLoading = true;
                var ticketData = await _salesService.RecoverTicketAsync(SelectedItem.Id);
                
                if (ticketData != null)
                {
                    var ticketText = _ticketService.GenerateTicketText(ticketData);
                    ReprintRequested?.Invoke(this, (SelectedItem.Sale, ticketText));
                }
                else
                {
                    StatusMessage = "No se pudo recuperar el ticket de esta venta.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al reimprimir: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            ExportRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Prepara un reporte de 2 hojas:
        /// Hoja 1: Resumen de todas las ventas
        /// Hoja 2: Detalles de todas las ventas con productos en UNA sola hoja (con separadores)
        /// </summary>
        public async Task<List<ExportSheetData>> PrepareMultiSheetExportAsync(ExportService exportService)
        {
            var sheets = new List<ExportSheetData>();

            var summaryData = new List<SaleExportSummary>();
            var allDetails = new List<(string Label, object Value)>();

            foreach (var item in Items)
            {
                var sale = item.Sale;

                // ===== CALCULAR DESCUENTOS PARA EL RESUMEN =====
                var products = await _salesService.GetSaleProductsAsync(sale.Id);

                decimal descMayoreo = 0;
                decimal descCategoria = 0;
                decimal descEspecial = 0;
                decimal descVendedor = 0;

                foreach (var product in products)
                {
                    decimal discountLine = product.TotalDiscountAmount;
                    if (discountLine > 0)
                    {
                        var info = product.DiscountInfo ?? "";
                        decimal categoryDesc = 0;

                        // Extraer descuento de categoría si existe
                        var match = System.Text.RegularExpressions.Regex.Match(info, @"(\d+(?:\.\d+)?)%\s*desc\.");
                        if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal percent))
                        {
                            categoryDesc = product.ListPrice * (percent / 100m) * product.Quantity;
                            descCategoria += categoryDesc;
                        }

                        decimal remainingDesc = discountLine - categoryDesc;
                        if (remainingDesc < 0) remainingDesc = 0;

                        if (info.Contains("Mayoreo", StringComparison.OrdinalIgnoreCase))
                            descMayoreo += remainingDesc;
                        else if (info.Contains("Especial", StringComparison.OrdinalIgnoreCase))
                            descEspecial += remainingDesc;
                        else if (info.Contains("Vendedor", StringComparison.OrdinalIgnoreCase))
                            descVendedor += remainingDesc;
                    }
                }

                decimal descGeneral = sale.Discount - (descMayoreo + descCategoria + descEspecial + descVendedor);
                if (descGeneral < 0) descGeneral = 0;

                summaryData.Add(new SaleExportSummary
                {
                    Folio = sale.Folio,
                    SaleDate = sale.SaleDate,
                    Total = sale.Total,
                    TotalDiscount = sale.Discount,
                    UserName = item.UserName,
                    PaymentSummary = sale.PaymentSummary,
                    WholesaleDiscount = Math.Round(descMayoreo, 2),
                    CategoryDiscount = Math.Round(descCategoria, 2),
                    SpecialDiscount = Math.Round(descEspecial, 2),
                    DealerDiscount = Math.Round(descVendedor, 2),
                    GeneralDiscount = Math.Round(descGeneral, 2)
                });

                // ==== HOJA 2: DETALLES DE TODAS LAS VENTAS ====
                // Título de la venta
                allDetails.Add(($"═══════════════════════════════════════════════════", ""));
                allDetails.Add(($"VENTA: {sale.Folio}", ""));
                allDetails.Add(($"═══════════════════════════════════════════════════", ""));
                allDetails.Add(("", ""));

                // Información general
                allDetails.Add(("Fecha", $"{sale.SaleDate:dd/MM/yyyy HH:mm}"));
                allDetails.Add(("Usuario", item.UserName));
                allDetails.Add(("Método de Pago", sale.PaymentSummary));
                allDetails.Add(("", ""));

                if (products.Any())
                {
                    allDetails.Add(("--- PRODUCTOS VENDIDOS ---", ""));
                    foreach (var product in products)
                    {
                        allDetails.Add(($"  • {product.ProductName}", ""));
                        allDetails.Add(($"    Cantidad", product.Quantity.ToString()));
                        allDetails.Add(($"    Precio Unitario", $"{product.FinalUnitPrice:C2}"));
                        if (product.TotalDiscountAmount > 0)
                        {
                            allDetails.Add(($"    Descuento", $"{product.TotalDiscountAmount:C2}"));
                            if (!string.IsNullOrEmpty(product.DiscountInfo))
                            {
                                allDetails.Add(($"    Tipo Descuento", product.DiscountInfo));
                            }
                        }
                        allDetails.Add(($"    Subtotal", $"{product.LineTotal:C2}"));
                        allDetails.Add(("", "")); // Espacio entre productos
                    }
                }

                // Totales
                allDetails.Add(("--- TOTALES ---", ""));
                allDetails.Add(("Subtotal", $"{sale.Subtotal:C2}"));
                if (sale.Discount > 0)
                {
                    allDetails.Add(("Descuento", $"{sale.Discount:C2}"));
                }
                allDetails.Add(("Total", $"{sale.Total:C2}"));
                allDetails.Add(("", ""));

                // Separador entre ventas (espacio en blanco más amplio)
                allDetails.Add(("", ""));
                allDetails.Add(("", ""));
                allDetails.Add(("", ""));
                allDetails.Add(("", ""));
            }

            var detailColumns = new List<ExportColumn<(string Label, object Value)>>
            {
                new() { Header = "Campo", ValueSelector = d => d.Label, Width = 40 },
                new() { Header = "Valor", ValueSelector = d => d.Value, Width = 25 }
            };

            // Crear hoja 1 usando la nueva data sumaria
            var summaryColumns = new List<ExportColumn<SaleExportSummary>>
            {
                new() { Header = "Folio", ValueSelector = i => i.Folio, Width = 22 },
                new() { Header = "Fecha/Hora", ValueSelector = i => i.SaleDate, Format = "dd/MM/yyyy HH:mm", Width = 20 },
                new() { Header = "Usuario", ValueSelector = i => i.UserName, Width = 20 },
                new() { Header = "Método de Pago", ValueSelector = i => i.PaymentSummary, Width = 20 },
                new() { Header = "Total", ValueSelector = i => i.Total, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Desc. Mayoreo", ValueSelector = i => i.WholesaleDiscount, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Desc. Categoría", ValueSelector = i => i.CategoryDiscount, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Desc. Especial", ValueSelector = i => i.SpecialDiscount, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Desc. Vendedor", ValueSelector = i => i.DealerDiscount, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Desc. General", ValueSelector = i => i.GeneralDiscount, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Total Descuento", ValueSelector = i => i.TotalDiscount, Format = "$#,##0.00", Width = 15 }
            };

            sheets.Insert(0, exportService.CreateSheetData(
                summaryData,
                summaryColumns,
                "Resumen",
                "Resumen de Ventas"));

            sheets.Add(exportService.CreateSheetData(
                allDetails,
                detailColumns,
                "Detalles",
                "Detalles de Todas las Ventas"));

            return await Task.FromResult(sheets);
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}


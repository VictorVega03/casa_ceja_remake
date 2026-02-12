using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Helpers;


namespace CasaCejaRemake.ViewModels.POS
{
    public enum ListFilterType
    {
        All = 0,
        Credits = 1,
        Layaways = 2
    }

    public enum ListFilterStatus
    {
        All = 0,
        Pending = 1,
        Paid = 2,
        Overdue = 3
    }

    public class CreditLayawayListItemWrapper
    {
        public object Item { get; set; } = null!;
        public bool IsCredit { get; set; }
        public string Type => IsCredit ? "Credito" : "Apartado";
        public string CustomerName { get; set; } = string.Empty;

        public string Folio => IsCredit 
            ? ((Credit)Item).Folio 
            : ((Layaway)Item).Folio;

        public decimal Total => IsCredit 
            ? ((Credit)Item).Total 
            : ((Layaway)Item).Total;

        public decimal TotalPaid => IsCredit 
            ? ((Credit)Item).TotalPaid 
            : ((Layaway)Item).TotalPaid;

        public decimal RemainingBalance => IsCredit 
            ? ((Credit)Item).RemainingBalance 
            : ((Layaway)Item).RemainingBalance;

        public string StatusName => IsCredit 
            ? ((Credit)Item).StatusName 
            : ((Layaway)Item).StatusName;

        public string StatusColor => IsCredit 
            ? ((Credit)Item).GetStatusColor() 
            : ((Layaway)Item).GetStatusColor();

        public DateTime CreatedDate => IsCredit 
            ? ((Credit)Item).CreditDate 
            : ((Layaway)Item).LayawayDate;

        public int Id => IsCredit 
            ? ((Credit)Item).Id 
            : ((Layaway)Item).Id;
    }

    public partial class CreditsLayawaysListViewModel : ViewModelBase
    {
        private readonly CreditService _creditService;
        private readonly LayawayService _layawayService;
        private readonly CustomerService _customerService;
        private readonly int _branchId;
        private ObservableCollection<CreditLayawayListItemWrapper> _allItems = new();

        [ObservableProperty]
        private ListFilterType _filterType = ListFilterType.All;

        [ObservableProperty]
        private ListFilterStatus _filterStatus = ListFilterStatus.Pending;

        [ObservableProperty]
        private CreditLayawayListItemWrapper? _selectedItem;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<CreditLayawayListItemWrapper> Items { get; } = new();

        public bool HasSelectedItem => SelectedItem != null;

        public event EventHandler<CreditLayawayListItemWrapper>? ItemSelected;
        public event EventHandler? CloseRequested;
        public event EventHandler? ExportRequested;

        public CreditService CreditServiceInstance => _creditService;
        public LayawayService LayawayServiceInstance => _layawayService;
        public CustomerService CustomerServiceInstance => _customerService;

        public string FilterDescription => $"{GetFilterTypeName(FilterType)} - {GetFilterStatusName(FilterStatus)}";

        public CreditsLayawaysListViewModel(
            CreditService creditService, 
            LayawayService layawayService,
            CustomerService customerService,
            int branchId)
        {
            _creditService = creditService;
            _layawayService = layawayService;
            _customerService = customerService;
            _branchId = branchId;
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
                _allItems.Clear();

                if (FilterType == ListFilterType.All || FilterType == ListFilterType.Credits)
                {
                    int? statusFilter = FilterStatus == ListFilterStatus.All ? null : (int)FilterStatus;
                    var credits = await _creditService.SearchAsync(null, statusFilter, _branchId);

                    foreach (var credit in credits)
                    {
                        var customer = await _customerService.GetByIdAsync(credit.CustomerId);
                        _allItems.Add(new CreditLayawayListItemWrapper
                        {
                            Item = credit,
                            IsCredit = true,
                            CustomerName = customer?.Name ?? "N/A"
                        });
                    }
                }

                if (FilterType == ListFilterType.All || FilterType == ListFilterType.Layaways)
                {
                    int? statusFilter = FilterStatus == ListFilterStatus.All ? null : (int)FilterStatus;
                    var layaways = await _layawayService.SearchAsync(null, statusFilter, _branchId);

                    foreach (var layaway in layaways)
                    {
                        var customer = await _customerService.GetByIdAsync(layaway.CustomerId);
                        _allItems.Add(new CreditLayawayListItemWrapper
                        {
                            Item = layaway,
                            IsCredit = false,
                            CustomerName = customer?.Name ?? "N/A"
                        });
                    }
                }

                var sorted = _allItems.OrderByDescending(i => i.CreatedDate).ToList();
                _allItems.Clear();
                foreach (var item in sorted)
                {
                    _allItems.Add(item);
                }

                ApplySearch();
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

        private void ApplySearch()
        {
            Items.Clear();

            var filtered = _allItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLower();
                filtered = filtered.Where(item =>
                    item.Folio.ToLower().Contains(search) ||
                    item.CustomerName.ToLower().Contains(search));
            }

            foreach (var item in filtered)
            {
                Items.Add(item);
            }

            TotalCount = Items.Count;
            StatusMessage = $"{TotalCount} registro(s) encontrado(s)";
        }

        [RelayCommand]
        private void ExecuteSearch()
        {
            ApplySearch();
        }

        partial void OnFilterTypeChanged(ListFilterType value)
        {
            _ = LoadDataAsync();
            OnPropertyChanged(nameof(FilterDescription));
        }

        partial void OnFilterStatusChanged(ListFilterStatus value)
        {
            _ = LoadDataAsync();
            OnPropertyChanged(nameof(FilterDescription));
        }

        partial void OnSelectedItemChanged(CreditLayawayListItemWrapper? value)
        {
            OnPropertyChanged(nameof(HasSelectedItem));
        }

        [RelayCommand]
        private void SetFilter(string filter)
        {
            switch (filter.ToUpper())
            {
                case "ALL":
                    FilterType = ListFilterType.All;
                    FilterStatus = ListFilterStatus.All; // Reset status to avoid impossible combinations
                    break;
                case "CREDITS":
                    FilterType = ListFilterType.Credits;
                    FilterStatus = ListFilterStatus.All; // Reset status
                    break;
                case "LAYAWAYS":
                    FilterType = ListFilterType.Layaways;
                    FilterStatus = ListFilterStatus.All; // Reset status
                    break;
                case "PENDING":
                    FilterStatus = ListFilterStatus.Pending;
                    break;
                case "PAID":
                    FilterStatus = ListFilterStatus.Paid;
                    break;
                case "OVERDUE":
                    FilterStatus = ListFilterStatus.Overdue;
                    break;
            }
        }

        [RelayCommand]
        private void SelectItem()
        {
            if (SelectedItem != null)
            {
                ItemSelected?.Invoke(this, SelectedItem);
            }
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            ExportRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Prepara los datos de exportación multi-hoja para créditos y apartados.
        /// Genera 4 hojas: Créditos, Apartados, Abonos Créditos, Abonos Apartados.
        /// </summary>
        public async Task<List<ExportSheetData>> PrepareMultiSheetExportAsync(ExportService exportService)
        {
            var sheets = new List<ExportSheetData>();

            // ===== HOJA 1: CRÉDITOS =====
            var creditItems = Items.Where(i => i.IsCredit).ToList();
            var creditColumns = new List<ExportColumn<CreditLayawayListItemWrapper>>
            {
                new() { Header = "Folio", ValueSelector = i => i.Folio, Width = 20 },
                new() { Header = "Cliente", ValueSelector = i => i.CustomerName, Width = 25 },
                new() { Header = "Total", ValueSelector = i => i.Total, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Pagado", ValueSelector = i => i.TotalPaid, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Saldo", ValueSelector = i => i.RemainingBalance, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Fecha", ValueSelector = i => i.CreatedDate, Format = "dd/MM/yyyy", Width = 15 },
                new() { Header = "Estado", ValueSelector = i => i.StatusName, Width = 15 }
            };
            sheets.Add(exportService.CreateSheetData(creditItems, creditColumns, "Creditos", "Reporte de Créditos"));

            // ===== HOJA 2: APARTADOS =====
            var layawayItems = Items.Where(i => !i.IsCredit).ToList();
            sheets.Add(exportService.CreateSheetData(layawayItems, creditColumns, "Apartados", "Reporte de Apartados"));

            // ===== HOJA 3: ABONOS A CRÉDITOS =====
            var allCreditPayments = new List<(CreditPayment Payment, string CustomerName, string CreditFolio)>();
            foreach (var item in creditItems)
            {
                var credit = (Credit)item.Item;
                var payments = await _creditService.GetPaymentsAsync(credit.Id);
                foreach (var p in payments)
                {
                    allCreditPayments.Add((p, item.CustomerName, credit.Folio));
                }
            }

            var creditPaymentColumns = new List<ExportColumn<(CreditPayment Payment, string CustomerName, string CreditFolio)>>
            {
                new() { Header = "Folio Crédito", ValueSelector = i => i.CreditFolio, Width = 20 },
                new() { Header = "Cliente", ValueSelector = i => i.CustomerName, Width = 25 },
                new() { Header = "Monto", ValueSelector = i => i.Payment.AmountPaid, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Fecha", ValueSelector = i => i.Payment.PaymentDate, Format = "dd/MM/yyyy HH:mm", Width = 20 },
                new() { Header = "Método Pago", ValueSelector = i => i.Payment.PaymentMethod, Width = 18 }
            };
            sheets.Add(exportService.CreateSheetData(allCreditPayments, creditPaymentColumns, "Abonos Creditos", "Abonos a Créditos"));

            // ===== HOJA 4: ABONOS A APARTADOS =====
            var allLayawayPayments = new List<(LayawayPayment Payment, string CustomerName, string LayawayFolio)>();
            foreach (var item in layawayItems)
            {
                var layaway = (Layaway)item.Item;
                var payments = await _layawayService.GetPaymentsAsync(layaway.Id);
                foreach (var p in payments)
                {
                    allLayawayPayments.Add((p, item.CustomerName, layaway.Folio));
                }
            }

            var layawayPaymentColumns = new List<ExportColumn<(LayawayPayment Payment, string CustomerName, string LayawayFolio)>>
            {
                new() { Header = "Folio Apartado", ValueSelector = i => i.LayawayFolio, Width = 20 },
                new() { Header = "Cliente", ValueSelector = i => i.CustomerName, Width = 25 },
                new() { Header = "Monto", ValueSelector = i => i.Payment.AmountPaid, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Fecha", ValueSelector = i => i.Payment.PaymentDate, Format = "dd/MM/yyyy HH:mm", Width = 20 },
                new() { Header = "Método Pago", ValueSelector = i => i.Payment.PaymentMethod, Width = 18 }
            };
            sheets.Add(exportService.CreateSheetData(allLayawayPayments, layawayPaymentColumns, "Abonos Apartados", "Abonos a Apartados"));

            return sheets;
        }

        public string GetFilterTypeName(ListFilterType type)
        {
            return type switch
            {
                ListFilterType.All => "Todos",
                ListFilterType.Credits => "Creditos",
                ListFilterType.Layaways => "Apartados",
                _ => "Desconocido"
            };
        }

        public string GetFilterStatusName(ListFilterStatus status)
        {
            return status switch
            {
                ListFilterStatus.All => "Todos",
                ListFilterStatus.Pending => "Pendiente",
                ListFilterStatus.Paid => "Pagado",
                ListFilterStatus.Overdue => "Vencido",
                _ => "Desconocido"
            };
        }
    }
}

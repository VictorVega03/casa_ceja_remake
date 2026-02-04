using System;
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

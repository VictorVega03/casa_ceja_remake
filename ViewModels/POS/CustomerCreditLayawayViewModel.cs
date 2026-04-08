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
    public partial class CustomerCreditsLayawaysViewModel : ViewModelBase
    {
        private readonly CreditService _creditService;
        private readonly LayawayService _layawayService;
        private readonly AuthService _authService;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerPhone = string.Empty;

        private Customer? _customer;

        [ObservableProperty]
        private bool _isCreditsMode = true;

        [ObservableProperty]
        private string _modeTitle = "Mis Creditos";

        // 0=Todos, 1=Pendientes, 2=Pagados, 3=Vencidos
        [ObservableProperty]
        private int _selectedStatusFilter = 0;

        [ObservableProperty]
        private Credit? _selectedCredit;

        // Raw unfiltered data
        private List<Credit> _allCredits = new();
        private List<Layaway> _allLayaways = new();

        // Filtered collections shown in DataGrids
        public ObservableCollection<Credit> Credits { get; } = new();

        [ObservableProperty]
        private Layaway? _selectedLayaway;

        public ObservableCollection<Layaway> Layaways { get; } = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public event EventHandler<Credit>? AddPaymentToCredit;
        public event EventHandler<Layaway>? AddPaymentToLayaway;
        public event EventHandler<Layaway>? DeliverLayaway;
        public event EventHandler<Credit>? PrintCredit;
        public event EventHandler<Layaway>? PrintLayaway;
        public event EventHandler? CloseRequested;
        public event EventHandler? Cancelled;

        // Computed properties
        public string WindowTitle => IsCreditsMode ? "Creditos del Cliente" : "Apartados del Cliente";
        public string HeaderTitle => IsCreditsMode ? "MIS CREDITOS" : "MIS APARTADOS";
        public string HeaderColor => IsCreditsMode ? "#4CAF50" : "#2196F3";

        // Counts based on ALL records (for badge counters on filter buttons)
        public int TotalAllCount => IsCreditsMode ? _allCredits.Count : _allLayaways.Count;
        public int TotalPendingCount => IsCreditsMode
            ? _allCredits.Count(c => c.Status == 1)
            : _allLayaways.Count(l => l.Status == 1);
        public int TotalPaidCount => IsCreditsMode
            ? _allCredits.Count(c => c.Status == 2)
            : _allLayaways.Count(l => l.Status == 2);
        public int TotalOverdueCount => IsCreditsMode
            ? _allCredits.Count(c => c.Status == 3)
            : _allLayaways.Count(l => l.Status == 3);

        // Summary card counts (from filtered view)
        public int ItemCount => IsCreditsMode ? Credits.Count : Layaways.Count;
        public int PendingCount => IsCreditsMode
            ? Credits.Count(c => c.Status == 1 || c.Status == 3)
            : Layaways.Count(l => l.Status == 1 || l.Status == 3);
        public int PaidCount => IsCreditsMode
            ? Credits.Count(c => c.Status == 2)
            : Layaways.Count(l => l.Status == 2);

        // Totals (from filtered view)
        public decimal TotalPending => IsCreditsMode
            ? _allCredits.Where(c => c.Status == 1).Sum(c => c.RemainingBalance)
            : _allLayaways.Where(l => l.Status == 1).Sum(l => l.RemainingBalance);

        public decimal TotalDebt => IsCreditsMode
            ? Credits.Sum(c => c.Total)
            : Layaways.Sum(l => l.Total);

        public decimal TotalPaidSum => IsCreditsMode
            ? Credits.Sum(c => c.TotalPaid)
            : Layaways.Sum(l => l.TotalPaid);

        public decimal TotalPendingBalance => TotalDebt - TotalPaidSum;

        public bool HasSelection => IsCreditsMode ? SelectedCredit != null : SelectedLayaway != null;
        public bool CanAddPayment => IsCreditsMode
            ? (SelectedCredit?.CanAddPayment() ?? false)
            : (SelectedLayaway?.CanAddPayment() ?? false);
        public bool CanDeliver => !IsCreditsMode && (SelectedLayaway?.CanDeliver ?? false);

        public int TotalRecords => IsCreditsMode ? Credits.Count : Layaways.Count;

        // ── Indicadores activos de tipo (Créditos / Apartados) ──────────────
        public bool FilterCreditsActive => IsCreditsMode;
        public bool FilterLayawaysActive => !IsCreditsMode;

        // ── Indicadores activos de estado ────────────────────────────────────
        public bool FilterAllActive => SelectedStatusFilter == 0;
        public bool FilterPendingActive => SelectedStatusFilter == 1;
        public bool FilterPaidActive => SelectedStatusFilter == 2;
        public bool FilterOverdueActive => SelectedStatusFilter == 3;

        // Filter button background colors (kept for backwards-compat, indicator bars used in AXAML)
        public string FilterAllBg => SelectedStatusFilter == 0 ? "#4CAF50" : "#2D2D2D";
        public string FilterPendingBg => SelectedStatusFilter == 1 ? "#FF9800" : "#2D2D2D";
        public string FilterPaidBg => SelectedStatusFilter == 2 ? "#2196F3" : "#2D2D2D";
        public string FilterOverdueBg => SelectedStatusFilter == 3 ? "#F44336" : "#2D2D2D";

        // Filter button text colors
        public string FilterAllFg => SelectedStatusFilter == 0 ? "White" : "#AAAAAA";
        public string FilterPendingFg => SelectedStatusFilter == 1 ? "White" : "#AAAAAA";
        public string FilterPaidFg => SelectedStatusFilter == 2 ? "White" : "#AAAAAA";
        public string FilterOverdueFg => SelectedStatusFilter == 3 ? "White" : "#AAAAAA";


        public CustomerCreditsLayawaysViewModel(
            CreditService creditService,
            LayawayService layawayService,
            AuthService authService)
        {
            _creditService = creditService;
            _layawayService = layawayService;
            _authService = authService;
        }

        public void SetCustomerAndMode(Customer customer, bool isCreditsMode)
        {
            _customer = customer;
            CustomerName = customer.Name;
            CustomerPhone = customer.Phone;
            IsCreditsMode = isCreditsMode;
            ModeTitle = isCreditsMode ? "Mis Creditos" : "Mis Apartados";
        }

        partial void OnSelectedCreditChanged(Credit? value)
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanAddPayment));
        }

        partial void OnSelectedLayawayChanged(Layaway? value)
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanAddPayment));
            OnPropertyChanged(nameof(CanDeliver));
        }

        partial void OnIsCreditsModeChanged(bool value)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderColor));
            OnPropertyChanged(nameof(FilterCreditsActive));
            OnPropertyChanged(nameof(FilterLayawaysActive));
            NotifyComputedProperties();
        }

        partial void OnSelectedStatusFilterChanged(int value)
        {
            OnPropertyChanged(nameof(FilterAllActive));
            OnPropertyChanged(nameof(FilterPendingActive));
            OnPropertyChanged(nameof(FilterPaidActive));
            OnPropertyChanged(nameof(FilterOverdueActive));
            OnPropertyChanged(nameof(FilterAllBg));
            OnPropertyChanged(nameof(FilterPendingBg));
            OnPropertyChanged(nameof(FilterPaidBg));
            OnPropertyChanged(nameof(FilterOverdueBg));
            OnPropertyChanged(nameof(FilterAllFg));
            OnPropertyChanged(nameof(FilterPendingFg));
            OnPropertyChanged(nameof(FilterPaidFg));
            OnPropertyChanged(nameof(FilterOverdueFg));
            ApplyFilter();
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (_customer == null) return;

            try
            {
                IsLoading = true;

                if (IsCreditsMode)
                    await LoadCreditsAsync();
                else
                    await LoadLayawaysAsync();
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

        [RelayCommand]
        private void SetFilter(string? status)
        {
            if (int.TryParse(status, out var parsed))
                SelectedStatusFilter = parsed;
        }

        /// <summary>Cambia entre modo Créditos y Apartados.</summary>
        [RelayCommand]
        private async Task SetMode(string? mode)
        {
            if (mode == null) return;
            var wantsCredits = mode == "credits";
            if (IsCreditsMode == wantsCredits) return;

            IsCreditsMode = wantsCredits;
            ModeTitle = wantsCredits ? "Mis Creditos" : "Mis Apartados";
            SelectedStatusFilter = 0; // Reiniciar filtro de estado al cambiar de tipo
            await LoadDataCommand.ExecuteAsync(null);
        }

        private async Task LoadCreditsAsync()
        {
            var all = await _creditService.GetAllByCustomerAsync(_customer!.Id);

            // Actualizar status de vencidos
            foreach (var credit in all.Where(c => c.Status == 1 && c.IsOverdue))
                await _creditService.UpdateStatusAsync(credit.Id);

            // Recargar después de actualizar estados
            _allCredits = await _creditService.GetAllByCustomerAsync(_customer!.Id);

            ApplyFilter();
            StatusMessage = $"{Credits.Count} credito(s) mostrado(s) de {_allCredits.Count} total";
            NotifyComputedProperties();
        }

        private async Task LoadLayawaysAsync()
        {
            var all = await _layawayService.GetAllByCustomerAsync(_customer!.Id);

            // Actualizar status de vencidos
            foreach (var layaway in all.Where(l => l.Status == 1 && l.IsExpired))
                await _layawayService.UpdateStatusAsync(layaway.Id);

            // Recargar después de actualizar estados
            _allLayaways = await _layawayService.GetAllByCustomerAsync(_customer!.Id);

            ApplyFilter();
            StatusMessage = $"{Layaways.Count} apartado(s) mostrado(s) de {_allLayaways.Count} total";
            NotifyComputedProperties();
        }

        private void ApplyFilter()
        {
            if (IsCreditsMode)
            {
                var filtered = SelectedStatusFilter switch
                {
                    1 => _allCredits.Where(c => c.Status == 1).ToList(),
                    2 => _allCredits.Where(c => c.Status == 2).ToList(),
                    3 => _allCredits.Where(c => c.Status == 3).ToList(),
                    _ => _allCredits
                };

                Credits.Clear();
                foreach (var c in filtered)
                    Credits.Add(c);
            }
            else
            {
                var filtered = SelectedStatusFilter switch
                {
                    1 => _allLayaways.Where(l => l.Status == 1).ToList(),
                    2 => _allLayaways.Where(l => l.Status == 2).ToList(),
                    3 => _allLayaways.Where(l => l.Status == 3).ToList(),
                    _ => _allLayaways
                };

                Layaways.Clear();
                foreach (var l in filtered)
                    Layaways.Add(l);
            }

            NotifyComputedProperties();
        }

        private void NotifyComputedProperties()
        {
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(PaidCount));
            OnPropertyChanged(nameof(TotalAllCount));
            OnPropertyChanged(nameof(TotalPendingCount));
            OnPropertyChanged(nameof(TotalPaidCount));
            OnPropertyChanged(nameof(TotalOverdueCount));
            OnPropertyChanged(nameof(TotalPending));
            OnPropertyChanged(nameof(TotalDebt));
            OnPropertyChanged(nameof(TotalPaidSum));
            OnPropertyChanged(nameof(TotalPendingBalance));
            OnPropertyChanged(nameof(TotalRecords));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanAddPayment));
            OnPropertyChanged(nameof(CanDeliver));
        }

        [RelayCommand]
        private void AddPayment()
        {
            if (IsCreditsMode)
            {
                if (SelectedCredit == null)
                {
                    StatusMessage = "Seleccione un credito";
                    return;
                }

                AddPaymentToCredit?.Invoke(this, SelectedCredit);
            }
            else
            {
                if (SelectedLayaway == null)
                {
                    StatusMessage = "Seleccione un apartado";
                    return;
                }

                AddPaymentToLayaway?.Invoke(this, SelectedLayaway);
            }
        }

        [RelayCommand]
        private void Deliver()
        {
            if (IsCreditsMode)
            {
                StatusMessage = "La entrega solo aplica para apartados";
                return;
            }

            if (SelectedLayaway == null)
            {
                StatusMessage = "Seleccione un apartado";
                return;
            }

            if (!SelectedLayaway.CanDeliver)
            {
                StatusMessage = "Este apartado no puede ser entregado (saldo pendiente o ya entregado)";
                return;
            }

            DeliverLayaway?.Invoke(this, SelectedLayaway);
        }

        [RelayCommand]
        private void Print()
        {
            if (IsCreditsMode)
            {
                if (SelectedCredit == null)
                {
                    StatusMessage = "Seleccione un credito";
                    return;
                }

                PrintCredit?.Invoke(this, SelectedCredit);
            }
            else
            {
                if (SelectedLayaway == null)
                {
                    StatusMessage = "Seleccione un apartado";
                    return;
                }

                PrintLayaway?.Invoke(this, SelectedLayaway);
            }
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        public void HandleKeyPress(string key)
        {
            switch (key.ToUpper())
            {
                case "ENTER":
                    AddPayment();
                    break;
                case "F6":
                    if (!IsCreditsMode) Deliver();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }
    }
}

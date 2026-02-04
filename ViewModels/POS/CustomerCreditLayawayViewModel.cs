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

        [ObservableProperty]
        private Credit? _selectedCredit;

        public ObservableCollection<Credit> Credits { get; } = new();

        [ObservableProperty]
        private Layaway? _selectedLayaway;

        public ObservableCollection<Layaway> Layaways { get; } = new();

        // Estado
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

        // Computed properties for view binding
        public string WindowTitle => IsCreditsMode ? "Creditos del Cliente" : "Apartados del Cliente";
        public string HeaderTitle => IsCreditsMode ? "MIS CREDITOS" : "MIS APARTADOS";
        public string HeaderColor => IsCreditsMode ? "#4CAF50" : "#2196F3";
        
        // Counts
        public int ItemCount => IsCreditsMode ? Credits.Count : Layaways.Count;
        public int PendingCount => IsCreditsMode 
            ? Credits.Count(c => c.Status == 1 || c.Status == 3) 
            : Layaways.Count(l => l.Status == 1 || l.Status == 3);
        public int PaidCount => IsCreditsMode 
            ? Credits.Count(c => c.Status == 2) 
            : Layaways.Count(l => l.Status == 2);
        
        // Totals
        public decimal TotalPending => IsCreditsMode 
            ? Credits.Where(c => c.Status == 1 || c.Status == 3).Sum(c => c.RemainingBalance)
            : Layaways.Where(l => l.Status == 1 || l.Status == 3).Sum(l => l.RemainingBalance);
        
        public decimal TotalDebt => IsCreditsMode 
            ? Credits.Sum(c => c.Total)
            : Layaways.Sum(l => l.Total);
        
        public decimal TotalPaidSum => IsCreditsMode 
            ? Credits.Sum(c => c.TotalPaid)
            : Layaways.Sum(l => l.TotalPaid);
        
        public decimal TotalPendingBalance => TotalDebt - TotalPaidSum;
        
        // Selection state
        public bool HasSelection => IsCreditsMode ? SelectedCredit != null : SelectedLayaway != null;
        public bool CanAddPayment => IsCreditsMode 
            ? (SelectedCredit?.CanAddPayment() ?? false) 
            : (SelectedLayaway?.CanAddPayment() ?? false);
        public bool CanDeliver => !IsCreditsMode && (SelectedLayaway?.CanDeliver ?? false);
        
        public int TotalRecords => IsCreditsMode ? Credits.Count : Layaways.Count;

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
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(PaidCount));
            OnPropertyChanged(nameof(TotalPending));
            OnPropertyChanged(nameof(TotalRecords));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanAddPayment));
            OnPropertyChanged(nameof(CanDeliver));
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
                {
                    await LoadCreditsAsync();
                }
                else
                {
                    await LoadLayawaysAsync();
                }
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

        private async Task LoadCreditsAsync()
        {
            // Obtener los datos primero
            var credits = await _creditService.GetPendingByCustomerAsync(_customer!.Id);
            
            // Limpiar y agregar todos de una vez para evitar múltiples notificaciones
            Credits.Clear();
            foreach (var credit in credits)
            {
                Credits.Add(credit);
            }

            StatusMessage = $"{Credits.Count} credito(s) encontrado(s)";
            NotifyComputedProperties();
        }

        private async Task LoadLayawaysAsync()
        {
            // Obtener los datos primero
            var layaways = await _layawayService.GetPendingByCustomerAsync(_customer!.Id);
            
            // Limpiar y agregar todos de una vez para evitar múltiples notificaciones
            Layaways.Clear();
            foreach (var layaway in layaways)
            {
                Layaways.Add(layaway);
            }

            StatusMessage = $"{Layaways.Count} apartado(s) encontrado(s)";
            NotifyComputedProperties();
        }

        private void NotifyComputedProperties()
        {
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(PaidCount));
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

                if (!SelectedCredit.CanAddPayment())
                {
                    StatusMessage = "Este credito no acepta mas pagos";
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

                if (!SelectedLayaway.CanAddPayment())
                {
                    StatusMessage = "Este apartado no acepta mas pagos";
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

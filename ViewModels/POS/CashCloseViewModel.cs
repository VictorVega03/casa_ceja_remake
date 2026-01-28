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
    /// ViewModel para la vista de corte de caja.
    /// </summary>
    public partial class CashCloseViewModel : ViewModelBase
    {
        private readonly CashCloseService _cashCloseService;
        private readonly AuthService _authService;
        private readonly CashClose _currentCashClose;

        // ========== CAMPOS DE APERTURA ==========
        [ObservableProperty]
        private string _folio = string.Empty;

        [ObservableProperty]
        private decimal _openingAmount;

        [ObservableProperty]
        private DateTime _openingDate;

        [ObservableProperty]
        private DateTime _closeDate;

        [ObservableProperty]
        private string _userName = string.Empty;

        // ========== TOTALES POR MÉTODO DE PAGO ==========
        [ObservableProperty]
        private decimal _totalCash;

        [ObservableProperty]
        private decimal _totalDebit;

        [ObservableProperty]
        private decimal _totalCredit;

        [ObservableProperty]
        private decimal _totalTransfer;

        [ObservableProperty]
        private decimal _totalCheck;

        // ========== APARTADOS Y CRÉDITOS ==========
        [ObservableProperty]
        private decimal _layawayCash;

        [ObservableProperty]
        private decimal _layawayTotal;

        [ObservableProperty]
        private decimal _creditPaymentsCash;

        [ObservableProperty]
        private decimal _creditPaymentsTotal;

        // ========== GASTOS E INGRESOS ==========
        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _totalIncome;

        public ObservableCollection<CashMovement> Expenses { get; } = new();
        public ObservableCollection<CashMovement> Incomes { get; } = new();

        // ========== TOTALES Y DIFERENCIA ==========
        [ObservableProperty]
        private decimal _expectedAmount;

        [ObservableProperty]
        private decimal _declaredAmount;

        [ObservableProperty]
        private int _salesCount;

        public decimal Difference => DeclaredAmount - ExpectedAmount;

        public string DifferenceText
        {
            get
            {
                if (Difference > 0)
                    return $"+${Difference:F2} (Sobrante)";
                else if (Difference < 0)
                    return $"-${Math.Abs(Difference):F2} (Faltante)";
                else
                    return "$0.00 (Exacto)";
            }
        }

        public bool HasSurplus => Difference > 0;
        public bool HasShortage => Difference < 0;
        public bool IsBalanced => Difference == 0;

        // ========== ESTADO ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // ========== EVENTOS ==========
        public event EventHandler<CashClose>? CloseCompleted;
        public event EventHandler? Cancelled;

        public CashCloseViewModel(CashCloseService cashCloseService, AuthService authService, CashClose currentCashClose)
        {
            _cashCloseService = cashCloseService;
            _authService = authService;
            _currentCashClose = currentCashClose;

            // Inicializar datos básicos
            Folio = currentCashClose.Folio;
            OpeningAmount = currentCashClose.OpeningCash;
            OpeningDate = currentCashClose.OpeningDate;
            CloseDate = DateTime.Now;
            UserName = authService.CurrentUserName ?? "Usuario";
        }

        partial void OnDeclaredAmountChanged(decimal value)
        {
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(DifferenceText));
            OnPropertyChanged(nameof(HasSurplus));
            OnPropertyChanged(nameof(HasShortage));
            OnPropertyChanged(nameof(IsBalanced));
        }

        /// <summary>
        /// Carga los datos del corte (totales calculados).
        /// </summary>
        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Calcular totales
                var totals = await _cashCloseService.CalculateTotalsAsync(_currentCashClose.Id, _currentCashClose.OpeningDate);

                TotalCash = totals.TotalCash;
                TotalDebit = totals.TotalDebit;
                TotalCredit = totals.TotalCredit;
                TotalTransfer = totals.TotalTransfer;
                TotalCheck = totals.TotalCheck;
                LayawayCash = totals.LayawayCash;
                LayawayTotal = totals.LayawayTotal;
                CreditPaymentsCash = totals.CreditPaymentsCash;
                CreditPaymentsTotal = totals.CreditPaymentsTotal;
                TotalExpenses = totals.TotalExpenses;
                TotalIncome = totals.TotalIncome;
                SalesCount = totals.SalesCount;

                // Calcular efectivo esperado
                ExpectedAmount = OpeningAmount + TotalCash + LayawayCash + CreditPaymentsCash + TotalIncome - TotalExpenses;
                
                // Por defecto, el monto declarado es el esperado
                DeclaredAmount = ExpectedAmount;

                // Cargar movimientos
                var movements = await _cashCloseService.GetMovementsAsync(_currentCashClose.Id);
                
                Expenses.Clear();
                Incomes.Clear();
                
                foreach (var movement in movements)
                {
                    if (movement.IsExpense)
                        Expenses.Add(movement);
                    else
                        Incomes.Add(movement);
                }

                Console.WriteLine($"[CashCloseVM] Datos cargados: {SalesCount} ventas, Esperado=${ExpectedAmount}");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar datos: {ex.Message}";
                Console.WriteLine($"[CashCloseVM] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Confirma el corte de caja.
        /// </summary>
        [RelayCommand]
        private async Task ConfirmAsync()
        {
            if (DeclaredAmount < 0)
            {
                ErrorMessage = "El monto declarado no puede ser negativo";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _cashCloseService.CloseCashAsync(_currentCashClose, DeclaredAmount);

                if (result.Success && result.CashClose != null)
                {
                    Console.WriteLine($"[CashCloseVM] Corte completado: {result.CashClose.Folio}");
                    CloseCompleted?.Invoke(this, result.CashClose);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Error al cerrar caja";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cancela el corte.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}

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
    /// 
    /// CONCEPTOS IMPORTANTES (de reglas_corte.md):
    /// 
    /// 1. TOTAL DEL CORTE (Productividad):
    ///    = Ventas Directas + Créditos CREADOS + Apartados CREADOS
    ///    Esto mide la productividad del turno.
    /// 
    /// 2. EFECTIVO ESPERADO (Caja física):
    ///    = Fondo + Ventas Efectivo + Abonos Créditos (efectivo) + Abonos Apartados (efectivo) + Ingresos - Gastos
    ///    NOTA: El cambio NO se resta porque ya está implícito al usar sale.Total
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

        // ========== VENTAS DIRECTAS POR MÉTODO DE PAGO ==========
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

        /// <summary>
        /// Total de todas las ventas directas (suma de todos los métodos de pago).
        /// </summary>
        public decimal TotalSalesDirectas => TotalCash + TotalDebit + TotalCredit + TotalTransfer + TotalCheck;

        // ========== CRÉDITOS ===========
        /// <summary>
        /// Total de CRÉDITOS CREADOS durante el turno (suma al Total del Corte).
        /// </summary>
        [ObservableProperty]
        private decimal _creditTotalCreated;

        /// <summary>
        /// Efectivo recibido por abonos/enganches de créditos (suma al Efectivo Esperado).
        /// </summary>
        [ObservableProperty]
        private decimal _creditCash;

        [ObservableProperty]
        private int _creditCount;

        // ========== APARTADOS ===========
        /// <summary>
        /// Total de APARTADOS CREADOS durante el turno (suma al Total del Corte).
        /// </summary>
        [ObservableProperty]
        private decimal _layawayTotalCreated;

        /// <summary>
        /// Efectivo recibido por abonos de apartados (suma al Efectivo Esperado).
        /// </summary>
        [ObservableProperty]
        private decimal _layawayCash;

        [ObservableProperty]
        private int _layawayCount;

        // ========== GASTOS E INGRESOS ==========
        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _totalIncome;

        public ObservableCollection<CashMovement> Expenses { get; } = new();
        public ObservableCollection<CashMovement> Incomes { get; } = new();

        // ========== TOTALES Y DIFERENCIA ==========
        /// <summary>
        /// Total del Corte = Productividad del turno
        /// = Ventas Directas + Créditos Creados + Apartados Creados
        /// </summary>
        public decimal TotalDelCorte => TotalSalesDirectas + CreditTotalCreated + LayawayTotalCreated;

        /// <summary>
        /// Total de efectivo que se espera en caja.
        /// = Fondo + Efectivo Ventas + Efectivo Abonos + Ingresos - Gastos
        /// </summary>
        [ObservableProperty]
        private decimal _expectedAmount;

        [ObservableProperty]
        private decimal _declaredAmount;

        [ObservableProperty]
        private int _salesCount;

        [ObservableProperty]
        private string _notes = string.Empty;

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
                // Calcular totales según reglas de negocio
                var totals = await _cashCloseService.CalculateTotalsAsync(_currentCashClose.Id, _currentCashClose.OpeningDate);

                // ==================== VENTAS DIRECTAS ====================
                TotalCash = totals.TotalCash;
                TotalDebit = totals.TotalDebitCard;
                TotalCredit = totals.TotalCreditCard;
                TotalTransfer = totals.TotalTransfers;
                TotalCheck = totals.TotalChecks;
                SalesCount = totals.SalesCount;
                
                // ==================== CRÉDITOS ====================
                CreditTotalCreated = totals.CreditTotalCreated;  // Total para productividad
                CreditCash = totals.CreditCash;                   // Efectivo recibido
                CreditCount = totals.CreditCount;
                
                // ==================== APARTADOS ====================
                LayawayTotalCreated = totals.LayawayTotalCreated; // Total para productividad
                LayawayCash = totals.LayawayCash;                  // Efectivo recibido
                LayawayCount = totals.LayawayCount;
                
                // ==================== MOVIMIENTOS ====================
                TotalExpenses = totals.TotalExpenses;
                TotalIncome = totals.TotalIncome;

                // ==================== EFECTIVO ESPERADO ====================
                // FÓRMULA (de reglas_corte.md):
                // = Fondo + Efectivo Ventas + Efectivo Abonos Créditos + Efectivo Abonos Apartados + Ingresos - Gastos
                // NOTA: El cambio NO se resta porque ya está implícito al usar sale.Total
                ExpectedAmount = OpeningAmount 
                               + TotalCash           // Ventas en efectivo
                               + CreditCash          // Abonos/enganches créditos en efectivo
                               + LayawayCash         // Abonos apartados en efectivo
                               + TotalIncome         // Ingresos extra
                               - TotalExpenses;      // Gastos
                
                // Por defecto, el monto declarado es el esperado
                DeclaredAmount = ExpectedAmount;

                // Cargar movimientos para mostrar en la UI
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

                // Notificar propiedades calculadas
                OnPropertyChanged(nameof(TotalSalesDirectas));
                OnPropertyChanged(nameof(TotalDelCorte));

                Console.WriteLine($"[CashCloseVM] === DATOS CARGADOS ===");
                Console.WriteLine($"  Ventas directas: {SalesCount} ventas, ${TotalSalesDirectas}");
                Console.WriteLine($"    Efectivo: ${TotalCash}");
                Console.WriteLine($"    Débito: ${TotalDebit}");
                Console.WriteLine($"    Crédito: ${TotalCredit}");
                Console.WriteLine($"    Transfer: ${TotalTransfer}");
                Console.WriteLine($"  Créditos creados: {CreditCount}, Total: ${CreditTotalCreated}");
                Console.WriteLine($"    Efectivo abonos: ${CreditCash}");
                Console.WriteLine($"  Apartados creados: {LayawayCount}, Total: ${LayawayTotalCreated}");
                Console.WriteLine($"    Efectivo abonos: ${LayawayCash}");
                Console.WriteLine($"  ---");
                Console.WriteLine($"  TOTAL DEL CORTE (Productividad): ${TotalDelCorte}");
                Console.WriteLine($"  EFECTIVO ESPERADO: ${ExpectedAmount}");
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
                // Asignar las notas al corte actual antes de cerrar
                _currentCashClose.Notes = Notes;
                
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

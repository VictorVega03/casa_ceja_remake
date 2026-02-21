using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    /// <summary>
    /// Representa un movimiento de caja (gasto o ingreso) para mostrar en el detalle.
    /// </summary>
    public class CashMovementItem
    {
        public string Type { get; set; } = string.Empty;
        public string Concept { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TypeColor => Type == "Gasto" ? "#F44336" : "#4CAF50";
    }

    /// <summary>
    /// ViewModel para la vista de detalle de un corte de caja.
    /// </summary>
    public partial class CashCloseDetailViewModel : ViewModelBase
    {
        private readonly CashCloseService _cashCloseService;

        [ObservableProperty]
        private CashClose _cashClose = null!;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _branchName = string.Empty;

        [ObservableProperty]
        private List<CashMovementItem> _movements = new();

        [ObservableProperty]
        private bool _isLoading;

        // ============ PROPIEDADES DE SOLO LECTURA ============
        
        public string Folio => CashClose?.Folio ?? string.Empty;
        public DateTime OpeningDate => CashClose?.OpeningDate ?? DateTime.MinValue;
        public DateTime CloseDate => CashClose?.CloseDate ?? DateTime.MinValue;
        
        // Fondo de apertura
        public decimal OpeningCash => CashClose?.OpeningCash ?? 0;
        
        // Ventas directas por método
        public decimal TotalCash => CashClose?.TotalCash ?? 0;
        public decimal TotalDebitCard => CashClose?.TotalDebitCard ?? 0;
        public decimal TotalCreditCard => CashClose?.TotalCreditCard ?? 0;
        public decimal TotalTransfers => CashClose?.TotalTransfers ?? 0;
        public decimal TotalChecks => CashClose?.TotalChecks ?? 0;
        public decimal TotalSales => CashClose?.TotalSales ?? 0;
        
        // Créditos y apartados
        public decimal CreditTotalCreated => CashClose?.CreditTotalCreated ?? 0;
        public decimal LayawayTotalCreated => CashClose?.LayawayTotalCreated ?? 0;
        public decimal CreditCash => CashClose?.CreditCash ?? 0;
        public decimal LayawayCash => CashClose?.LayawayCash ?? 0;
        
        // Totales calculados
        public decimal TotalDelCorte => CashClose?.TotalDelCorte ?? 0;
        public decimal EfectivoTotal => CashClose?.EfectivoTotal ?? 0;
        public decimal TotalElectronicPayments => CashClose?.TotalElectronicPayments ?? 0;
        
        // Efectivo esperado y diferencia
        public decimal ExpectedCash => CashClose?.ExpectedCash ?? 0;
        public decimal Surplus => CashClose?.Surplus ?? 0;
        
        // Estado del corte
        public string SurplusStatus => Surplus switch
        {
            > 0 => "SOBRANTE",
            < 0 => "FALTANTE",
            _ => "CUADRADO"
        };

        public string SurplusColor => Surplus switch
        {
            > 0 => "#4CAF50", // Verde - sobrante
            < 0 => "#F44336", // Rojo - faltante
            _ => "#66BB6A"    // Verde claro - cuadrado
        };

        public string SurplusIcon => Surplus switch
        {
            > 0 => "↑",
            < 0 => "↓",
            _ => "✓"
        };

        // Duración del turno
        public string ShiftDuration
        {
            get
            {
                if (CashClose == null) return "N/A";
                var duration = CashClose.CloseDate - CashClose.OpeningDate;
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
        }

        // Notas
        public string Notes => CashClose?.Notes ?? string.Empty;
        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

        // Movimientos
        public decimal TotalExpenses { get; private set; }
        public decimal TotalIncome { get; private set; }
        public bool HasMovements => Movements.Count > 0;

        public event EventHandler? CloseRequested;
        public event EventHandler<(string Folio, string TicketText)>? PrintRequested;

        public CashCloseDetailViewModel(CashCloseService cashCloseService)
        {
            _cashCloseService = cashCloseService;
        }

        public async Task InitializeAsync(CashClose cashClose, string userName, string branchName)
        {
            IsLoading = true;

            try
            {
                CashClose = cashClose;
                UserName = userName;
                BranchName = branchName;

                // Cargar movimientos
                await LoadMovementsAsync();

                // Notificar cambios en todas las propiedades calculadas
                NotifyAllPropertiesChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMovementsAsync()
        {
            try
            {
                var movements = await _cashCloseService.GetMovementsAsync(CashClose.Id);
                
                Movements.Clear();
                TotalExpenses = 0;
                TotalIncome = 0;

                foreach (var movement in movements)
                {
                    Movements.Add(new CashMovementItem
                    {
                        Type = movement.IsExpense ? "Gasto" : "Ingreso",
                        Concept = movement.Concept,
                        Amount = movement.Amount
                    });

                    if (movement.IsExpense)
                        TotalExpenses += movement.Amount;
                    else
                        TotalIncome += movement.Amount;
                }

                OnPropertyChanged(nameof(Movements));
                OnPropertyChanged(nameof(HasMovements));
                OnPropertyChanged(nameof(TotalExpenses));
                OnPropertyChanged(nameof(TotalIncome));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseDetailViewModel] Error cargando movimientos: {ex.Message}");
            }
        }

        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(Folio));
            OnPropertyChanged(nameof(OpeningDate));
            OnPropertyChanged(nameof(CloseDate));
            OnPropertyChanged(nameof(OpeningCash));
            OnPropertyChanged(nameof(TotalCash));
            OnPropertyChanged(nameof(TotalDebitCard));
            OnPropertyChanged(nameof(TotalCreditCard));
            OnPropertyChanged(nameof(TotalTransfers));
            OnPropertyChanged(nameof(TotalChecks));
            OnPropertyChanged(nameof(TotalSales));
            OnPropertyChanged(nameof(CreditTotalCreated));
            OnPropertyChanged(nameof(LayawayTotalCreated));
            OnPropertyChanged(nameof(CreditCash));
            OnPropertyChanged(nameof(LayawayCash));
            OnPropertyChanged(nameof(TotalDelCorte));
            OnPropertyChanged(nameof(EfectivoTotal));
            OnPropertyChanged(nameof(TotalElectronicPayments));
            OnPropertyChanged(nameof(ExpectedCash));
            OnPropertyChanged(nameof(Surplus));
            OnPropertyChanged(nameof(SurplusStatus));
            OnPropertyChanged(nameof(SurplusColor));
            OnPropertyChanged(nameof(SurplusIcon));
            OnPropertyChanged(nameof(ShiftDuration));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(HasNotes));
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(BranchName));
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Print()
        {
            if (CashClose == null) return;
            try
            {
                var ticketService = new TicketService();
                var expenseList = new List<(string Concept, decimal Amount)>();
                var incomeList = new List<(string Concept, decimal Amount)>();
                foreach (var m in Movements)
                {
                    if (m.Type == "Gasto")
                        expenseList.Add((m.Concept, m.Amount));
                    else
                        incomeList.Add((m.Concept, m.Amount));
                }

                var ticketText = ticketService.GenerateCashCloseTicketText(
                    branchName: BranchName,
                    branchAddress: string.Empty,
                    branchPhone: string.Empty,
                    folio: CashClose.Folio,
                    userName: UserName,
                    openingDate: CashClose.OpeningDate,
                    closeDate: CashClose.CloseDate,
                    openingCash: CashClose.OpeningCash,
                    totalCash: CashClose.TotalCash,
                    totalDebit: CashClose.TotalDebitCard,
                    totalCredit: CashClose.TotalCreditCard,
                    totalTransfer: CashClose.TotalTransfers,
                    totalChecks: CashClose.TotalChecks,
                    layawayCash: CashClose.LayawayCash,
                    creditCash: CashClose.CreditCash,
                    creditTotalCreated: CashClose.CreditTotalCreated,
                    layawayTotalCreated: CashClose.LayawayTotalCreated,
                    totalExpenses: TotalExpenses,
                    totalIncome: TotalIncome,
                    expectedCash: CashClose.ExpectedCash,
                    declaredAmount: CashClose.ExpectedCash,
                    difference: CashClose.Surplus,
                    salesCount: 0,
                    expenses: expenseList,
                    incomes: incomeList);

                PrintRequested?.Invoke(this, (CashClose.Folio, ticketText));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseDetailViewModel] Error generando ticket: {ex.Message}");
            }
        }
    }
}

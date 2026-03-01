using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.ViewModels.POS
{
    /// <summary>
    /// ViewModel para agregar gastos o ingresos.
    /// </summary>
    public partial class CashMovementViewModel : ViewModelBase
    {
        private readonly ICashCloseService _cashCloseService;
        private readonly int _cashCloseId;
        private readonly int _userId;
        private readonly int _branchId;

        [ObservableProperty]
        private bool _isExpense = true;

        [ObservableProperty]
        private string _concept = string.Empty;

        [ObservableProperty]
        private decimal _amount;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Título del modal según el tipo preseleccionado.
        /// </summary>
        public string Title => IsExpense ? "Registrar Gasto" : "Registrar Ingreso";

        /// <summary>
        /// Evento cuando se confirma el movimiento.
        /// </summary>
        public event EventHandler<CashMovement>? MovementAdded;

        /// <summary>
        /// Evento cuando se cancela.
        /// </summary>
        public event EventHandler? Cancelled;

        public CashMovementViewModel(ICashCloseService cashCloseService, int cashCloseId, int userId, int branchId, bool isExpense = true)
        {
            _cashCloseService = cashCloseService;
            _cashCloseId = cashCloseId;
            _userId = userId;
            _branchId = branchId;
            _isExpense = isExpense;
        }

        partial void OnIsExpenseChanged(bool value)
        {
            OnPropertyChanged(nameof(Title));
        }

        [RelayCommand]
        private async Task ConfirmAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Concept))
            {
                ErrorMessage = "Ingrese un concepto";
                return;
            }

            if (Amount <= 0)
            {
                ErrorMessage = "El monto debe ser mayor a 0";
                return;
            }

            // Validar retiros contra efectivo disponible
            if (IsExpense)
            {
                Console.WriteLine($"[CashMovementVM] Validando retiro de ${Amount}...");
                
                var cashClose = await _cashCloseService.GetOpenCashAsync(_branchId);
                if (cashClose != null)
                {
                    Console.WriteLine($"[CashMovementVM] Caja abierta encontrada: {cashClose.Folio}, ID={cashClose.Id}");
                    
                    var totals = await _cashCloseService.CalculateTotalsAsync(cashClose.Id, cashClose.OpeningDate);
                    
                    // Calcular efectivo disponible actual
                    decimal availableCash = cashClose.OpeningCash + totals.TotalCash + 
                                           totals.LayawayCash + totals.CreditCash + 
                                           totals.TotalIncome - totals.TotalExpenses;

                    Console.WriteLine($"[CashMovementVM] Efectivo disponible: ${availableCash:N2}");
                    Console.WriteLine($"[CashMovementVM] Fondo: ${cashClose.OpeningCash}, Ventas: ${totals.TotalCash}, " +
                                    $"Abonos: ${totals.LayawayCash + totals.CreditCash}, " +
                                    $"Ingresos: ${totals.TotalIncome}, Gastos: ${totals.TotalExpenses}");

                    // No permitir retiros que dejen la caja en negativo
                    if (Amount > availableCash)
                    {
                        Console.WriteLine($"[CashMovementVM] RETIRO RECHAZADO: ${Amount} > ${availableCash}");
                        ErrorMessage = $"No hay suficiente efectivo en caja.\nDisponible: ${availableCash:N2}";
                        Console.WriteLine($"[CashMovementVM] ErrorMessage establecido: '{ErrorMessage}'");
                        Console.WriteLine($"[CashMovementVM] IsLoading: {IsLoading}");
                        return;
                    }


                    // Advertir si el retiro deja la caja vacía o casi vacía
                    if (Amount >= availableCash * 0.95m) // 95% o más del efectivo
                    {
                        Console.WriteLine($"[CashMovementVM] ADVERTENCIA: Retiro dejará caja con ${(availableCash - Amount):N2}");
                        ErrorMessage = $"ADVERTENCIA: Este retiro dejará la caja con ${(availableCash - Amount):N2}.\n¿Desea continuar? (Presione F5 nuevamente)";
                        
                        // Permitir confirmación doble
                        if (!_warningShown)
                        {
                            _warningShown = true;
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[CashMovementVM] ERROR: No se encontró caja abierta para branchId={_branchId}");
                }
            }

            IsLoading = true;
            try
            {
                var type = IsExpense ? "expense" : "income";
                var result = await _cashCloseService.AddMovementAsync(_cashCloseId, type, Concept, Amount, _userId);

                if (result.Success && result.Movement != null)
                {
                    Console.WriteLine($"[CashMovementVM] Movimiento agregado: {type} - {Concept} - ${Amount}");
                    MovementAdded?.Invoke(this, result.Movement);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Error al agregar movimiento";
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

        private bool _warningShown = false;

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}

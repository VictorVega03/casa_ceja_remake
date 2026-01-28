using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    /// <summary>
    /// ViewModel para agregar gastos o ingresos.
    /// </summary>
    public partial class CashMovementViewModel : ViewModelBase
    {
        private readonly CashCloseService _cashCloseService;
        private readonly int _cashCloseId;
        private readonly int _userId;

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

        public CashMovementViewModel(CashCloseService cashCloseService, int cashCloseId, int userId, bool isExpense = true)
        {
            _cashCloseService = cashCloseService;
            _cashCloseId = cashCloseId;
            _userId = userId;
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

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}

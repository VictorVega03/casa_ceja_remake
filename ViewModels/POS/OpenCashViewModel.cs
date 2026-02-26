using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    /// <summary>
    /// ViewModel para la vista de apertura de caja.
    /// </summary>
    public partial class OpenCashViewModel : ViewModelBase
    {
        private readonly CashCloseService _cashCloseService;
        private readonly AuthService _authService;
        private readonly int _branchId;

        [ObservableProperty]
        private decimal _openingAmount = 0;

        [ObservableProperty]
        private string _openingAmountString = "0.00";

        partial void OnOpeningAmountStringChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                OpeningAmount = 0;
            }
            else if (decimal.TryParse(value, out var parsedValue))
            {
                OpeningAmount = Math.Max(0, parsedValue);
            }
        }

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Evento que se dispara cuando la caja se abre exitosamente.
        /// </summary>
        public event EventHandler<CashClose>? CashOpened;

        /// <summary>
        /// Evento que se dispara cuando el usuario cancela.
        /// </summary>
        public event EventHandler? Cancelled;

        public OpenCashViewModel(CashCloseService cashCloseService, AuthService authService, int branchId)
        {
            _cashCloseService = cashCloseService;
            _authService = authService;
            _branchId = branchId;
        }

        /// <summary>
        /// Abre la caja con el fondo inicial especificado.
        /// </summary>
        [RelayCommand]
        private async Task OpenCashAsync()
        {
            ErrorMessage = string.Empty;

            if (OpeningAmount <= 0)
            {
                ErrorMessage = "El fondo de apertura debe ser mayor a $0.00";
                return;
            }

            IsLoading = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 0;
                var result = await _cashCloseService.OpenCashAsync(OpeningAmount, userId, _branchId);

                if (result.Success && result.CashClose != null)
                {
                    Console.WriteLine($"[OpenCashViewModel] Caja abierta exitosamente: {result.CashClose.Folio}");
                    CashOpened?.Invoke(this, result.CashClose);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Error desconocido al abrir caja";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenCashViewModel] Error: {ex.Message}");
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cancela la apertura de caja.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}

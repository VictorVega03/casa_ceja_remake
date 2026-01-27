using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class CreateCreditViewModel : ViewModelBase
    {
        private readonly CreditService _creditService;
        private readonly AuthService _authService;
        private readonly int _branchId;

        // Cliente (readonly)
        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerPhone = string.Empty;

        private Customer? _customer;

        // Configuracion
        [ObservableProperty]
        private int _monthsToPay = 3;

        [ObservableProperty]
        private decimal _initialPayment;

        [ObservableProperty]
        private PaymentMethod _paymentMethod = PaymentMethod.Efectivo;

        [ObservableProperty]
        private string _notes = string.Empty;

        // Totales calculados
        [ObservableProperty]
        private decimal _total;

        [ObservableProperty]
        private decimal _totalPaid;

        [ObservableProperty]
        private decimal _remainingBalance;

        [ObservableProperty]
        private DateTime _dueDate;

        // Estado
        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        // Productos
        public ObservableCollection<CartItem> CartItems { get; } = new();

        // Metodos de pago disponibles
        public ObservableCollection<PaymentMethod> PaymentMethods { get; } = new()
        {
            PaymentMethod.Efectivo,
            PaymentMethod.TarjetaDebito,
            PaymentMethod.TarjetaCredito,
            PaymentMethod.Transferencia
        };

        public event EventHandler<Credit>? CreditCreated;
        public event EventHandler? Cancelled;

        public CreateCreditViewModel(CreditService creditService, AuthService authService, int branchId)
        {
            _creditService = creditService;
            _authService = authService;
            _branchId = branchId;

            UpdateDueDate();
        }

        public void Initialize(Customer customer, System.Collections.Generic.List<CartItem> items)
        {
            _customer = customer;
            CustomerName = customer.Name;
            CustomerPhone = customer.Phone;

            CartItems.Clear();
            foreach (var item in items)
            {
                CartItems.Add(item);
            }

            Total = items.Sum(i => i.LineTotal);
            InitialPayment = 0;
            UpdateCalculations();
        }

        partial void OnMonthsToPayChanged(int value)
        {
            UpdateDueDate();
        }

        partial void OnInitialPaymentChanged(decimal value)
        {
            UpdateCalculations();
        }

        private void UpdateDueDate()
        {
            DueDate = DateTime.Now.AddMonths(MonthsToPay);
        }

        private void UpdateCalculations()
        {
            TotalPaid = InitialPayment;
            RemainingBalance = Total - InitialPayment;
        }

        [RelayCommand]
        private async Task ConfirmAsync()
        {
            // Validaciones
            if (_customer == null)
            {
                ShowError("No hay cliente seleccionado.");
                return;
            }

            if (!CartItems.Any())
            {
                ShowError("No hay productos en el carrito.");
                return;
            }

            if (MonthsToPay <= 0)
            {
                ShowError("Los meses para pagar deben ser mayor a 0.");
                return;
            }

            if (InitialPayment < 0)
            {
                ShowError("El abono inicial no puede ser negativo.");
                return;
            }

            if (InitialPayment > Total)
            {
                ShowError("El abono inicial no puede ser mayor al total.");
                return;
            }

            try
            {
                IsProcessing = true;
                ClearError();

                var (success, credit, error) = await _creditService.CreateCreditAsync(
                    CartItems.ToList(),
                    _customer.Id,
                    MonthsToPay,
                    InitialPayment,
                    PaymentMethod,
                    _authService.CurrentUser?.Id ?? 0,
                    _branchId,
                    string.IsNullOrWhiteSpace(Notes) ? null : Notes);

                if (success && credit != null)
                {
                    CreditCreated?.Invoke(this, credit);
                }
                else
                {
                    ShowError(error ?? "Error al crear el credito.");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        public void HandleKeyPress(string key)
        {
            switch (key.ToUpper())
            {
                case "F5":
                    _ = ConfirmAsync();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }
    }
}

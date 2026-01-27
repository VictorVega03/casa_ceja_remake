using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal AmountPaid { get; set; }
        public string Folio { get; set; } = string.Empty;
        public bool IsCredit { get; set; }

        public static PaymentResult Ok(decimal amountPaid, string folio, bool isCredit)
        {
            return new PaymentResult
            {
                Success = true,
                AmountPaid = amountPaid,
                Folio = folio,
                IsCredit = isCredit
            };
        }

        public static PaymentResult Error(string message)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }

    public partial class AddPaymentViewModel : ViewModelBase
    {
        private readonly CreditService _creditService;
        private readonly LayawayService _layawayService;
        private readonly AuthService _authService;

        // Info del credito/apartado
        [ObservableProperty]
        private string _folio = string.Empty;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty; // "Credito" o "Apartado"

        [ObservableProperty]
        private decimal _total;

        [ObservableProperty]
        private decimal _totalPaid;

        [ObservableProperty]
        private decimal _remainingBalance;

        [ObservableProperty]
        private DateTime _dueDate;

        // Pago
        [ObservableProperty]
        private decimal _amountToPay;

        [ObservableProperty]
        private PaymentMethod _paymentMethod = PaymentMethod.Efectivo;

        [ObservableProperty]
        private string _notes = string.Empty;

        // Estado
        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        // Datos internos
        private int _creditId;
        private int _layawayId;
        private bool _isCredit;
        private Customer? _customer;

        // Metodos de pago disponibles
        public ObservableCollection<PaymentMethod> PaymentMethods { get; } = new()
        {
            PaymentMethod.Efectivo,
            PaymentMethod.TarjetaDebito,
            PaymentMethod.TarjetaCredito,
            PaymentMethod.Transferencia
        };

        public event EventHandler<PaymentResult>? PaymentCompleted;
        public event EventHandler? Cancelled;

        // Propiedades calculadas para la vista
        public string HeaderColor => _isCredit ? "#4CAF50" : "#2196F3";
        public int DaysRemaining => (DueDate - DateTime.Now).Days;
        public string DaysRemainingColor => DaysRemaining < 0 ? "#F44336" : (DaysRemaining < 7 ? "#FF9800" : "#4CAF50");

        public AddPaymentViewModel(
            CreditService creditService,
            LayawayService layawayService,
            AuthService authService)
        {
            _creditService = creditService;
            _layawayService = layawayService;
            _authService = authService;
        }

        public async Task InitializeForCreditAsync(int creditId, Customer customer)
        {
            _isCredit = true;
            _creditId = creditId;
            _customer = customer;
            CustomerName = customer.Name;
            Type = "Credito";

            var credit = await _creditService.GetByIdAsync(creditId);
            if (credit != null)
            {
                Folio = credit.Folio;
                Total = credit.Total;
                TotalPaid = credit.TotalPaid;
                RemainingBalance = credit.RemainingBalance;
                DueDate = credit.DueDate;
                AmountToPay = RemainingBalance;
            }
        }

        public async Task InitializeForLayawayAsync(int layawayId, Customer customer)
        {
            _isCredit = false;
            _layawayId = layawayId;
            _customer = customer;
            CustomerName = customer.Name;
            Type = "Apartado";

            var layaway = await _layawayService.GetByIdAsync(layawayId);
            if (layaway != null)
            {
                Folio = layaway.Folio;
                Total = layaway.Total;
                TotalPaid = layaway.TotalPaid;
                RemainingBalance = layaway.RemainingBalance;
                DueDate = layaway.PickupDate;
                AmountToPay = RemainingBalance;
            }
        }

        partial void OnAmountToPayChanged(decimal value)
        {
            // Asegurar que no sea mayor al saldo pendiente
            if (value > RemainingBalance)
            {
                AmountToPay = RemainingBalance;
            }
        }

        [RelayCommand]
        private void PayAll()
        {
            AmountToPay = RemainingBalance;
        }

        [RelayCommand]
        private async Task ConfirmAsync()
        {
            // Validaciones
            if (AmountToPay <= 0)
            {
                ShowError("El monto a abonar debe ser mayor a 0.");
                return;
            }

            if (AmountToPay > RemainingBalance)
            {
                ShowError("El monto no puede ser mayor al saldo pendiente.");
                return;
            }

            try
            {
                IsProcessing = true;
                ClearError();

                bool success;
                
                if (_isCredit)
                {
                    success = await _creditService.AddPaymentAsync(
                        _creditId,
                        AmountToPay,
                        PaymentMethod,
                        _authService.CurrentUser?.Id ?? 0,
                        string.IsNullOrWhiteSpace(Notes) ? null : Notes);
                }
                else
                {
                    success = await _layawayService.AddPaymentAsync(
                        _layawayId,
                        AmountToPay,
                        PaymentMethod,
                        _authService.CurrentUser?.Id ?? 0,
                        string.IsNullOrWhiteSpace(Notes) ? null : Notes);
                }

                if (success)
                {
                    PaymentCompleted?.Invoke(this, PaymentResult.Ok(AmountToPay, Folio, _isCredit));
                }
                else
                {
                    ShowError("Error al procesar el abono.");
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
                case "F4":
                    PayAll();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }
    }
}

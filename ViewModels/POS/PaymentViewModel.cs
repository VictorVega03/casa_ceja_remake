using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class PaymentViewModel : ViewModelBase
    {
        [ObservableProperty]
        private decimal _totalToPay;

        [ObservableProperty]
        private decimal _amountPaid;

        [ObservableProperty]
        private decimal _change;

        [ObservableProperty]
        private PaymentMethod _selectedPaymentMethod = PaymentMethod.Efectivo;

        [ObservableProperty]
        private bool _isCashSelected = true;

        [ObservableProperty]
        private bool _isDebitSelected;

        [ObservableProperty]
        private bool _isCreditSelected;

        [ObservableProperty]
        private bool _isTransferSelected;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _canConfirm;

        [ObservableProperty]
        private int _totalItems;

        public event EventHandler<(PaymentMethod, decimal)>? PaymentConfirmed;
        public event EventHandler? PaymentCancelled;

        public PaymentViewModel(decimal total, int articulos)
        {
            TotalToPay = total;
            TotalItems = articulos;
            AmountPaid = total;
            CalculateChange();
        }

        partial void OnAmountPaidChanged(decimal value)
        {
            CalculateChange();
        }

        partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
        {
            // Actualizar flags de seleccion
            IsCashSelected = value == PaymentMethod.Efectivo;
            IsDebitSelected = value == PaymentMethod.TarjetaDebito;
            IsCreditSelected = value == PaymentMethod.TarjetaCredito;
            IsTransferSelected = value == PaymentMethod.Transferencia;

            // Si no es efectivo, el monto pagado es el total
            if (value != PaymentMethod.Efectivo)
            {
                AmountPaid = TotalToPay;
            }

            CalculateChange();
        }

        private void CalculateChange()
        {
            if (SelectedPaymentMethod == PaymentMethod.Efectivo)
            {
                Change = AmountPaid - TotalToPay;
                
                if (AmountPaid < TotalToPay)
                {
                    ErrorMessage = "Monto insuficiente";
                    CanConfirm = false;
                }
                else
                {
                    ErrorMessage = string.Empty;
                    CanConfirm = true;
                }
            }
            else
            {
                Change = 0;
                ErrorMessage = string.Empty;
                CanConfirm = true;
            }
        }

        [RelayCommand]
        private void SelectCash()
        {
            SelectedPaymentMethod = PaymentMethod.Efectivo;
        }

        [RelayCommand]
        private void SelectDebit()
        {
            SelectedPaymentMethod = PaymentMethod.TarjetaDebito;
        }

        [RelayCommand]
        private void SelectCredit()
        {
            SelectedPaymentMethod = PaymentMethod.TarjetaCredito;
        }

        [RelayCommand]
        private void SelectTransfer()
        {
            SelectedPaymentMethod = PaymentMethod.Transferencia;
        }

        [RelayCommand]
        private void AddAmount(string monto)
        {
            if (decimal.TryParse(monto, out decimal cantidad))
            {
                AmountPaid += cantidad;
            }
        }

        [RelayCommand]
        private void ExactPayment()
        {
            AmountPaid = TotalToPay;
        }

        [RelayCommand]
        private void ClearAmount()
        {
            AmountPaid = 0;
        }

        [RelayCommand]
        private void Confirm()
        {
            if (!CanConfirm) return;

            PaymentConfirmed?.Invoke(this, (SelectedPaymentMethod, AmountPaid));
        }

        [RelayCommand]
        private void Cancel()
        {
            PaymentCancelled?.Invoke(this, EventArgs.Empty);
        }

        public string PaymentMethodName => SelectedPaymentMethod switch
        {
            PaymentMethod.Efectivo => "Efectivo",
            PaymentMethod.TarjetaDebito => "Tarjeta Debito",
            PaymentMethod.TarjetaCredito => "Tarjeta Credito",
            PaymentMethod.Transferencia => "Transferencia",
            _ => "Desconocido"
        };
    }
}
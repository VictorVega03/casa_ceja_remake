using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.ViewModels.POS
{
    // Representa un pago individual en la lista de pagos mixtos
    public class PaymentEntry
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public partial class PaymentViewModel : ViewModelBase
    {
        [ObservableProperty]
        private decimal _totalToPay;

        [ObservableProperty]
        private decimal _totalPaid;

        [ObservableProperty]
        private decimal _remainingAmount;

        [ObservableProperty]
        private decimal _change;

        [ObservableProperty]
        private bool _showChange;

        [ObservableProperty]
        private string _remainingColor = "#FF9800";

        [ObservableProperty]
        private decimal _currentAmount;

        [ObservableProperty]
        private string _currentMethodName = "Efectivo";

        [ObservableProperty]
        private PaymentMethod _currentMethod = PaymentMethod.Efectivo;

        [ObservableProperty]
        private bool _isEffectivoSelected = true;

        [ObservableProperty]
        private bool _isDebitoSelected;

        [ObservableProperty]
        private bool _isCreditoSelected;

        [ObservableProperty]
        private bool _isTransferenciaSelected;

        [ObservableProperty]
        private bool _isChequesSelected;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _canConfirm;

        [ObservableProperty]
        private bool _hasPayments;

        [ObservableProperty]
        private int _totalItems;

        // Lista de pagos mixtos
        public ObservableCollection<PaymentEntry> PaymentsList { get; } = new();

        public event EventHandler<(string PaymentJson, decimal TotalPaid, decimal Change)>? PaymentConfirmed;
        public event EventHandler? PaymentCancelled;

        public PaymentViewModel(decimal total, int articulos)
        {
            TotalToPay = total;
            TotalItems = articulos;
            RemainingAmount = total;
            CurrentAmount = 0; // Iniciar en 0 por defecto
            UpdateState();
        }

        private void UpdateState()
        {
            // Calcular total pagado
            decimal sum = 0;
            foreach (var p in PaymentsList)
            {
                sum += p.Amount;
            }
            TotalPaid = sum;
            
            // Calcular restante
            RemainingAmount = TotalToPay - TotalPaid;
            
            // Color del restante
            if (RemainingAmount <= 0)
                RemainingColor = "#4CAF50"; // Verde - pagado
            else if (RemainingAmount < TotalToPay)
                RemainingColor = "#2196F3"; // Azul - parcialmente pagado
            else
                RemainingColor = "#FF9800"; // Naranja - sin pagar

            // Calcular cambio (solo si hay efectivo y se pagó de más)
            Change = TotalPaid > TotalToPay ? TotalPaid - TotalToPay : 0;
            
            // Mostrar cambio solo si hay pagos en efectivo y hay exceso
            bool hasCash = false;
            foreach (var p in PaymentsList)
            {
                if (p.Method == "Efectivo") { hasCash = true; break; }
            }
            ShowChange = hasCash && Change > 0;

            // Puede confirmar si cubrió el total
            HasPayments = PaymentsList.Count > 0;
            CanConfirm = TotalPaid >= TotalToPay;
        }

        [RelayCommand]
        private void SelectMethod(string method)
        {
            CurrentMethodName = method switch
            {
                "Efectivo" => "Efectivo",
                "Debito" => "Tarjeta Débito",
                "Credito" => "Tarjeta Crédito",
                "Transferencia" => "Transferencia",
                "Cheque" => "Cheque",
                _ => "Efectivo"
            };

            CurrentMethod = method switch
            {
                "Efectivo" => PaymentMethod.Efectivo,
                "Debito" => PaymentMethod.TarjetaDebito,
                "Credito" => PaymentMethod.TarjetaCredito,
                "Transferencia" => PaymentMethod.Transferencia,
                "Cheque" => PaymentMethod.Cheque,
                _ => PaymentMethod.Efectivo
            };

            // Actualizar estados de selección visual
            IsEffectivoSelected = method == "Efectivo";
            IsDebitoSelected = method == "Debito";
            IsCreditoSelected = method == "Credito";
            IsTransferenciaSelected = method == "Transferencia";
            IsChequesSelected = method == "Cheque";

            // Si no es efectivo, sugerir el restante exacto
            if (CurrentMethod != PaymentMethod.Efectivo)
            {
                CurrentAmount = RemainingAmount > 0 ? RemainingAmount : 0;
            }
        }

        [RelayCommand]
        private void AddToCurrent(string monto)
        {
            if (decimal.TryParse(monto, out decimal cantidad))
            {
                CurrentAmount += cantidad;
            }
        }

        [RelayCommand]
        private void ClearCurrent()
        {
            CurrentAmount = 0;
        }

        [RelayCommand]
        private void PayRemaining()
        {
            CurrentAmount = RemainingAmount > 0 ? RemainingAmount : 0;
        }

        [RelayCommand]
        private void AddPayment()
        {
            if (CurrentAmount <= 0)
            {
                ErrorMessage = "El monto debe ser mayor a 0";
                return;
            }

            // Validación: métodos que NO son efectivo no pueden exceder el restante
            if (CurrentMethod != PaymentMethod.Efectivo && CurrentAmount > RemainingAmount)
            {
                ErrorMessage = $"Con {CurrentMethodName} no puede pagar más del restante (${RemainingAmount:N2})";
                return;
            }

            // Agregar pago a la lista (al inicio para que aparezca arriba)
            PaymentsList.Insert(0, new PaymentEntry
            {
                Method = CurrentMethodName,
                Amount = CurrentAmount
            });

            ErrorMessage = string.Empty;
            UpdateState();

            // Preparar para siguiente pago
            CurrentAmount = RemainingAmount > 0 ? RemainingAmount : 0;
        }

        [RelayCommand]
        private void RemovePayment(PaymentEntry payment)
        {
            if (payment != null)
            {
                PaymentsList.Remove(payment);
                UpdateState();
                CurrentAmount = RemainingAmount > 0 ? RemainingAmount : 0;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            if (!CanConfirm)
            {
                ErrorMessage = "Debe cubrir el total para confirmar la venta";
                return;
            }

            // Generar JSON de pagos mixtos
            var paymentDict = new System.Collections.Generic.Dictionary<string, decimal>();
            foreach (var p in PaymentsList)
            {
                string key = p.Method.ToLower()
                    .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                    .Replace("ó", "o").Replace("ú", "u")
                    .Replace(" ", "_");
                
                if (paymentDict.ContainsKey(key))
                    paymentDict[key] += p.Amount;
                else
                    paymentDict[key] = p.Amount;
            }

            string paymentJson = JsonSerializer.Serialize(paymentDict);
            
            PaymentConfirmed?.Invoke(this, (paymentJson, TotalPaid, Change));
        }

        [RelayCommand]
        private void Cancel()
        {
            PaymentCancelled?.Invoke(this, EventArgs.Empty);
        }

        // Incrementar/decrementar con flechas
        public void AdjustAmount(int delta)
        {
            CurrentAmount = Math.Max(0, CurrentAmount + delta);
        }
    }
}
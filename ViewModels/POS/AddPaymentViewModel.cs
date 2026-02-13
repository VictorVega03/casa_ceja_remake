using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
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

    // Representa un pago individual en la lista de pagos mixtos
    public class AddPaymentEntry
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
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

        // Pago actual (pagos mixtos)
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
        private decimal _totalCurrentlyPaid; // Total abonado en esta sesión

        [ObservableProperty]
        private decimal _currentRemaining; // Lo que falta por abonar en esta sesión

        [ObservableProperty]
        private string _currentRemainingColor = "#FF9800";

        [ObservableProperty]
        private string _notes = string.Empty;

        // Estado
        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _canConfirm;

        [ObservableProperty]
        private bool _hasPayments;

        // Datos internos
        private int _creditId;
        private int _layawayId;
        private bool _isCredit;
        private Customer? _customer;

        // Lista de pagos agregados en esta sesión
        public ObservableCollection<AddPaymentEntry> PaymentsList { get; } = new();

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
                CurrentAmount = RemainingBalance; // Sugerir pagar todo
                CurrentRemaining = RemainingBalance;
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
                CurrentAmount = RemainingBalance; // Sugerir pagar todo
                CurrentRemaining = RemainingBalance;
            }
        }

        private void UpdateState()
        {
            // Calcular total abonado en esta sesión
            decimal sum = 0;
            foreach (var p in PaymentsList)
            {
                sum += p.Amount;
            }
            TotalCurrentlyPaid = sum;

            // Restante es el balance original menos lo abonado en esta sesión
            CurrentRemaining = RemainingBalance - TotalCurrentlyPaid;

            // Color del restante
            if (CurrentRemaining <= 0)
                CurrentRemainingColor = "#4CAF50"; // Verde - pagado
            else if (CurrentRemaining < RemainingBalance)
                CurrentRemainingColor = "#2196F3"; // Azul - parcialmente pagado
            else
                CurrentRemainingColor = "#FF9800"; // Naranja - sin abonar

            // Puede confirmar si se abonó algo (no necesariamente todo)
            HasPayments = PaymentsList.Count > 0;
            CanConfirm = HasPayments;
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
                _ => "Efectivo"
            };

            CurrentMethod = method switch
            {
                "Efectivo" => PaymentMethod.Efectivo,
                "Debito" => PaymentMethod.TarjetaDebito,
                "Credito" => PaymentMethod.TarjetaCredito,
                "Transferencia" => PaymentMethod.Transferencia,
                _ => PaymentMethod.Efectivo
            };

            // Actualizar estados de selección visual
            IsEffectivoSelected = method == "Efectivo";
            IsDebitoSelected = method == "Debito";
            IsCreditoSelected = method == "Credito";
            IsTransferenciaSelected = method == "Transferencia";

            // Si no es efectivo, sugerir el restante
            if (CurrentMethod != PaymentMethod.Efectivo && CurrentRemaining > 0)
            {
                CurrentAmount = CurrentRemaining;
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
            CurrentAmount = CurrentRemaining > 0 ? CurrentRemaining : 0;
        }

        [RelayCommand]
        private void AddPayment()
        {
            if (CurrentAmount <= 0)
            {
                ShowError("El monto debe ser mayor a 0");
                return;
            }

            if (CurrentAmount > CurrentRemaining)
            {
                ShowError("El monto no puede ser mayor al saldo pendiente");
                return;
            }

            // Agregar pago a la lista (al inicio para que aparezca arriba)
            PaymentsList.Insert(0, new AddPaymentEntry
            {
                Method = CurrentMethodName,
                Amount = CurrentAmount
            });

            ClearError();
            UpdateState();

            // Preparar para siguiente pago
            CurrentAmount = CurrentRemaining > 0 ? CurrentRemaining : 0;
        }

        [RelayCommand]
        private void RemovePayment(AddPaymentEntry payment)
        {
            if (payment != null)
            {
                PaymentsList.Remove(payment);
                UpdateState();
                CurrentAmount = CurrentRemaining > 0 ? CurrentRemaining : 0;
            }
        }

        [RelayCommand]
        private async Task ConfirmAsync()
        {
            if (!CanConfirm)
            {
                ShowError("Debe agregar al menos un pago");
                return;
            }

            try
            {
                IsProcessing = true;
                ClearError();

                // Generar JSON de pagos mixtos
                var paymentDict = new Dictionary<string, decimal>();
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

                bool success;

                if (_isCredit)
                {
                    success = await _creditService.AddPaymentWithMixedAsync(
                        _creditId,
                        TotalCurrentlyPaid,
                        paymentJson,
                        _authService.CurrentUser?.Id ?? 0,
                        string.IsNullOrWhiteSpace(Notes) ? null : Notes);
                }
                else
                {
                    success = await _layawayService.AddPaymentWithMixedAsync(
                        _layawayId,
                        TotalCurrentlyPaid,
                        paymentJson,
                        _authService.CurrentUser?.Id ?? 0,
                        string.IsNullOrWhiteSpace(Notes) ? null : Notes);
                }

                if (success)
                {
                    PaymentCompleted?.Invoke(this, PaymentResult.Ok(TotalCurrentlyPaid, Folio, _isCredit));
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

        // Incrementar/decrementar con flechas (igual que PaymentViewModel)
        public void AdjustAmount(int delta)
        {
            CurrentAmount = Math.Max(0, CurrentAmount + delta);
        }

        public void HandleKeyPress(string key)
        {
            switch (key.ToUpper())
            {
                case "F5":
                    _ = ConfirmAsync();
                    break;
                case "F4":
                    PayRemaining();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }
    }
}

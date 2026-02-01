using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Helpers;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class CreditLayawayDetailViewModel : ViewModelBase
    {
        private readonly CreditService _creditService;
        private readonly LayawayService _layawayService;
        private readonly CustomerService _customerService;
        private readonly AuthService _authService;

        // Tipo de vista
        [ObservableProperty]
        private bool _isCredit;

        // Información básica
        [ObservableProperty]
        private string _folio = string.Empty;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerPhone = string.Empty;

        [ObservableProperty]
        private DateTime _createdDate;

        [ObservableProperty]
        private DateTime _dueDate;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private string _statusColor = string.Empty;

        // Información financiera
        [ObservableProperty]
        private decimal _total;

        [ObservableProperty]
        private decimal _totalPaid;

        [ObservableProperty]
        private decimal _remainingBalance;

        [ObservableProperty]
        private string _notes = string.Empty;

        // Productos
        public ObservableCollection<ProductDetailItem> Products { get; } = new();

        // Historial de pagos
        public ObservableCollection<PaymentHistoryItem> PaymentHistory { get; } = new();

        // Estado
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        // Objetos internos
        private Credit? _credit;
        private Layaway? _layaway;
        private Customer? _customer;

        // Propiedades calculadas
        public string WindowTitle => IsCredit ? "Detalle de Crédito" : "Detalle de Apartado";
        public string HeaderTitle => IsCredit ? "CRÉDITO" : "APARTADO";
        public string HeaderColor => IsCredit ? "#4CAF50" : "#2196F3";
        public string TypeLabel => IsCredit ? "Crédito" : "Apartado";
        public string DateLabel => IsCredit ? "Fecha crédito" : "Fecha apartado";
        public string DueDateLabel => IsCredit ? "Fecha vencimiento" : "Fecha entrega";
        
        public bool CanAddPayment => IsCredit 
            ? (_credit?.CanAddPayment() ?? false) 
            : (_layaway?.CanAddPayment() ?? false);
        
        public bool CanDeliver => !IsCredit && (_layaway?.CanDeliver ?? false);
        
        public int PaymentCount => PaymentHistory.Count;
        public int ProductCount => Products.Count;

        // Eventos
        public event EventHandler? AddPaymentRequested;
        public event EventHandler? DeliverRequested;
        public event EventHandler? PrintRequested;
        public event EventHandler? CloseRequested;

        public CreditLayawayDetailViewModel(
            CreditService creditService,
            LayawayService layawayService,
            CustomerService customerService,
            AuthService authService)
        {
            _creditService = creditService;
            _layawayService = layawayService;
            _customerService = customerService;
            _authService = authService;
        }

        public async Task InitializeForCreditAsync(int creditId)
        {
            IsCredit = true;
            await LoadCreditDataAsync(creditId);
        }

        public async Task InitializeForLayawayAsync(int layawayId)
        {
            IsCredit = false;
            await LoadLayawayDataAsync(layawayId);
        }

        private async Task LoadCreditDataAsync(int creditId)
        {
            try
            {
                IsLoading = true;
                ClearError();

                _credit = await _creditService.GetByIdAsync(creditId);
                if (_credit == null)
                {
                    ShowError("Crédito no encontrado");
                    return;
                }

                _customer = await _customerService.GetByIdAsync(_credit.CustomerId);
                
                // Cargar información básica
                Folio = _credit.Folio;
                CustomerName = _customer?.Name ?? "N/A";
                CustomerPhone = _customer?.Phone ?? "N/A";
                CreatedDate = _credit.CreditDate;
                DueDate = _credit.DueDate;
                Status = _credit.StatusName;
                StatusColor = _credit.GetStatusColor();
                Total = _credit.Total;
                TotalPaid = _credit.TotalPaid;
                RemainingBalance = _credit.RemainingBalance;
                Notes = _credit.Notes ?? string.Empty;

                // Cargar productos
                await LoadCreditProductsAsync(creditId);

                // Cargar historial de pagos
                await LoadCreditPaymentsAsync(creditId);

                OnPropertyChanged(nameof(CanAddPayment));
            }
            catch (Exception ex)
            {
                ShowError($"Error al cargar crédito: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadLayawayDataAsync(int layawayId)
        {
            try
            {
                IsLoading = true;
                ClearError();

                _layaway = await _layawayService.GetByIdAsync(layawayId);
                if (_layaway == null)
                {
                    ShowError("Apartado no encontrado");
                    return;
                }

                _customer = await _customerService.GetByIdAsync(_layaway.CustomerId);
                
                // Cargar información básica
                Folio = _layaway.Folio;
                CustomerName = _customer?.Name ?? "N/A";
                CustomerPhone = _customer?.Phone ?? "N/A";
                CreatedDate = _layaway.LayawayDate;
                DueDate = _layaway.PickupDate;
                Status = _layaway.StatusName;
                StatusColor = _layaway.GetStatusColor();
                Total = _layaway.Total;
                TotalPaid = _layaway.TotalPaid;
                RemainingBalance = _layaway.RemainingBalance;
                Notes = _layaway.Notes ?? string.Empty;

                // Cargar productos
                await LoadLayawayProductsAsync(layawayId);

                // Cargar historial de pagos
                await LoadLayawayPaymentsAsync(layawayId);

                OnPropertyChanged(nameof(CanAddPayment));
                OnPropertyChanged(nameof(CanDeliver));
            }
            catch (Exception ex)
            {
                ShowError($"Error al cargar apartado: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadCreditProductsAsync(int creditId)
        {
            Products.Clear();
            var products = await _creditService.GetProductsAsync(creditId);
            
            foreach (var product in products)
            {
                Products.Add(new ProductDetailItem
                {
                    Name = product.ProductName,
                    Barcode = product.Barcode,
                    Quantity = product.Quantity,
                    UnitPrice = product.UnitPrice,
                    LineTotal = product.LineTotal
                });
            }

            OnPropertyChanged(nameof(ProductCount));
        }

        private async Task LoadLayawayProductsAsync(int layawayId)
        {
            Products.Clear();
            var products = await _layawayService.GetProductsAsync(layawayId);
            
            foreach (var product in products)
            {
                Products.Add(new ProductDetailItem
                {
                    Name = product.ProductName,
                    Barcode = product.Barcode,
                    Quantity = product.Quantity,
                    UnitPrice = product.UnitPrice,
                    LineTotal = product.LineTotal
                });
            }

            OnPropertyChanged(nameof(ProductCount));
        }

        private async Task LoadCreditPaymentsAsync(int creditId)
        {
            PaymentHistory.Clear();
            var payments = await _creditService.GetPaymentsAsync(creditId);
            
            foreach (var payment in payments)
            {
                PaymentHistory.Add(new PaymentHistoryItem
                {
                    Folio = payment.Folio,
                    Date = payment.PaymentDate,
                    Amount = payment.AmountPaid,
                    PaymentMethod = ParsePaymentMethod(payment.PaymentMethod),
                    Notes = payment.Notes ?? ""
                });
            }

            OnPropertyChanged(nameof(PaymentCount));
        }

        private async Task LoadLayawayPaymentsAsync(int layawayId)
        {
            PaymentHistory.Clear();
            var payments = await _layawayService.GetPaymentsAsync(layawayId);
            
            foreach (var payment in payments)
            {
                PaymentHistory.Add(new PaymentHistoryItem
                {
                    Folio = payment.Folio,
                    Date = payment.PaymentDate,
                    Amount = payment.AmountPaid,
                    PaymentMethod = ParsePaymentMethod(payment.PaymentMethod),
                    Notes = payment.Notes ?? ""
                });
            }

            OnPropertyChanged(nameof(PaymentCount));
        }

        [RelayCommand]
        private void AddPayment()
        {
            if (!CanAddPayment)
            {
                ShowError($"Este {TypeLabel.ToLower()} no acepta más pagos");
                return;
            }

            AddPaymentRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Deliver()
        {
            if (IsCredit)
            {
                ShowError("La entrega solo aplica para apartados");
                return;
            }

            if (!CanDeliver)
            {
                ShowError("Este apartado no puede ser entregado (saldo pendiente o ya entregado)");
                return;
            }

            DeliverRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Print()
        {
            PrintRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public async Task RefreshAsync()
        {
            if (IsCredit && _credit != null)
            {
                await LoadCreditDataAsync(_credit.Id);
            }
            else if (!IsCredit && _layaway != null)
            {
                await LoadLayawayDataAsync(_layaway.Id);
            }
        }

        public Credit? GetCredit() => _credit;
        public Layaway? GetLayaway() => _layaway;
        public Customer? GetCustomer() => _customer;

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
                    AddPayment();
                    break;
                case "F6":
                    if (!IsCredit) Deliver();
                    break;
                case "F7":
                    Print();
                    break;
                case "ESCAPE":
                    Close();
                    break;
            }
        }

        /// <summary>
        /// Parse payment method string - could be JSON for mixed payments or a simple string
        /// </summary>
        private static string ParsePaymentMethod(string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return "N/A";

            // Intentar parsear como JSON (pagos mixtos)
            if (paymentMethod.TrimStart().StartsWith("{"))
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, decimal>>(paymentMethod);
                    if (dict != null && dict.Count > 0)
                    {
                        // Formatear como: "Efectivo: $100, Débito: $50"
                        var parts = dict.Select(kvp => 
                        {
                            string methodName = kvp.Key switch
                            {
                                "efectivo" => "Efectivo",
                                "tarjeta_debito" => "Débito",
                                "tarjeta_credito" => "Crédito",
                                "transferencia" => "Transfer.",
                                _ => kvp.Key
                            };
                            return $"{methodName}: ${kvp.Value:F2}";
                        });
                        return string.Join(", ", parts);
                    }
                }
                catch
                {
                    // Si falla el parsing, devolver el string original
                }
            }

            // Si no es JSON o falla el parsing, devolver el string tal cual
            return paymentMethod;
        }
    }

    // Clases auxiliares para mostrar datos
    public class ProductDetailItem
    {
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string QuantityDisplay => $"x{Quantity}";
    }

    public class PaymentHistoryItem
    {
        public string Folio { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string DateFormatted => Date.ToString("dd/MM/yyyy HH:mm");
    }
}

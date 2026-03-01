using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class CreateLayawayViewModel : ViewModelBase
    {
        private readonly ILayawayService _layawayService;
        private readonly IAuthService _authService;
        private readonly int _branchId;

        // Cliente (readonly)
        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerPhone = string.Empty;

        private Customer? _customer;

        // Configuracion
        [ObservableProperty]
        private int _daysToPickup = 30;

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
        private DateTime _pickupDate;

        [ObservableProperty]
        private decimal _minimumPayment;

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

        public event EventHandler<Layaway>? LayawayCreated;
        public event EventHandler? Cancelled;

        public CreateLayawayViewModel(ILayawayService layawayService, IAuthService authService, int branchId)
        {
            _layawayService = layawayService;
            _authService = authService;
            _branchId = branchId;

            UpdatePickupDate();
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
            
            // Calcular abono minimo (10% del total)
            MinimumPayment = Math.Ceiling(Total * 0.10m);
            InitialPayment = MinimumPayment;
            
            UpdateCalculations();
        }

        partial void OnDaysToPickupChanged(int value)
        {
            UpdatePickupDate();
        }

        partial void OnInitialPaymentChanged(decimal value)
        {
            UpdateCalculations();
        }

        private void UpdatePickupDate()
        {
            PickupDate = DateTime.Now.AddDays(DaysToPickup);
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

            if (DaysToPickup <= 0)
            {
                ShowError("Los dias para recoger deben ser mayor a 0.");
                return;
            }

            if (InitialPayment <= 0)
            {
                ShowError("Se requiere un abono inicial para crear el apartado.");
                return;
            }

            if (InitialPayment < MinimumPayment)
            {
                ShowError($"El abono minimo es ${MinimumPayment:N2} (10% del total).");
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

                var (success, layaway, error) = await _layawayService.CreateLayawayAsync(
                    CartItems.ToList(),
                    _customer.Id,
                    DaysToPickup,
                    InitialPayment,
                    PaymentMethod,
                    _authService.CurrentUser?.Id ?? 0,
                    _branchId,
                    string.IsNullOrWhiteSpace(Notes) ? null : Notes);

                if (success && layaway != null)
                {
                    LayawayCreated?.Invoke(this, layaway);
                }
                else
                {
                    ShowError(error ?? "Error al crear el apartado.");
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

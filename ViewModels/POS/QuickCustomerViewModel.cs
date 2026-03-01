using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class QuickCustomerViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;

        // Campos obligatorios
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        // Campos opcionales
        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _rfc = string.Empty;

        [ObservableProperty]
        private string _street = string.Empty;

        [ObservableProperty]
        private string _city = string.Empty;

        // Estado
        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public event EventHandler<Customer>? CustomerCreated;
        public event EventHandler? Cancelled;

        public QuickCustomerViewModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowError("El nombre es requerido.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                ShowError("El telefono es requerido.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(Email))
            {
                if (!Email.Contains('@') || !Email.Contains('.'))
                {
                    ShowError("El formato del correo electronico es invalido.");
                    return;
                }
            }

            // Validar formato de telefono (al menos 10 digitos)
            var cleanPhone = Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (cleanPhone.Length < 10)
            {
                ShowError("El telefono debe tener al menos 10 digitos.");
                return;
            }

            try
            {
                IsSaving = true;
                ClearError();

                // Verificar si ya existe
                if (await _customerService.ExistsByPhoneAsync(Phone))
                {
                    ShowError($"Ya existe un cliente con el telefono {Phone}.");
                    return;
                }

                var customer = new Customer
                {
                    Name = Name.Trim(),
                    Phone = Phone.Trim(),
                    Email = Email?.Trim() ?? string.Empty,
                    Rfc = Rfc?.Trim() ?? string.Empty,
                    Street = Street?.Trim() ?? string.Empty,
                    City = City?.Trim() ?? string.Empty,
                    Active = true
                };

                var customerId = await _customerService.CreateAsync(customer);
                customer.Id = customerId;

                CustomerCreated?.Invoke(this, customer);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                IsSaving = false;
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
                    _ = SaveAsync();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }

        public void Clear()
        {
            Name = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            Rfc = string.Empty;
            Street = string.Empty;
            City = string.Empty;
            ClearError();
        }
    }
}

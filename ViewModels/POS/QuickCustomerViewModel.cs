using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class QuickCustomerViewModel : ViewModelBase
    {
        private readonly CustomerService _customerService;
        private Customer? _editingCustomer;

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
        private string _exteriorNumber = string.Empty;

        [ObservableProperty]
        private string _interiorNumber = string.Empty;

        [ObservableProperty]
        private string _neighborhood = string.Empty;

        [ObservableProperty]
        private string _postalCode = string.Empty;

        [ObservableProperty]
        private string _city = string.Empty;

        // Estado
        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        private bool _editModeActive;

        public bool EditModeActive
        {
            get => _editModeActive;
            set
            {
                if (SetProperty(ref _editModeActive, value))
                {
                    OnPropertyChanged(nameof(FormTitle));
                    OnPropertyChanged(nameof(HeaderTitle));
                    OnPropertyChanged(nameof(SaveButtonText));
                }
            }
        }

        public string FormTitle => EditModeActive ? "Editar Cliente" : "Alta de Cliente";
        public string HeaderTitle => EditModeActive ? "EDITAR CLIENTE" : "ALTA DE CLIENTE";
        public string SaveButtonText => EditModeActive ? "Guardar Cambios" : "Guardar";

        public event EventHandler<Customer>? CustomerCreated;
        public event EventHandler? Cancelled;

        public QuickCustomerViewModel(CustomerService customerService)
        {
            _customerService = customerService;
        }

        partial void OnEmailChanged(string value)
        {
            // No permitir espacios en correo
            var sanitized = (value ?? string.Empty).Replace(" ", string.Empty);
            if (!string.Equals(sanitized, value, StringComparison.Ordinal))
            {
                Email = sanitized;
            }
        }

        partial void OnPhoneChanged(string value)
        {
            var sanitized = new string(value.Where(char.IsDigit).ToArray());
            if (!string.Equals(sanitized, value, StringComparison.Ordinal))
            {
                Phone = sanitized;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            var normalizedPhone = RemoveWhitespace(Phone);

            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowError("El nombre es requerido.");
                return;
            }

            if (string.IsNullOrWhiteSpace(normalizedPhone))
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
            var cleanPhone = normalizedPhone.Replace("-", "").Replace("(", "").Replace(")", "");
            if (cleanPhone.Length < 10)
            {
                ShowError("El telefono debe tener al menos 10 digitos.");
                return;
            }

            try
            {
                IsSaving = true;
                ClearError();

                // Verificar duplicados solo en alta
                if (!EditModeActive && await _customerService.ExistsByPhoneAsync(normalizedPhone))
                {
                    ShowError($"Ya existe un cliente con el telefono {normalizedPhone}.");
                    return;
                }

                if (EditModeActive)
                {
                    if (_editingCustomer == null || _editingCustomer.Id <= 0)
                    {
                        ShowError("No se pudo cargar el cliente a editar.");
                        return;
                    }

                    var updatedCustomer = new Customer
                    {
                        Id = _editingCustomer.Id,
                        Name = Name.Trim(),
                        Phone = normalizedPhone.Trim(),
                        Email = Email?.Trim() ?? string.Empty,
                        Rfc = Rfc?.Trim() ?? string.Empty,
                        Street = Street?.Trim() ?? string.Empty,
                        ExteriorNumber = ExteriorNumber?.Trim() ?? string.Empty,
                        InteriorNumber = InteriorNumber?.Trim() ?? string.Empty,
                        Neighborhood = Neighborhood?.Trim() ?? string.Empty,
                        PostalCode = PostalCode?.Trim() ?? string.Empty,
                        City = City?.Trim() ?? string.Empty,
                        Active = _editingCustomer.Active,
                        CreatedAt = _editingCustomer.CreatedAt,
                        UpdatedAt = _editingCustomer.UpdatedAt,
                        SyncStatus = _editingCustomer.SyncStatus,
                        LastSync = _editingCustomer.LastSync
                    };

                    await _customerService.UpdateAsync(updatedCustomer);
                    CustomerCreated?.Invoke(this, updatedCustomer);
                    return;
                }

                var customer = new Customer
                {
                    Name = Name.Trim(),
                    Phone = normalizedPhone.Trim(),
                    Email = Email?.Trim() ?? string.Empty,
                    Rfc = Rfc?.Trim() ?? string.Empty,
                    Street = Street?.Trim() ?? string.Empty,
                    ExteriorNumber = ExteriorNumber?.Trim() ?? string.Empty,
                    InteriorNumber = InteriorNumber?.Trim() ?? string.Empty,
                    Neighborhood = Neighborhood?.Trim() ?? string.Empty,
                    PostalCode = PostalCode?.Trim() ?? string.Empty,
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

        private static string RemoveWhitespace(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
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
            _editingCustomer = null;
            EditModeActive = false;
            Name = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            Rfc = string.Empty;
            Street = string.Empty;
            ExteriorNumber = string.Empty;
            InteriorNumber = string.Empty;
            Neighborhood = string.Empty;
            PostalCode = string.Empty;
            City = string.Empty;
            ClearError();
        }

        public void LoadForEdit(Customer customer)
        {
            _editingCustomer = customer ?? throw new ArgumentNullException(nameof(customer));
            EditModeActive = true;

            Name = customer.Name ?? string.Empty;
            Phone = customer.Phone ?? string.Empty;
            Email = customer.Email ?? string.Empty;
            Rfc = customer.Rfc ?? string.Empty;
            Street = customer.Street ?? string.Empty;
            ExteriorNumber = customer.ExteriorNumber ?? string.Empty;
            InteriorNumber = customer.InteriorNumber ?? string.Empty;
            Neighborhood = customer.Neighborhood ?? string.Empty;
            PostalCode = customer.PostalCode ?? string.Empty;
            City = customer.City ?? string.Empty;

            ClearError();
        }
    }
}

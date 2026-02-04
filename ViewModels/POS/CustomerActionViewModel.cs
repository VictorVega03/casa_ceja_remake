using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.ViewModels.POS
{
    public enum CustomerActionOption
    {
        NewCredit = 1,      // F1 - Nuevo Credito
        NewLayaway = 2      // F2 - Nuevo Apartado
    }

    public partial class CustomerActionViewModel : ViewModelBase
    {
        private Customer? _customer;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerPhone = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _hasCartItems;

        /// <summary>
        /// Indica si la vista está en modo "crear desde POS" (tiene carrito).
        /// </summary>
        [ObservableProperty]
        private bool _isCreateMode;

        public event EventHandler<CustomerActionOption>? ActionSelected;
        public event EventHandler? Cancelled;

        public Customer? Customer
        {
            get => _customer;
            set
            {
                _customer = value;
                if (value != null)
                {
                    CustomerName = value.Name;
                    CustomerPhone = value.Phone;
                }
            }
        }

        public void SetCustomer(Customer customer, bool hasCartItems)
        {
            Customer = customer;
            HasCartItems = hasCartItems;
            IsCreateMode = hasCartItems; // Si tiene carrito, está en modo crear
        }

        public void SetCustomerForCreate(Customer customer, bool hasCartItems)
        {
            Customer = customer;
            HasCartItems = hasCartItems;
            IsCreateMode = true; // Fuerza modo crear (solo Nuevo Crédito/Apartado)
        }

        [RelayCommand]
        private void NewCredit()
        {
            if (!HasCartItems)
            {
                ShowError("Debe agregar productos al carrito para crear un crédito");
                return;
            }
            ActionSelected?.Invoke(this, CustomerActionOption.NewCredit);
        }

        [RelayCommand]
        private void NewLayaway()
        {
            if (!HasCartItems)
            {
                ShowError("Debe agregar productos al carrito para crear un apartado");
                return;
            }
            ActionSelected?.Invoke(this, CustomerActionOption.NewLayaway);
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        public void HandleKeyPress(string key)
        {
            switch (key.ToUpper())
            {
                case "F1":
                    NewCredit();
                    break;
                case "F2":
                    NewLayaway();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }
    }
}

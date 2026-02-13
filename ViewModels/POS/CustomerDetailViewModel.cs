using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class CustomerDetailViewModel : ViewModelBase
    {
        private readonly CustomerService _customerService;
        private readonly CreditService _creditService;
        private readonly LayawayService _layawayService;
        private Customer? _customer;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerPhone = string.Empty;

        [ObservableProperty]
        private string _customerAddress = string.Empty;

        [ObservableProperty]
        private string _customerRfc = string.Empty;

        [ObservableProperty]
        private string _customerEmail = string.Empty;

        [ObservableProperty]
        private string _customerCity = string.Empty;

        [ObservableProperty]
        private string _customerNotes = string.Empty;

        [ObservableProperty]
        private DateTime _createdAt;

        [ObservableProperty]
        private int _totalCredits;

        [ObservableProperty]
        private int _activeCredits;

        [ObservableProperty]
        private decimal _totalCreditAmount;

        [ObservableProperty]
        private decimal _totalCreditBalance;

        [ObservableProperty]
        private int _totalLayaways;

        [ObservableProperty]
        private int _activeLayaways;

        [ObservableProperty]
        private decimal _totalLayawayAmount;

        [ObservableProperty]
        private decimal _totalLayawayBalance;

        [ObservableProperty]
        private bool _isLoading;

        public event EventHandler? ViewCreditsRequested;
        public event EventHandler? ViewLayawaysRequested;
        public event EventHandler? CloseRequested;

        public Customer? Customer => _customer;

        public CustomerDetailViewModel(
            CustomerService customerService,
            CreditService creditService,
            LayawayService layawayService)
        {
            _customerService = customerService;
            _creditService = creditService;
            _layawayService = layawayService;
        }

        public async Task InitializeAsync(int customerId)
        {
            IsLoading = true;

            try
            {
                // Cargar cliente
                _customer = await _customerService.GetByIdAsync(customerId);
                if (_customer == null)
                {
                    return;
                }

                // Cargar datos del cliente
                CustomerName = _customer.Name;
                CustomerPhone = _customer.Phone;
                CustomerAddress = string.IsNullOrWhiteSpace(_customer.FullAddress) ? "No especificado" : _customer.FullAddress;
                CustomerRfc = string.IsNullOrWhiteSpace(_customer.Rfc) ? "No especificado" : _customer.Rfc;
                CustomerEmail = string.IsNullOrWhiteSpace(_customer.Email) ? "No especificado" : _customer.Email;
                CustomerCity = string.IsNullOrWhiteSpace(_customer.City) ? "No especificado" : _customer.City;
                CustomerNotes = "Sin notas"; // Customer no tiene campo Notes
                CreatedAt = _customer.CreatedAt;

                // Cargar créditos
                var credits = await _creditService.GetPendingByCustomerAsync(customerId);
                TotalCredits = credits.Count;
                ActiveCredits = 0;
                TotalCreditAmount = 0;
                TotalCreditBalance = 0;

                foreach (var credit in credits)
                {
                    TotalCreditAmount += credit.Total;
                    TotalCreditBalance += credit.RemainingBalance;
                    if (credit.RemainingBalance > 0)
                    {
                        ActiveCredits++;
                    }
                }

                // Cargar apartados
                var layaways = await _layawayService.GetPendingByCustomerAsync(customerId);
                TotalLayaways = layaways.Count;
                ActiveLayaways = 0;
                TotalLayawayAmount = 0;
                TotalLayawayBalance = 0;

                foreach (var layaway in layaways)
                {
                    TotalLayawayAmount += layaway.Total;
                    TotalLayawayBalance += layaway.RemainingBalance;
                    if (layaway.RemainingBalance > 0)
                    {
                        ActiveLayaways++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CustomerDetailVM] Error al cargar datos: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ViewCredits()
        {
            ViewCreditsRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ViewLayaways()
        {
            ViewLayawaysRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void HandleKeyPress(string key)
        {
            switch (key.ToUpper())
            {
                case "F1":
                    ViewCredits();
                    break;
                case "F2":
                    ViewLayaways();
                    break;
                case "ESCAPE":
                    Close();
                    break;
            }
        }
    }
}

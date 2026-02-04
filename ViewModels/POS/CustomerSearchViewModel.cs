using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class CustomerSearchViewModel : ViewModelBase
    {
        private readonly CustomerService _customerService;

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _showActionButtons;

        public ObservableCollection<Customer> Customers { get; } = new();

        public event EventHandler<Customer>? CustomerSelected;
        public event EventHandler? CreateNewRequested;
        public event EventHandler<(Customer customer, bool isCreditsMode)>? ViewCustomerCreditsLayaways;
        public event EventHandler? Cancelled;

        public bool HasSelectedCustomer => SelectedCustomer != null;

        public CustomerSearchViewModel(CustomerService customerService)
        {
            _customerService = customerService;
        }

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            OnPropertyChanged(nameof(HasSelectedCustomer));
        }

        public async Task InitializeAsync()
        {
            // Cargar clientes iniciales
            await SearchAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            try
            {
                IsSearching = true;
                StatusMessage = "Buscando...";
                Customers.Clear();

                var results = await _customerService.SearchAsync(SearchTerm);

                foreach (var customer in results)
                {
                    Customers.Add(customer);
                }

                StatusMessage = $"{Customers.Count} cliente(s) encontrado(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSearching = false;
            }
        }

        [RelayCommand]
        private void SelectCustomer()
        {
            if (SelectedCustomer == null)
            {
                StatusMessage = "Seleccione un cliente";
                return;
            }

            CustomerSelected?.Invoke(this, SelectedCustomer);
        }

        [RelayCommand]
        private void ViewCredits()
        {
            if (SelectedCustomer == null)
            {
                StatusMessage = "Seleccione un cliente";
                return;
            }

            ViewCustomerCreditsLayaways?.Invoke(this, (SelectedCustomer, true));
        }

        [RelayCommand]
        private void ViewLayaways()
        {
            if (SelectedCustomer == null)
            {
                StatusMessage = "Seleccione un cliente";
                return;
            }

            ViewCustomerCreditsLayaways?.Invoke(this, (SelectedCustomer, false));
        }

        [RelayCommand]
        private void CreateNew()
        {
            CreateNewRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        public void HandleKeyPress(string key)
        {
            // Este método ya no es necesario porque ahora manejamos las teclas directamente en la vista
            // Lo dejamos vacío por compatibilidad pero ya no se usa
        }

        partial void OnSearchTermChanged(string value)
        {
            // Auto-buscar cuando cambia el termino (con debounce en la vista)
        }
    }
}

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

        public ObservableCollection<Customer> Customers { get; } = new();

        public event EventHandler<Customer>? CustomerSelected;
        public event EventHandler? CreateNewRequested;
        public event EventHandler? Cancelled;

        public CustomerSearchViewModel(CustomerService customerService)
        {
            _customerService = customerService;
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
            switch (key.ToUpper())
            {
                case "ENTER":
                    if (SelectedCustomer != null)
                        SelectCustomer();
                    else
                        _ = SearchAsync();
                    break;
                case "F5":
                    CreateNew();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }

        partial void OnSearchTermChanged(string value)
        {
            // Auto-buscar cuando cambia el termino (con debounce en la vista)
        }
    }
}

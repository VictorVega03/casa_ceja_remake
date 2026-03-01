using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.ViewModels.POS
{
    public partial class CustomerSearchViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;

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
        public event EventHandler? ExportRequested;

        public bool HasSelectedCustomer => SelectedCustomer != null;

        public CustomerSearchViewModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            OnPropertyChanged(nameof(HasSelectedCustomer));
            ViewCreditsCommand.NotifyCanExecuteChanged();
            ViewLayawaysCommand.NotifyCanExecuteChanged();
            SelectCustomerCommand.NotifyCanExecuteChanged();
        }

        partial void OnShowActionButtonsChanged(bool value)
        {
            ViewCreditsCommand.NotifyCanExecuteChanged();
            ViewLayawaysCommand.NotifyCanExecuteChanged();
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

        [RelayCommand(CanExecute = nameof(HasSelectedCustomer))]
        private void SelectCustomer()
        {
            if (SelectedCustomer == null)
            {
                StatusMessage = "Seleccione un cliente";
                return;
            }

            CustomerSelected?.Invoke(this, SelectedCustomer);
        }

        [RelayCommand(CanExecute = nameof(CanViewCreditsOrLayaways))]
        private void ViewCredits()
        {
            if (SelectedCustomer == null)
            {
                StatusMessage = "Seleccione un cliente";
                return;
            }

            ViewCustomerCreditsLayaways?.Invoke(this, (SelectedCustomer, true));
        }

        [RelayCommand(CanExecute = nameof(CanViewCreditsOrLayaways))]
        private void ViewLayaways()
        {
            if (SelectedCustomer == null)
            {
                StatusMessage = "Seleccione un cliente";
                return;
            }

            ViewCustomerCreditsLayaways?.Invoke(this, (SelectedCustomer, false));
        }

        private bool CanViewCreditsOrLayaways()
        {
            return SelectedCustomer != null && ShowActionButtons;
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

        [RelayCommand]
        private void ExportToExcel()
        {
            ExportRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Obtiene las columnas de exportación para el reporte de clientes.
        /// Solo incluye los campos del formulario de alta.
        /// </summary>
        public List<ExportColumn<Customer>> GetExportColumns()
        {
            return new List<ExportColumn<Customer>>
            {
                new() { Header = "ID", ValueSelector = c => c.Id, Width = 10 },
                new() { Header = "Nombre", ValueSelector = c => c.Name, Width = 30 },
                new() { Header = "Teléfono", ValueSelector = c => c.Phone, Width = 18 },
                new() { Header = "Email", ValueSelector = c => c.Email, Width = 30 },
                new() { Header = "RFC", ValueSelector = c => c.Rfc, Width = 15 },
                new() { Header = "Calle", ValueSelector = c => c.Street, Width = 30 },
                new() { Header = "Ciudad", ValueSelector = c => c.City, Width = 20 }
            };
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

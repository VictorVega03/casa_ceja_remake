using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Views.POS
{
    public partial class CustomerSearchView : Window
    {
        private CustomerSearchViewModel? _viewModel;

        public CustomerSearchView()
        {
            InitializeComponent();

            // Focus search box when loaded
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            // Asegurar que la ventana tenga focus para recibir eventos de teclado
            Focus();
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CustomerSearchViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CustomerSelected += OnCustomerSelected;
                _viewModel.CreateNewRequested += OnCreateNewRequested;
                _viewModel.Cancelled += OnCancelled;
                
                // Cargar clientes iniciales
                await _viewModel.InitializeAsync();
            }
            
            SearchBox?.Focus();
            
            // Establecer handler para Enter en el DataGrid
            var dataGrid = this.FindControl<DataGrid>("CustomersGrid");
            if (dataGrid != null)
            {
                // Usar PreviewKeyDown (Tunneling) para interceptar Enter ANTES del DataGrid
                dataGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // PreviewKeyDown (Tunneling) se ejecuta ANTES de que el DataGrid procese la tecla
            if (e.Key == Key.Enter && _viewModel?.SelectedCustomer != null)
            {
                _viewModel.SelectCustomerCommand.Execute(null);
                e.Handled = true; // Evitar que el DataGrid navegue a la siguiente fila
            }
        }

        private void OnCustomerSelected(object? sender, Customer e)
        {
            Tag = ("CustomerSelected", e);
            Close();
        }

        private void OnCreateNewRequested(object? sender, EventArgs e)
        {
            Tag = "CreateNew";
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is CustomerSearchViewModel vm)
            {
                // Enter con lógica condicional
                if (e.Key == Key.Enter)
                {
                    if (vm.SelectedCustomer != null)
                    {
                        vm.SelectCustomerCommand.Execute(null);
                    }
                    else
                    {
                        vm.SearchCommand.Execute(null);
                    }
                    e.Handled = true;
                    return;
                }

                // Delegar otros atajos al ViewModel
                vm.HandleKeyPress(e.Key.ToString());
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CustomerSelected -= OnCustomerSelected;
                _viewModel.CreateNewRequested -= OnCreateNewRequested;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

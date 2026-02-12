using System;
using System.Collections.Generic;
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
                _viewModel.ViewCustomerCreditsLayaways += OnViewCustomerCreditsLayaways;
                _viewModel.Cancelled += OnCancelled;
                _viewModel.ExportRequested += OnExportRequested;
                
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

        private async void OnViewCustomerCreditsLayaways(object? sender, (Customer customer, bool isCreditsMode) e)
        {
            // NO cerrar la vista, mantenerla abierta
            // Abrir CustomerCreditsLayawaysView como diálogo hijo
            await ShowCustomerCreditsLayawaysDialog(e.customer, e.isCreditsMode);
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        private async System.Threading.Tasks.Task ShowCustomerCreditsLayawaysDialog(Customer customer, bool isCreditsMode)
        {
            // Obtener los servicios necesarios
            var app = (Avalonia.Application.Current as App);
            if (app == null) return;

            var creditService = app.GetCreditService();
            var layawayService = app.GetLayawayService();
            var authService = app.GetAuthService();

            if (creditService == null || layawayService == null || authService == null)
            {
                return;
            }

            // Crear vista y ViewModel de créditos/apartados
            var creditsLayawaysView = new CustomerCreditsLayawaysView();
            var creditsLayawaysViewModel = new CustomerCreditsLayawaysViewModel(
                creditService,
                layawayService,
                authService);

            creditsLayawaysViewModel.SetCustomerAndMode(customer, isCreditsMode);
            await creditsLayawaysViewModel.InitializeAsync();
            creditsLayawaysView.DataContext = creditsLayawaysViewModel;

            // Mostrar como diálogo HIJO - CustomerSearchView permanece abierta
            await creditsLayawaysView.ShowDialog(this);
            
            // Cuando regrese aquí, dar focus al SearchBox
            SearchBox?.Focus();
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
                        if (vm.SelectCustomerCommand.CanExecute(null))
                        {
                            vm.SelectCustomerCommand.Execute(null);
                        }
                    }
                    else
                    {
                        vm.SearchCommand.Execute(null);
                    }
                    e.Handled = true;
                    return;
                }

                // Manejar atajos de créditos/apartados solo si están visibles y habilitados
                if (e.Key == Key.F3)
                {
                    if (vm.ViewCreditsCommand.CanExecute(null))
                    {
                        vm.ViewCreditsCommand.Execute(null);
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F4)
                {
                    if (vm.ViewLayawaysCommand.CanExecute(null))
                    {
                        vm.ViewLayawaysCommand.Execute(null);
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F5)
                {
                    vm.CreateNewCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F8)
                {
                    vm.ExportToExcelCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Escape)
                {
                    vm.CancelCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CustomerSelected -= OnCustomerSelected;
                _viewModel.CreateNewRequested -= OnCreateNewRequested;
                _viewModel.ViewCustomerCreditsLayaways -= OnViewCustomerCreditsLayaways;
                _viewModel.Cancelled -= OnCancelled;
                _viewModel.ExportRequested -= OnExportRequested;
            }
            base.OnClosed(e);
        }

        private async void OnExportRequested(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;

            await ExportHelper.ExportSingleSheetAsync(
                this,
                _viewModel.Customers,
                _viewModel.GetExportColumns(),
                "Clientes",
                "Lista de Clientes",
                "Lista de Clientes");
        }
    }
}

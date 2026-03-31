using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CustomerDetailView : Window
    {
        private CustomerDetailViewModel? _viewModel;

        public CustomerDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CustomerDetailViewModel;

            if (_viewModel != null)
            {
                _viewModel.ViewCreditsRequested += OnViewCreditsRequested;
                _viewModel.ViewLayawaysRequested += OnViewLayawaysRequested;
                _viewModel.DeleteRequested += OnDeleteRequested;
                _viewModel.CustomerDeleted += OnCustomerDeleted;
                _viewModel.CloseRequested += OnCloseRequested;
            }

            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.HandleKeyPress(e.Key.ToString());
            }
            base.OnKeyDown(e);
        }

        private void OnViewCreditsRequested(object? sender, EventArgs e)
        {
            Tag = "ViewCredits";
            Close();
        }

        private void OnViewLayawaysRequested(object? sender, EventArgs e)
        {
            Tag = "ViewLayaways";
            Close();
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private async void OnDeleteRequested(object? sender, Customer customer)
        {
            if (_viewModel == null)
                return;

            var confirmed = await DialogHelper.ShowConfirmDialog(
                this,
                "Eliminar cliente",
                $"¿Seguro que deseas desactivar al cliente '{customer.Name}'?\n\n" +
                "Esta acción es Soft Delete: no borra registros relacionados.");

            if (!confirmed)
                return;

            var ok = await _viewModel.ConfirmDeleteAsync();
            if (ok)
            {
                await DialogHelper.ShowMessageDialog(this, "Éxito", "Cliente desactivado correctamente.");
                Tag = ("CustomerDeleted", customer);
                Close();
            }
            else
            {
                await DialogHelper.ShowMessageDialog(this, "Error", "No se pudo desactivar el cliente.");
            }
        }

        private void OnCustomerDeleted(object? sender, Customer customer)
        {
            // Evento disponible para futuros flujos. La UI ya cierra en OnDeleteRequested.
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ViewCreditsRequested -= OnViewCreditsRequested;
                _viewModel.ViewLayawaysRequested -= OnViewLayawaysRequested;
                _viewModel.DeleteRequested -= OnDeleteRequested;
                _viewModel.CustomerDeleted -= OnCustomerDeleted;
                _viewModel.CloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}

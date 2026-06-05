using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.Admin
{
    public partial class SupplierListViewModel : ViewModelBase
    {
        private readonly SupplierService _supplierService;
        private Window? _parentWindow;
        private List<Supplier> _allSuppliers = new();

        [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
        [ObservableProperty] private Supplier? _selectedSupplier;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public event EventHandler? AddRequested;
        public event EventHandler<Supplier>? EditRequested;
        public event EventHandler<Supplier>? DeactivateRequested;
        public event EventHandler? GoBackRequested;

        public SupplierListViewModel(SupplierService supplierService)
        {
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        }

        public void SetParentWindow(Window window) => _parentWindow = window;

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsLoading = true;
            StatusMessage = "Cargando proveedores...";
            try
            {
                _allSuppliers = await _supplierService.GetAllAsync();
                ApplyFilter();
                StatusMessage = $"{_allSuppliers.Count} proveedor(es) encontrado(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar proveedores: {ex.Message}";
                Console.WriteLine($"[SupplierListVM] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        partial void OnSelectedSupplierChanged(Supplier? value)
        {
            EditCommand.NotifyCanExecuteChanged();
            DeactivateCommand.NotifyCanExecuteChanged();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Suppliers = new ObservableCollection<Supplier>(_allSuppliers);
            }
            else
            {
                var q = SearchText.Trim().ToLowerInvariant();
                var filtered = _allSuppliers.Where(s =>
                    s.Name.ToLowerInvariant().Contains(q));
                Suppliers = new ObservableCollection<Supplier>(filtered);
            }
        }

        [RelayCommand]
        private void Add() => AddRequested?.Invoke(this, EventArgs.Empty);

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void Edit()
        {
            if (SelectedSupplier != null)
                EditRequested?.Invoke(this, SelectedSupplier);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeactivateAsync()
        {
            if (SelectedSupplier == null || _parentWindow == null) return;

            var confirmed = await DialogHelper.ShowConfirmDialog(
                _parentWindow,
                "Confirmar eliminación",
                $"¿Dar de baja el proveedor '{SelectedSupplier.Name}'?\n\nSe marcará como inactivo y dejará de aparecer en el sistema.");

            if (!confirmed) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _supplierService.ApiClient,
                () => _supplierService.DeactivateAsync(SelectedSupplier.Id),
                $"Proveedor '{SelectedSupplier.Name}' dado de baja exitosamente.");

            if (success)
                await LoadAsync();
        }

        private bool HasSelection => SelectedSupplier != null;

        [RelayCommand]
        private void GoBack() => GoBackRequested?.Invoke(this, EventArgs.Empty);

        public async Task RefreshAsync() => await LoadAsync();
    }
}

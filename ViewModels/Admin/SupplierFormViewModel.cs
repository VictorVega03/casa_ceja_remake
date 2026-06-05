using System;
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
    public partial class SupplierFormViewModel : ViewModelBase
    {
        private readonly SupplierService _supplierService;
        private readonly Supplier? _existingSupplier;
        private Window? _parentWindow;

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _phone = string.Empty;
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _address = string.Empty;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _hasError;

        public string Title => IsEditing ? "EDITAR PROVEEDOR" : "NUEVO PROVEEDOR";

        public event EventHandler? SaveCompleted;
        public event EventHandler? CloseRequested;

        public SupplierFormViewModel(SupplierService supplierService, Supplier? existingSupplier = null)
        {
            _supplierService  = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _existingSupplier = existingSupplier;
            IsEditing         = existingSupplier != null;

            if (existingSupplier != null)
            {
                Name    = existingSupplier.Name;
                Phone   = existingSupplier.Phone ?? string.Empty;
                Email   = existingSupplier.Email ?? string.Empty;
                Address = existingSupplier.Address ?? string.Empty;
            }
        }

        public void SetParentWindow(Window window) => _parentWindow = window;

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (_parentWindow == null) return;

            var validationError = Validate();
            if (validationError != null)
            {
                HasError = true;
                StatusMessage = validationError;
                return;
            }

            string? operationMessage = null;
            var isUpdating = IsEditing && _existingSupplier != null;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _supplierService.ApiClient,
                async () =>
                {
                    if (isUpdating)
                    {
                        var supplierToUpdate = new Supplier
                        {
                            Id         = _existingSupplier!.Id,
                            Name       = Name.Trim(),
                            Phone      = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                            Email      = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                            Address    = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                            Active     = _existingSupplier.Active,
                            CreatedAt  = _existingSupplier.CreatedAt,
                            UpdatedAt  = DateTime.Now,
                            SyncStatus = _existingSupplier.SyncStatus,
                            LastSync   = _existingSupplier.LastSync,
                        };

                        var result = await _supplierService.UpdateAsync(supplierToUpdate);
                        operationMessage = result.Message;
                        return result;
                    }
                    else
                    {
                        var newSupplier = new Supplier
                        {
                            Name    = Name.Trim(),
                            Phone   = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                            Email   = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                        };

                        var result = await _supplierService.CreateAsync(newSupplier);
                        operationMessage = result.Message;
                        return (result.Success, result.Message);
                    }
                },
                isUpdating ? "Proveedor actualizado exitosamente." : "Proveedor creado exitosamente.",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                HasError = false;
                StatusMessage = operationMessage ?? string.Empty;
                SaveCompleted?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                HasError = true;
                StatusMessage = operationMessage ?? StatusMessage;
            }
        }

        [RelayCommand]
        private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

        private string? Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "El nombre del proveedor es requerido.";
            return null;
        }
    }
}

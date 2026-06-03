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
    public partial class BranchFormViewModel : ViewModelBase
    {
        private readonly BranchService _branchService;
        private readonly Branch? _existingBranch;
        private Window? _parentWindow;

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _address = string.Empty;
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _razonSocial = string.Empty;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _hasError;

        public string Title => IsEditing ? "EDITAR SUCURSAL" : "NUEVA SUCURSAL";

        public event EventHandler? SaveCompleted;
        public event EventHandler? CloseRequested;

        public BranchFormViewModel(BranchService branchService, Branch? existingBranch = null)
        {
            _branchService  = branchService ?? throw new ArgumentNullException(nameof(branchService));
            _existingBranch = existingBranch;
            IsEditing       = existingBranch != null;

            if (existingBranch != null)
            {
                Name        = existingBranch.Name;
                Address     = existingBranch.Address;
                Email       = existingBranch.Email;
                RazonSocial = existingBranch.RazonSocial;
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
            var isUpdating = IsEditing && _existingBranch != null;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _branchService.ApiClient,
                async () =>
                {
                    if (isUpdating)
                    {
                        var branchToUpdate = new Branch
                        {
                            Id          = _existingBranch!.Id,
                            Name        = Name.Trim(),
                            Address     = Address.Trim(),
                            Email       = Email.Trim(),
                            RazonSocial = RazonSocial.Trim(),
                            Active      = _existingBranch.Active,
                            CreatedAt   = _existingBranch.CreatedAt,
                            UpdatedAt   = DateTime.Now,
                            SyncStatus  = _existingBranch.SyncStatus,
                            LastSync    = _existingBranch.LastSync,
                        };

                        var result = await _branchService.UpdateAsync(branchToUpdate);
                        operationMessage = result.Message;
                        return result;
                    }
                    else
                    {
                        var newBranch = new Branch
                        {
                            Name        = Name.Trim(),
                            Address     = Address.Trim(),
                            Email       = Email.Trim(),
                            RazonSocial = RazonSocial.Trim(),
                        };

                        var result = await _branchService.CreateAsync(newBranch);
                        operationMessage = result.Message;
                        return (result.Success, result.Message);
                    }
                },
                isUpdating ? "Sucursal actualizada exitosamente." : "Sucursal creada exitosamente.",
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
                return "El nombre de la sucursal es requerido.";
            return null;
        }
    }
}

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
    public partial class BranchListViewModel : ViewModelBase
    {
        private readonly BranchService _branchService;
        private Window? _parentWindow;
        private List<Branch> _allBranches = new();

        [ObservableProperty] private ObservableCollection<Branch> _branches = new();
        [ObservableProperty] private Branch? _selectedBranch;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public event EventHandler? AddRequested;
        public event EventHandler<Branch>? EditRequested;
        public event EventHandler<Branch>? DeactivateRequested;
        public event EventHandler? GoBackRequested;

        public BranchListViewModel(BranchService branchService)
        {
            _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
        }

        public void SetParentWindow(Window window) => _parentWindow = window;

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsLoading = true;
            StatusMessage = "Cargando sucursales...";
            try
            {
                _allBranches = await _branchService.GetAllAsync();
                ApplyFilter();
                StatusMessage = $"{_allBranches.Count} sucursal(es) encontrada(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar sucursales: {ex.Message}";
                Console.WriteLine($"[BranchListVM] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        partial void OnSelectedBranchChanged(Branch? value)
        {
            EditCommand.NotifyCanExecuteChanged();
            DeactivateCommand.NotifyCanExecuteChanged();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Branches = new ObservableCollection<Branch>(_allBranches);
            }
            else
            {
                var q = SearchText.Trim().ToLowerInvariant();
                var filtered = _allBranches.Where(b =>
                    b.Name.ToLowerInvariant().Contains(q));
                Branches = new ObservableCollection<Branch>(filtered);
            }
        }

        [RelayCommand]
        private void Add() => AddRequested?.Invoke(this, EventArgs.Empty);

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void Edit()
        {
            if (SelectedBranch != null)
                EditRequested?.Invoke(this, SelectedBranch);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeactivateAsync()
        {
            if (SelectedBranch == null || _parentWindow == null) return;

            var confirmed = await DialogHelper.ShowConfirmDialog(
                _parentWindow,
                "Confirmar eliminación",
                $"¿Dar de baja la sucursal '{SelectedBranch.Name}'?\n\nSe marcará como inactiva y dejará de aparecer en el sistema.");

            if (!confirmed) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _branchService.ApiClient,
                () => _branchService.DeactivateAsync(SelectedBranch.Id),
                $"Sucursal '{SelectedBranch.Name}' dada de baja exitosamente.");

            if (success)
                await LoadAsync();
        }

        private bool HasSelection => SelectedBranch != null;

        [RelayCommand]
        private void GoBack() => GoBackRequested?.Invoke(this, EventArgs.Empty);

        public async Task RefreshAsync() => await LoadAsync();
    }
}

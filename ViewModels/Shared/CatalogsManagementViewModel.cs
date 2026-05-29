using CasaCejaRemake.Models;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Services;
using casa_ceja_remake.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Shared
{
    public partial class CatalogsManagementViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly ApiClient _apiClient;
        private Window? _parentWindow;

        public bool IsAdminMode { get; }

        public event EventHandler? GoBackRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler<string>? ShowErrorRequested;

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Unit> Units { get; } = new();

        [ObservableProperty]
        private Category? _selectedCategory;

        [ObservableProperty]
        private Unit? _selectedUnit;

        [ObservableProperty]
        private string _newCategoryName = string.Empty;

        [ObservableProperty]
        private string _newUnitName = string.Empty;

        [ObservableProperty]
        private bool _isSaving = false;

        public CatalogsManagementViewModel(InventoryService inventoryService, ApiClient apiClient, bool isAdminMode = false)
        {
            _inventoryService = inventoryService;
            _apiClient = apiClient;
            IsAdminMode = isAdminMode;
            _ = LoadDataAsync();
        }

        public void SetParentWindow(Window parentWindow)
        {
            _parentWindow = parentWindow;
        }

        private async Task LoadDataAsync()
        {
            Categories.Clear();
            var cats = await _inventoryService.GetCategoriesAsync();
            // Filter out placeholder ID 0 if any
            foreach (var c in cats.Where(x => x.Id > 0)) Categories.Add(c);

            Units.Clear();
            var units = await _inventoryService.GetUnitsAsync();
            foreach (var u in units.Where(x => x.Id > 0)) Units.Add(u);
        }

        [RelayCommand]
        private async Task AddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName) || _parentWindow == null) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    if (Categories.Any(c => c.Name.Equals(NewCategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
                        return (false, "Ya existe una categoría con ese nombre.");

                    var cat = new Category { Name = NewCategoryName.Trim(), Active = true };
                    var result = await _inventoryService.SaveCategoryAsync(cat);
                    return (result.Success, result.Message);
                },
                "Categoría guardada",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                NewCategoryName = string.Empty;
                await LoadDataAsync();
            }
        }

        public async Task<bool> SaveCategoryEditAsync(Category? category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name) || _parentWindow == null) return false;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    var result = await _inventoryService.SaveCategoryAsync(category);
                    return (result.Success, result.Message);
                },
                "Categoría guardada",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                await LoadDataAsync();
                return true;
            }

            return false;
        }

        [RelayCommand]
        private async Task AddUnitAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUnitName) || _parentWindow == null) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    if (Units.Any(u => u.Name.Equals(NewUnitName.Trim(), StringComparison.OrdinalIgnoreCase)))
                        return (false, "Ya existe una medida con ese nombre.");

                    var unit = new Unit { Name = NewUnitName.Trim(), Active = true };
                    var result = await _inventoryService.SaveUnitAsync(unit);
                    return (result.Success, result.Message);
                },
                "Medida guardada",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                NewUnitName = string.Empty;
                await LoadDataAsync();
            }
        }

        public async Task<bool> SaveUnitEditAsync(Unit? unit)
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.Name) || _parentWindow == null) return false;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    var result = await _inventoryService.SaveUnitAsync(unit);
                    return (result.Success, result.Message);
                },
                "Medida guardada",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                await LoadDataAsync();
                return true;
            }

            return false;
        }

        [RelayCommand]
        private async Task DeactivateCategoryAsync(Category? category)
        {
            if (category == null || _parentWindow == null) return;

            bool confirmed = await DialogHelper.ShowConfirmDialog(
                _parentWindow,
                "Eliminar categoría",
                $"¿Estás seguro de que deseas eliminar la categoría '{category.Name}'?\nEsta acción no se puede deshacer.");

            if (!confirmed) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    var result = await _inventoryService.DeactivateCategoryAsync(category);
                    return (result.Success, result.Message);
                },
                $"Categoría '{category.Name}' eliminada exitosamente.",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                SelectedCategory = null;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task DeactivateUnitAsync(Unit? unit)
        {
            if (unit == null || _parentWindow == null) return;

            bool confirmed = await DialogHelper.ShowConfirmDialog(
                _parentWindow,
                "Eliminar medida",
                $"¿Estás seguro de que deseas eliminar la medida '{unit.Name}'?\nEsta acción no se puede deshacer.");

            if (!confirmed) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    var result = await _inventoryService.DeactivateUnitAsync(unit);
                    return (result.Success, result.Message);
                },
                $"Medida '{unit.Name}' eliminada exitosamente.",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                SelectedUnit = null;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private void Close()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

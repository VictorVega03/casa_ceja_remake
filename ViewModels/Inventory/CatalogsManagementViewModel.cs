using CasaCejaRemake.Models;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class CatalogsManagementViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;
        private readonly ApiClient _apiClient;
        private readonly bool _isAdminMode;
        private Window? _parentWindow;

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
            _isAdminMode = isAdminMode;
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
            if (string.IsNullOrWhiteSpace(NewCategoryName)) return;

            if (!_isAdminMode)
            {
                if (Categories.Any(c => c.Name.Equals(NewCategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    ShowErrorRequested?.Invoke(this, "Ya existe una categoría con ese nombre.");
                    return;
                }

                var cat = new Category { Name = NewCategoryName.Trim(), Active = true };
                var result = await _inventoryService.SaveCategoryAsync(cat, false);
                if (!result.Success)
                {
                    ShowErrorRequested?.Invoke(this, result.Message);
                    return;
                }

                NewCategoryName = string.Empty;
                await LoadDataAsync();
                return;
            }

            if (_parentWindow == null) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    if (Categories.Any(c => c.Name.Equals(NewCategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
                        return (false, "Ya existe una categoría con ese nombre.");

                    var cat = new Category { Name = NewCategoryName.Trim(), Active = true };
                    var result = await _inventoryService.SaveCategoryAsync(cat, true);
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
            if (category == null || string.IsNullOrWhiteSpace(category.Name)) return false;
            if (!_isAdminMode)
            {
                var result = await _inventoryService.SaveCategoryAsync(category, false);
                if (!result.Success)
                {
                    ShowErrorRequested?.Invoke(this, result.Message);
                    return false;
                }

                await LoadDataAsync();
                return true;
            }

            if (_parentWindow == null) return false;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    var result = await _inventoryService.SaveCategoryAsync(category, true);
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
            if (string.IsNullOrWhiteSpace(NewUnitName)) return;

            if (!_isAdminMode)
            {
                if (Units.Any(u => u.Name.Equals(NewUnitName.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    ShowErrorRequested?.Invoke(this, "Ya existe una medida con ese nombre.");
                    return;
                }

                var unit = new Unit { Name = NewUnitName.Trim(), Active = true };
                var result = await _inventoryService.SaveUnitAsync(unit, false);
                if (!result.Success)
                {
                    ShowErrorRequested?.Invoke(this, result.Message);
                    return;
                }

                NewUnitName = string.Empty;
                await LoadDataAsync();
                return;
            }

            if (_parentWindow == null) return;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    if (Units.Any(u => u.Name.Equals(NewUnitName.Trim(), StringComparison.OrdinalIgnoreCase)))
                        return (false, "Ya existe una medida con ese nombre.");

                    var unit = new Unit { Name = NewUnitName.Trim(), Active = true };
                    var result = await _inventoryService.SaveUnitAsync(unit, true);
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
            if (unit == null || string.IsNullOrWhiteSpace(unit.Name)) return false;
            if (!_isAdminMode)
            {
                var result = await _inventoryService.SaveUnitAsync(unit, false);
                if (!result.Success)
                {
                    ShowErrorRequested?.Invoke(this, result.Message);
                    return false;
                }

                await LoadDataAsync();
                return true;
            }

            if (_parentWindow == null) return false;

            var success = await AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    var result = await _inventoryService.SaveUnitAsync(unit, true);
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
        private void Close()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

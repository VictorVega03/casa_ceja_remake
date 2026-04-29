using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class CatalogsManagementViewModel : ViewModelBase
    {
        private readonly InventoryService _inventoryService;

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

        public CatalogsManagementViewModel(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            _ = LoadDataAsync();
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

            if (Categories.Any(c => c.Name.Equals(NewCategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ShowErrorRequested?.Invoke(this, "Ya existe una categoría con ese nombre.");
                return;
            }

            var cat = new Category { Name = NewCategoryName.Trim(), Active = true };
            await _inventoryService.SaveCategoryAsync(cat);
            NewCategoryName = string.Empty;
            await LoadDataAsync();
        }

        public async Task SaveCategoryEditAsync(Category? category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name)) return;
            await _inventoryService.SaveCategoryAsync(category);
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task AddUnitAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUnitName)) return;

            if (Units.Any(u => u.Name.Equals(NewUnitName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ShowErrorRequested?.Invoke(this, "Ya existe una medida con ese nombre.");
                return;
            }

            var unit = new Unit { Name = NewUnitName.Trim(), Active = true };
            await _inventoryService.SaveUnitAsync(unit);
            NewUnitName = string.Empty;
            await LoadDataAsync();
        }

        public async Task SaveUnitEditAsync(Unit? unit)
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.Name)) return;
            await _inventoryService.SaveUnitAsync(unit);
            await LoadDataAsync();
        }

        [RelayCommand]
        private void Close()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

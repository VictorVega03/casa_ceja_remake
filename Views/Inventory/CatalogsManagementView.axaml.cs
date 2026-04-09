using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Inventory;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class CatalogsManagementView : Window
    {
        public CatalogsManagementView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            this.Opened += OnOpened;
        }

        private void OnOpened(object? sender, System.EventArgs e)
        {
            if (DataContext is CatalogsManagementViewModel vm)
            {
                vm.ShowErrorRequested += async (s, errorMsg) =>
                {
                    await DialogHelper.ShowMessageDialog(this, "Aviso", errorMsg);
                };
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is CatalogsManagementViewModel vm)
                {
                    vm.CloseCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        // ── Categorías ────────────────────────────────────────────────────────

        private async void OnAddCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CatalogsManagementViewModel vm) return;

            var name = await DialogHelper.ShowInputDialog(
                this,
                "Nueva categoría",
                "Nombre de la nueva categoría:");

            if (string.IsNullOrWhiteSpace(name)) return;

            vm.NewCategoryName = name.Trim();
            vm.AddCategoryCommand.Execute(null);
        }

        private async void OnEditCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CatalogsManagementViewModel vm || vm.SelectedCategory == null) return;

            var newName = await DialogHelper.ShowInputDialog(
                this,
                "Editar categoría",
                "Nombre de la categoría:",
                vm.SelectedCategory.Name);

            if (newName == null) return;
            newName = newName.Trim();
            if (string.IsNullOrEmpty(newName) || newName == vm.SelectedCategory.Name) return;

            vm.SelectedCategory.Name = newName;
            await vm.SaveCategoryEditAsync(vm.SelectedCategory);
        }

        private async void OnDeleteCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CatalogsManagementViewModel vm || vm.SelectedCategory == null) return;

            var confirmed = await DialogHelper.ShowConfirmDialog(
                this,
                "Eliminar categoría",
                $"¿Estás seguro de que deseas eliminar la categoría \"{vm.SelectedCategory.Name}\"?\n\nEsta acción no se puede deshacer.");

            if (confirmed)
                vm.DeleteCategoryCommand.Execute(vm.SelectedCategory);
        }

        // ── Unidades ──────────────────────────────────────────────────────────

        private async void OnAddUnitClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CatalogsManagementViewModel vm) return;

            var name = await DialogHelper.ShowInputDialog(
                this,
                "Nueva unidad de medida",
                "Nombre de la nueva unidad de medida:");

            if (string.IsNullOrWhiteSpace(name)) return;

            vm.NewUnitName = name.Trim();
            vm.AddUnitCommand.Execute(null);
        }

        private async void OnEditUnitClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CatalogsManagementViewModel vm || vm.SelectedUnit == null) return;

            var newName = await DialogHelper.ShowInputDialog(
                this,
                "Editar unidad de medida",
                "Nombre de la unidad de medida:",
                vm.SelectedUnit.Name);

            if (newName == null) return;
            newName = newName.Trim();
            if (string.IsNullOrEmpty(newName) || newName == vm.SelectedUnit.Name) return;

            vm.SelectedUnit.Name = newName;
            await vm.SaveUnitEditAsync(vm.SelectedUnit);
        }

        private async void OnDeleteUnitClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CatalogsManagementViewModel vm || vm.SelectedUnit == null) return;

            var confirmed = await DialogHelper.ShowConfirmDialog(
                this,
                "Eliminar unidad de medida",
                $"¿Estás seguro de que deseas eliminar la unidad \"{vm.SelectedUnit.Name}\"?\n\nEsta acción no se puede deshacer.");

            if (confirmed)
                vm.DeleteUnitCommand.Execute(vm.SelectedUnit);
        }
    }
}

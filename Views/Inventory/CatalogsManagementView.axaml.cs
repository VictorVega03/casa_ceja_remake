using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CasaCejaRemake.ViewModels.Inventory;
using casa_ceja_remake.Helpers;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class CatalogsManagementView : Window
    {
        private CatalogsManagementViewModel? _viewModel;

        public CatalogsManagementView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnViewPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object? sender, System.EventArgs e)
        {
            if (DataContext is CatalogsManagementViewModel vm)
            {
                _viewModel = vm;
                vm.ShowErrorRequested += async (s, errorMsg) =>
                {
                    await DialogHelper.ShowMessageDialog(this, "Aviso", errorMsg);
                };

                vm.Categories.CollectionChanged += OnCategoriesCollectionChanged;
                vm.Units.CollectionChanged += OnUnitsCollectionChanged;
            }

            CategoryGrid?.AddHandler(InputElement.KeyDownEvent, OnViewPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            UnitGrid?.AddHandler(InputElement.KeyDownEvent, OnViewPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);

            Dispatcher.UIThread.Post(() =>
            {
                EnsureFirstRowSelected(CategoryGrid, focusGrid: SectionsTabControl?.SelectedIndex == 0);
                EnsureFirstRowSelected(UnitGrid, focusGrid: false);
            }, DispatcherPriority.Loaded);
        }

        private void OnClosed(object? sender, System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.Categories.CollectionChanged -= OnCategoriesCollectionChanged;
                _viewModel.Units.CollectionChanged -= OnUnitsCollectionChanged;
                _viewModel = null;
            }
        }

        private void OnCategoriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                EnsureFirstRowSelected(CategoryGrid, focusGrid: SectionsTabControl?.SelectedIndex == 0);
            }, DispatcherPriority.Loaded);
        }

        private void OnUnitsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                EnsureFirstRowSelected(UnitGrid, focusGrid: SectionsTabControl?.SelectedIndex == 1);
            }, DispatcherPriority.Loaded);
        }

        private void OnSectionsTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SectionsTabControl?.SelectedIndex == 0)
            {
                Dispatcher.UIThread.Post(() => EnsureFirstRowSelected(CategoryGrid, focusGrid: true), DispatcherPriority.Loaded);
                return;
            }

            if (SectionsTabControl?.SelectedIndex == 1)
            {
                Dispatcher.UIThread.Post(() => EnsureFirstRowSelected(UnitGrid, focusGrid: true), DispatcherPriority.Loaded);
            }
        }

        private static void EnsureFirstRowSelected(DataGrid? grid, bool focusGrid)
        {
            if (grid == null)
            {
                return;
            }

            var hasItems = grid.ItemsSource switch
            {
                ICollection collection => collection.Count > 0,
                IEnumerable enumerable => enumerable.Cast<object>().Any(),
                _ => false
            };

            if (!hasItems)
            {
                return;
            }

            if (grid.SelectedIndex < 0)
            {
                grid.SelectedIndex = 0;
            }

            if (focusGrid)
            {
                grid.Focus();
            }
        }

        private void OnViewPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            // Bloquear Enter en todo el diálogo para evitar que se vuelva a abrir.
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
            }
        }

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

    }
}

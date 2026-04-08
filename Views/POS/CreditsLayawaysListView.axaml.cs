using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreditsLayawaysListView : Window
    {
        private CreditsLayawaysListViewModel? _viewModel;

        public CreditsLayawaysListView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            // Asegurar que la ventana tenga focus para recibir eventos de teclado
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CreditsLayawaysListViewModel;

            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.ItemSelected += OnItemSelected;
                _viewModel.ExportRequested += OnExportRequested;

                // Escuchar cambios de filtro para actualizar colores de botones
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;

                // Seleccionar primer item automáticamente si hay items
                if (_viewModel.Items.Count > 0)
                    _viewModel.SelectedItem = _viewModel.Items[0];

                // Aplicar colores iniciales
                UpdateFilterButtonColors();
            }
            
            // Configurar el TextBox de búsqueda
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            if (searchTextBox != null)
            {
                searchTextBox.KeyDown += SearchTextBox_KeyDown;
            }
            
            // Establecer focus en el DataGrid
            var dataGrid = this.FindControl<DataGrid>("DataGridItems");
            if (dataGrid != null)
            {
                dataGrid.Focus();
                
                // Usar PreviewKeyDown (Tunneling) para interceptar Enter ANTES del DataGrid
                dataGrid.AddHandler(KeyDownEvent, DataGrid_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CreditsLayawaysListViewModel.FilterType) ||
                e.PropertyName == nameof(CreditsLayawaysListViewModel.FilterStatus))
            {
                UpdateFilterButtonColors();
            }
        }

        private static readonly IBrush ColorInactive  = new SolidColorBrush(Color.Parse("#3D3D3D"));
        private static readonly IBrush ColorTodos     = new SolidColorBrush(Color.Parse("#666666"));
        private static readonly IBrush ColorCredits   = new SolidColorBrush(Color.Parse("#4CAF50"));
        private static readonly IBrush ColorLayaways  = new SolidColorBrush(Color.Parse("#2196F3"));
        private static readonly IBrush ColorPending   = new SolidColorBrush(Color.Parse("#FF9800"));
        private static readonly IBrush ColorPaid      = new SolidColorBrush(Color.Parse("#00BCD4"));
        private static readonly IBrush ColorOverdue   = new SolidColorBrush(Color.Parse("#F44336"));

        private void UpdateFilterButtonColors()
        {
            if (_viewModel == null) return;

            var ft = _viewModel.FilterType;
            var fs = _viewModel.FilterStatus;

            // Tipo
            SetButtonBg("BtnFilterAll",      ft == ListFilterType.All      ? ColorTodos    : ColorInactive);
            SetButtonBg("BtnFilterCredits",  ft == ListFilterType.Credits  ? ColorCredits  : ColorInactive);
            SetButtonBg("BtnFilterLayaways", ft == ListFilterType.Layaways ? ColorLayaways : ColorInactive);

            // Estado
            SetButtonBg("BtnFilterPending",  fs == ListFilterStatus.Pending ? ColorPending  : ColorInactive);
            SetButtonBg("BtnFilterPaid",     fs == ListFilterStatus.Paid    ? ColorPaid     : ColorInactive);
            SetButtonBg("BtnFilterOverdue",  fs == ListFilterStatus.Overdue ? ColorOverdue  : ColorInactive);
        }

        private void SetButtonBg(string name, IBrush brush)
        {
            var btn = this.FindControl<Button>(name);
            if (btn != null) btn.Background = brush;
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnItemSelected(object? sender, ViewModels.POS.CreditLayawayListItemWrapper e)
        {
            Tag = ("ItemSelected", e);
            Close();
        }

        private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel != null)
            {
                _viewModel.ExecuteSearchCommand.Execute(null);
                e.Handled = true;
                
                // Mover focus al DataGrid después de buscar
                var dataGrid = this.FindControl<DataGrid>("DataGridItems");
                dataGrid?.Focus();
            }
        }

        private void DataGrid_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // PreviewKeyDown (Tunneling) se ejecuta ANTES de que el DataGrid procese la tecla
            if (e.Key == Key.Enter && _viewModel?.SelectedItem != null)
            {
                _viewModel.SelectItemCommand.Execute(null);
                e.Handled = true; // Evitar que el DataGrid navegue a la siguiente fila
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CloseCommand.Execute(null) },
                    { Key.Enter, () => _viewModel.SelectItemCommand.Execute(null) },
                    { Key.F1, () => _viewModel.SetFilterCommand.Execute("ALL") },
                    { Key.F2, () => _viewModel.SetFilterCommand.Execute("CREDITS") },
                    { Key.F3, () => _viewModel.SetFilterCommand.Execute("LAYAWAYS") },
                    { Key.F4, () => _viewModel.SetFilterCommand.Execute("PENDING") },
                    { Key.F5, () => _viewModel.SetFilterCommand.Execute("PAID") },
                    { Key.F6, () => _viewModel.SetFilterCommand.Execute("OVERDUE") },
                    { Key.F7, () => {
                        var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
                        searchTextBox?.Focus();
                        searchTextBox?.SelectAll();
                    }},
                    { Key.F8, () => _viewModel.ExportToExcelCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.ItemSelected -= OnItemSelected;
                _viewModel.ExportRequested -= OnExportRequested;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            base.OnClosed(e);
        }

        private async void OnExportRequested(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;

            var sheets = await _viewModel.PrepareMultiSheetExportAsync(App.ExportService);
            await ExportHelper.ExportMultiSheetAsync(
                this,
                sheets,
                "Creditos y Apartados");
        }
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreditsLayawaysListView : Window
    {
        private CreditsLayawaysListViewModel? _viewModel;

        public CreditsLayawaysListView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CreditsLayawaysListViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.ItemSelected += OnItemSelected;
                
                // Seleccionar primer item automáticamente si hay items
                if (_viewModel.Items.Count > 0)
                {
                    _viewModel.SelectedItem = _viewModel.Items[0];
                }
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

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnItemSelected(object? sender, ViewModels.POS.CreditLayawayListItemWrapper e)
        {
            Tag = ("ItemSelected", e);
            Close();
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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    _viewModel?.CloseCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    _viewModel?.SelectItemCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F1:
                    _viewModel?.SetFilterCommand.Execute("ALL");
                    e.Handled = true;
                    break;

                case Key.F2:
                    _viewModel?.SetFilterCommand.Execute("CREDITS");
                    e.Handled = true;
                    break;

                case Key.F3:
                    _viewModel?.SetFilterCommand.Execute("LAYAWAYS");
                    e.Handled = true;
                    break;

                case Key.F4:
                    _viewModel?.SetFilterCommand.Execute("PENDING");
                    e.Handled = true;
                    break;

                case Key.F5:
                    _viewModel?.SetFilterCommand.Execute("OVERDUE");
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.ItemSelected -= OnItemSelected;
            }
            base.OnClosed(e);
        }
    }
}

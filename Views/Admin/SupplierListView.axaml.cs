using System;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.Admin;

namespace CasaCejaRemake.Views.Admin
{
    public partial class SupplierListView : Window
    {
        private SupplierListViewModel? _viewModel;
        private bool _isDetailOpen;
        private Window? _activeDetailDialog;

        public SupplierListView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SupplierListViewModel vm)
            {
                _viewModel = vm;
                _viewModel.SetParentWindow(this);
                _viewModel.GoBackRequested += (_, _) => Close();

                var grid = this.FindControl<DataGrid>("SuppliersGrid");
                grid?.AddHandler(InputElement.KeyDownEvent, OnGridKeyDown, RoutingStrategies.Tunnel);

                await _viewModel.LoadCommand.ExecuteAsync(null);

                Dispatcher.UIThread.Post(() =>
                {
                    EnsureFirstRowSelected(this.FindControl<DataGrid>("SuppliersGrid"));
                }, DispatcherPriority.Loaded);
            }
        }

        private async void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (_viewModel?.SelectedSupplier == null || _isDetailOpen) return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await ShowSupplierDetail(_viewModel.SelectedSupplier);
            }
            else if (e.Key == Key.F2)
            {
                e.Handled = true;
                _viewModel.EditCommand.Execute(null);
            }
        }

        private async System.Threading.Tasks.Task ShowSupplierDetail(Supplier supplier)
        {
            _isDetailOpen = true;

            var dialog = new Window
            {
                Title = "Detalle de Proveedor",
                Width = 480,
                Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur },
            };
            _activeDetailDialog = dialog;

            var root = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
                BorderBrush = new SolidColorBrush(Color.Parse("#37474F")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
            };

            var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };

            // Header
            var header = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#252525")),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(20, 14),
            };
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            headerStack.Children.Add(new Border
            {
                Width = 6, Height = 20, CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.Parse("#37474F")),
                VerticalAlignment = VerticalAlignment.Center,
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = $"🚚  {supplier.Name}",
                FontSize = 16, FontWeight = FontWeight.Bold,
                Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center,
            });
            header.Child = headerStack;
            Grid.SetRow(header, 0);
            layout.Children.Add(header);

            // Fields
            var fieldsPanel = new StackPanel { Spacing = 12, Margin = new Thickness(20, 16) };
            AddDetailField(fieldsPanel, "TELÉFONO", supplier.Phone ?? string.Empty);
            AddDetailField(fieldsPanel, "CORREO ELECTRÓNICO", supplier.Email ?? string.Empty);
            AddDetailField(fieldsPanel, "DIRECCIÓN", supplier.Address ?? string.Empty);
            AddDetailField(fieldsPanel, "ESTADO", supplier.Active ? "Activo" : "Inactivo");

            Grid.SetRow(fieldsPanel, 1);
            layout.Children.Add(fieldsPanel);

            // Footer
            var footer = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#252525")),
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                Padding = new Thickness(20, 12),
            };
            var closeBtn = new Button
            {
                Content = "Cerrar (Esc)",
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Color.Parse("#37474F")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(18, 8),
            };
            closeBtn.Click += (_, _) => CloseSupplierDetailDialog(dialog);
            footer.Child = closeBtn;
            Grid.SetRow(footer, 2);
            layout.Children.Add(footer);

            root.Child = layout;
            dialog.Content = root;

            dialog.AddHandler(InputElement.KeyDownEvent, (_, e2) =>
            {
                if (e2.Key == Key.Escape || e2.Key == Key.Enter)
                {
                    CloseSupplierDetailDialog(dialog);
                    e2.Handled = true;
                }
            }, RoutingStrategies.Tunnel, handledEventsToo: true);

            dialog.Opened += (_, _) =>
            {
                dialog.Focus();
                closeBtn.Focus();
            };

            await dialog.ShowDialog(this);
            _isDetailOpen = false;
            _activeDetailDialog = null;
        }

        private void CloseSupplierDetailDialog(Window dialog)
        {
            if (dialog.IsVisible)
                dialog.Close();
        }

        private static void AddDetailField(StackPanel parent, string label, string value)
        {
            var row = new StackPanel { Spacing = 3 };
            row.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11, FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#888")),
            });
            row.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                FontSize = 13,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
            });
            parent.Children.Add(row);
        }

        private static void EnsureFirstRowSelected(DataGrid? grid)
        {
            if (grid == null) return;

            var hasItems = grid.ItemsSource switch
            {
                ICollection col => col.Count > 0,
                IEnumerable en  => en.Cast<object>().Any(),
                _               => false
            };

            if (!hasItems) return;

            if (grid.SelectedIndex < 0)
                grid.SelectedIndex = 0;

            grid.Focus();
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            if (_isDetailOpen)
            {
                if (_activeDetailDialog != null)
                    CloseSupplierDetailDialog(_activeDetailDialog);

                e.Handled = true;
                return;
            }

            if (!_isDetailOpen)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}

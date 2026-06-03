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
    public partial class BranchListView : Window
    {
        private BranchListViewModel? _viewModel;
        private bool _isDetailOpen;

        public BranchListView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is BranchListViewModel vm)
            {
                _viewModel = vm;
                _viewModel.SetParentWindow(this);
                _viewModel.GoBackRequested += (_, _) => Close();

                var grid = this.FindControl<DataGrid>("BranchesGrid");
                grid?.AddHandler(InputElement.KeyDownEvent, OnGridKeyDown, RoutingStrategies.Tunnel);

                await _viewModel.LoadCommand.ExecuteAsync(null);

                Dispatcher.UIThread.Post(() =>
                {
                    EnsureFirstRowSelected(this.FindControl<DataGrid>("BranchesGrid"));
                }, DispatcherPriority.Loaded);
            }
        }

        private async void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (_viewModel?.SelectedBranch == null || _isDetailOpen) return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await ShowBranchDetail(_viewModel.SelectedBranch);
            }
            else if (e.Key == Key.F2)
            {
                e.Handled = true;
                _viewModel.EditCommand.Execute(null);
            }
        }

        private async System.Threading.Tasks.Task ShowBranchDetail(Branch branch)
        {
            _isDetailOpen = true;

            var dialog = new Window
            {
                Title = "Detalle de Sucursal",
                Width = 480,
                Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur },
            };

            var root = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
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
                Background = new SolidColorBrush(Color.Parse("#2196F3")),
                VerticalAlignment = VerticalAlignment.Center,
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = $"🏢  {branch.Name}",
                FontSize = 16, FontWeight = FontWeight.Bold,
                Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center,
            });
            header.Child = headerStack;
            Grid.SetRow(header, 0);
            layout.Children.Add(header);

            // Fields
            var fieldsPanel = new StackPanel { Spacing = 12, Margin = new Thickness(20, 16) };
            AddDetailField(fieldsPanel, "DIRECCIÓN", branch.Address);
            AddDetailField(fieldsPanel, "CORREO ELECTRÓNICO", branch.Email);
            AddDetailField(fieldsPanel, "RAZÓN SOCIAL", branch.RazonSocial);
            AddDetailField(fieldsPanel, "ESTADO", branch.Active ? "Activa" : "Inactiva");

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
                Background = new SolidColorBrush(Color.Parse("#2196F3")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(18, 8),
            };
            closeBtn.Click += (_, _) => dialog.Close();
            footer.Child = closeBtn;
            Grid.SetRow(footer, 2);
            layout.Children.Add(footer);

            root.Child = layout;
            dialog.Content = root;

            dialog.KeyDown += (_, e2) =>
            {
                if (e2.Key == Key.Escape || e2.Key == Key.Enter)
                {
                    dialog.Close();
                    e2.Handled = true;
                }
            };

            await dialog.ShowDialog(this);
            _isDetailOpen = false;
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
            if (e.Key == Key.Escape && !_isDetailOpen)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}

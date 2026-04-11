using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Linq;

namespace CasaCejaRemake.Views.Shared
{
    public class StockByBranchDialog : Window
    {
        public StockByBranchDialog(Product product, List<ProductStockItem> items, bool isFromCache)
        {
            Title = $"Existencias — {product.Name}";
            MinWidth = 520;
            Width = 680;
            MaxWidth = 960;
            MinHeight = 380;
            Height = 480;
            MaxHeight = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = true;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"));

            var header = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
                BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 14)
            };

            var headerStack = new StackPanel { Spacing = 2 };
            headerStack.Children.Add(new TextBlock
            {
                Text = product.Name,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = $"Código: {product.Barcode}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#AAAAAA"))
            });
            header.Child = headerStack;

            Border? cacheBanner = null;
            if (isFromCache)
            {
                cacheBanner = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#E65100")),
                    Padding = new Thickness(16, 6),
                    Child = new TextBlock
                    {
                        Text = "⚠ Datos de caché local — sin conexión a la API",
                        FontSize = 12,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                };
            }

            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (items.Count == 0)
            {
                wrapPanel.Children.Add(new TextBlock
                {
                    Text = "Sin datos de existencias disponibles",
                    Foreground = new SolidColorBrush(Color.Parse("#888888")),
                    FontSize = 14,
                    Margin = new Thickness(20)
                });
            }

            foreach (var item in items.OrderBy(x => x.BranchName))
            {
                var hasStock = item.Quantity > 0;
                var cardBg = hasStock ? "#1B5E20" : "#3E2723";
                var cardBorder = hasStock ? "#2E7D32" : "#BF360C";
                var quantityColor = hasStock ? "#69F0AE" : "#FF6E40";

                var card = new Border
                {
                    Background = new SolidColorBrush(Color.Parse(cardBg)),
                    BorderBrush = new SolidColorBrush(Color.Parse(cardBorder)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20, 16),
                    Margin = new Thickness(8),
                    MinWidth = 160,
                    MaxWidth = 220
                };

                var cardStack = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
                cardStack.Children.Add(new TextBlock
                {
                    Text = item.BranchName,
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MaxWidth = 180
                });

                cardStack.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse(cardBorder)),
                    Margin = new Thickness(0, 4)
                });

                cardStack.Children.Add(new TextBlock
                {
                    Text = item.Quantity.ToString(),
                    FontSize = 42,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse(quantityColor)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                });

                cardStack.Children.Add(new TextBlock
                {
                    Text = item.Quantity == 1 ? "unidad" : "unidades",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                });

                card.Child = cardStack;
                wrapPanel.Children.Add(card);
            }

            var scrollViewer = new ScrollViewer
            {
                Content = wrapPanel,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Margin = new Thickness(10, 12)
            };

            var footer = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
                BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 10)
            };

            var closeBtn = new Button
            {
                Content = "Cerrar (Esc)",
                Background = new SolidColorBrush(Color.Parse("#424242")),
                Foreground = Brushes.White,
                Padding = new Thickness(20, 8),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeBtn.Click += (_, __) => Close();
            footer.Child = closeBtn;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            if (cacheBanner != null) mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            mainGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var row = 0;
            Grid.SetRow(header, row++);
            mainGrid.Children.Add(header);

            if (cacheBanner != null)
            {
                Grid.SetRow(cacheBanner, row++);
                mainGrid.Children.Add(cacheBanner);
            }

            Grid.SetRow(scrollViewer, row++);
            mainGrid.Children.Add(scrollViewer);

            Grid.SetRow(footer, row);
            mainGrid.Children.Add(footer);

            Content = mainGrid;

            KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };
        }
    }
}

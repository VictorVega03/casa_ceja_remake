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
        public StockByBranchDialog(
            Product product,
            List<ProductStockItem> items,
            bool isFromCache,
            List<Branch> allBranches)
        {
            Title = $"Existencias — {product.Name}";
            MinWidth = 560;
            Width = 720;
            MaxWidth = 960;
            MinHeight = 300;
            Height = 480;
            MaxHeight = 750;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = true;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"));

            // ── Header ───────────────────────────────────────────────────────
            var header = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
                BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 14)
            };

            var headerContent = new Grid();
            headerContent.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            headerContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var headerLeft = new StackPanel { Spacing = 2 };
            headerLeft.Children.Add(new TextBlock
            {
                Text = product.Name,
                FontSize = 17,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });
            headerLeft.Children.Add(new TextBlock
            {
                Text = $"Código: {product.Barcode}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#AAAAAA"))
            });
            headerContent.Children.Add(headerLeft);

            // Contadores en el header
            var stockedCount = items.Where(i => i.Quantity > 0).Count();
            var headerRight = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 2
            };
            headerRight.Children.Add(new TextBlock
            {
                Text = $"{items.Count(i => i.Quantity != 0)} sucursales con movimiento",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            Grid.SetColumn(headerRight, 1);
            headerContent.Children.Add(headerRight);

            header.Child = headerContent;

            // ── Cache banner ─────────────────────────────────────────────────
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

            // ── Construir mapa de stock por sucursal ─────────────────────────
            // stock conocido desde la respuesta de la API
            var stockMap = items.ToDictionary(i => i.BranchId, i => i.Quantity);

            // Combinar todas las sucursales conocidas con los datos de stock.
            // Si hay items de sucursales que no están en allBranches, los incluimos también.
            var allBranchItems = new List<(int BranchId, string BranchName, int Quantity, bool IsKnown)>();

            // 1. Todas las sucursales del catálogo
            var coveredIds = new HashSet<int>();
            foreach (var branch in allBranches.OrderBy(b => b.Name))
            {
                var qty = stockMap.TryGetValue(branch.Id, out var q) ? q : 0;
                allBranchItems.Add((branch.Id, branch.Name, qty, true));
                coveredIds.Add(branch.Id);
            }

            // 2. Sucursales en el stock que no están en el catálogo (edge case)
            foreach (var item in items.OrderBy(i => i.BranchName))
            {
                if (!coveredIds.Contains(item.BranchId))
                    allBranchItems.Add((item.BranchId, item.BranchName, item.Quantity, false));
            }

            // ── Layout de 2 columnas ─────────────────────────────────────────
            // Dividir items entre columna izquierda y derecha
            int totalItems = allBranchItems.Count;
            int leftCount = (totalItems + 1) / 2; // columna izquierda tiene la mitad superior (o más si impar)

            var leftColumn = BuildItemColumn(allBranchItems.Take(leftCount).ToList());
            var rightColumn = BuildItemColumn(allBranchItems.Skip(leftCount).ToList());

            var twoColumnGrid = new Grid { Margin = new Thickness(10, 8, 10, 8) };
            twoColumnGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            twoColumnGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(8, GridUnitType.Pixel)));
            twoColumnGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            Grid.SetColumn(leftColumn, 0);
            Grid.SetColumn(rightColumn, 2);
            twoColumnGrid.Children.Add(leftColumn);
            twoColumnGrid.Children.Add(rightColumn);

            if (totalItems == 0)
            {
                twoColumnGrid.Children.Clear();
                twoColumnGrid.ColumnDefinitions.Clear();
                twoColumnGrid.Children.Add(new TextBlock
                {
                    Text = "Sin sucursales disponibles",
                    Foreground = new SolidColorBrush(Color.Parse("#888888")),
                    FontSize = 14,
                    Margin = new Thickness(20)
                });
            }

            var scrollViewer = new ScrollViewer
            {
                Content = twoColumnGrid,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 4)
            };

            // ── Total global ──────────────────────────────────────────────────
            int totalQty = allBranchItems.Sum(x => x.Quantity);
            string totalColor = totalQty > 0 ? "#4CAF50" : totalQty < 0 ? "#EF5350" : "#9E9E9E";

            var totalBanner = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#252525")),
                BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 10)
            };

            var totalContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
            totalContent.Children.Add(new TextBlock
            {
                Text = "EXISTENCIA TOTAL:",
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
                VerticalAlignment = VerticalAlignment.Center
            });
            totalContent.Children.Add(new TextBlock
            {
                Text = totalQty.ToString(),
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse(totalColor)),
                VerticalAlignment = VerticalAlignment.Center
            });
            totalContent.Children.Add(new TextBlock
            {
                Text = "uds.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#555555")),
                VerticalAlignment = VerticalAlignment.Center
            });
            totalBanner.Child = totalContent;

            // ── Footer ────────────────────────────────────────────────────────
            var footer = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
                BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 10)
            };

            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            // Leyenda de colores
            var legend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, VerticalAlignment = VerticalAlignment.Center };

            void AddLegendItem(StackPanel p, string color, string label)
            {
                var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
                item.Children.Add(new Border
                {
                    Width = 10, Height = 10, CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(Color.Parse(color)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                item.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                p.Children.Add(item);
            }

            AddLegendItem(legend, "#4CAF50", "Con existencia");
            AddLegendItem(legend, "#616161", "Sin existencia");
            AddLegendItem(legend, "#EF5350", "Negativo");
            footerGrid.Children.Add(legend);

            var closeBtn = new Button
            {
                Content = "Cerrar (Esc)",
                Background = new SolidColorBrush(Color.Parse("#424242")),
                Foreground = Brushes.White,
                Padding = new Thickness(20, 8),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            closeBtn.Click += (_, __) => Close();
            Grid.SetColumn(closeBtn, 1);
            footerGrid.Children.Add(closeBtn);

            footer.Child = footerGrid;

            // ── Layout principal ──────────────────────────────────────────────
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            if (cacheBanner != null) mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            mainGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
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

            Grid.SetRow(totalBanner, row++);
            mainGrid.Children.Add(totalBanner);

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

        private static StackPanel BuildItemColumn(List<(int BranchId, string BranchName, int Quantity, bool IsKnown)> items)
        {
            var column = new StackPanel { Spacing = 0 };

            for (int i = 0; i < items.Count; i++)
            {
                var (_, branchName, qty, _) = items[i];
                bool isFirst = i == 0;
                bool isLast = i == items.Count - 1;

                string quantityColor = qty > 0 ? "#4CAF50" : qty < 0 ? "#EF5350" : "#616161";

                var itemBorder = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#252525")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#2E2E2E")),
                    BorderThickness = isFirst ? new Thickness(1) : new Thickness(1, 0, 1, 1),
                    CornerRadius = (isFirst, isLast) switch
                    {
                        (true, true)  => new CornerRadius(8),
                        (true, false) => new CornerRadius(8, 8, 0, 0),
                        (false, true) => new CornerRadius(0, 0, 8, 8),
                        _             => new CornerRadius(0)
                    },
                    Padding = new Thickness(14, 13)
                };

                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                itemGrid.Children.Add(new TextBlock
                {
                    Text = branchName,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.NoWrap
                });

                var qtyStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    VerticalAlignment = VerticalAlignment.Center
                };
                qtyStack.Children.Add(new TextBlock
                {
                    Text = qty.ToString(),
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse(quantityColor)),
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 28,
                    TextAlignment = TextAlignment.Right
                });
                qtyStack.Children.Add(new TextBlock
                {
                    Text = "uds.",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#555555")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(qtyStack, 1);
                itemGrid.Children.Add(qtyStack);

                itemBorder.Child = itemGrid;
                column.Children.Add(itemBorder);
            }

            return column;
        }
    }
}

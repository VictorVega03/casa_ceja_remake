using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;
using System.Threading.Tasks;
using CasaCejaRemake.Models.Results;
using CasaCejaRemake.Services;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using CasaCejaRemake.Views.Shared;

namespace casa_ceja_remake.Helpers;

public static class DialogHelper
{
    public static async Task ShowMessageDialog(Window parentWindow, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Colors.DimGray)
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(Colors.White)
        });

        var okButton = new Button
        {
            Content = "Aceptar (Enter)",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Avalonia.Thickness(20, 5)
        };
        okButton.Click += (s, e) => dialog.Close();
        panel.Children.Add(okButton);

        // Shortcuts: Enter y Esc para cerrar
        dialog.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter || e.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };

        dialog.Content = panel;
        await dialog.ShowDialog(parentWindow);
    }

    public static async Task ShowTicketDialog(Window parentWindow, string folio, string ticketText)
    {
        // ── Construir el diálogo ──────────────────────────────────────────────
        var dialog = new Window
        {
            Title = $"Ticket - Folio: {folio}",
            Width = 450,
            Height = 580,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            Topmost = true,
            ShowInTaskbar = false
        };

        var mainPanel = new DockPanel { LastChildFill = true };

        // ── Banner de estado de impresión (anclado arriba) ───────────────────
        // Muestra el resultado de impresión automática y del botón Reimprimir.
        var printStatusBar = new Border
        {
            Height = 36,
            Background = new SolidColorBrush(Color.Parse("#1565C0")), // azul neutro inicial
            Padding = new Avalonia.Thickness(12, 0)
        };
        var printStatusText = new TextBlock
        {
            Text = "⏳ Enviando a impresora...",
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        printStatusBar.Child = printStatusText;
        DockPanel.SetDock(printStatusBar, Dock.Top);
        mainPanel.Children.Add(printStatusBar);

        // ── Panel de botones (anclado abajo) ─────────────────────────────────
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(10),
            Spacing = 10
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        var printButton = new Button
        {
            Content = "🖨️ Reimprimir (Ctrl+P)",
            Padding = new Avalonia.Thickness(20, 10),
            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            CornerRadius = new Avalonia.CornerRadius(6),
            Focusable = false
        };

        var closeButton = new Button
        {
            Content = "Cerrar (Esc)",
            Padding = new Avalonia.Thickness(20, 10),
            Background = new SolidColorBrush(Color.Parse("#757575")),
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 14,
            CornerRadius = new Avalonia.CornerRadius(6),
            Focusable = false
        };

        closeButton.Click += (s, e) => dialog.Close();
        buttonPanel.Children.Add(printButton);
        buttonPanel.Children.Add(closeButton);
        mainPanel.Children.Add(buttonPanel);

        // ── Contenido del ticket (área central) ──────────────────────────────
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Colors.White),
            Margin = new Avalonia.Thickness(10, 6, 10, 6)
        };
        scrollViewer.Content = new TextBlock
        {
            Text = ticketText,
            FontFamily = new FontFamily("Courier New"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.Black),
            Margin = new Avalonia.Thickness(10),
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap
        };
        mainPanel.Children.Add(scrollViewer);

        dialog.Content = mainPanel;

        // ── Helper: actualizar el banner según PrintResult ────────────────────
        void UpdateStatusBar(PrintResult result, bool isAuto)
        {
            if (result.Success)
            {
                printStatusBar.Background = new SolidColorBrush(Color.Parse("#2E7D32")); // verde
                printStatusText.Text = isAuto
                    ? "✓ Ticket enviado a la impresora automáticamente"
                    : "✓ Ticket enviado a la impresora";
            }
            else
            {
                var (bg, icon) = result.FailReason switch
                {
                    PrintFailReason.NoPrinterConfigured =>
                        ("#B71C1C", " Sin impresora configurada — ve a Configuración → Impresora"),
                    PrintFailReason.AutoPrintDisabled =>
                        ("#455A64", "ℹImpresión automática desactivada — usa Reimprimir si necesitas"),
                    PrintFailReason.DriverError =>
                        ("#E65100", "✗ Error de impresora — verifica que esté encendida y conectada"),
                    _ =>
                        ("#B71C1C", $"✗ {result.ErrorMessage ?? "Error desconocido"}")
                };
                printStatusBar.Background = new SolidColorBrush(Color.Parse(bg));
                printStatusText.Text = icon;
            }
        }

        // ── Botón Reimprimir ─────────────────────────────────────────────────
        printButton.Click += async (s, e) =>
        {
            printButton.IsEnabled = false;
            printStatusBar.Background = new SolidColorBrush(Color.Parse("#1565C0"));
            printStatusText.Text = "⏳ Enviando a impresora...";

            try
            {
                var app = (CasaCejaRemake.App)Avalonia.Application.Current!;
                var printService = app.GetPrintService();

                if (printService == null)
                {
                    printStatusBar.Background = new SolidColorBrush(Color.Parse("#B71C1C"));
                    printStatusText.Text = "✗ Servicio de impresión no disponible";
                }
                else
                {
                    var result = await printService.PrintAsync(ticketText);
                    UpdateStatusBar(result, isAuto: false);
                    Console.WriteLine($"[DialogHelper] Reimprimir: {(result.Success ? "OK" : result.ErrorMessage)}");
                }
            }
            catch (Exception ex)
            {
                printStatusBar.Background = new SolidColorBrush(Color.Parse("#B71C1C"));
                printStatusText.Text = $"✗ Error inesperado: {ex.Message}";
                Console.WriteLine($"[DialogHelper] Excepción al reimprimir: {ex.Message}");
            }
            finally
            {
                printButton.IsEnabled = true;
            }
        };

        // ── Shortcut Esc ─────────────────────────────────────────────────────
        dialog.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };

        // ── Mostrar el diálogo ────────────────────────────────────────────────
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        dialog.Closed += (s, e) => tcs.TrySetResult(true);
        dialog.Show(parentWindow);
        dialog.Activate();
        dialog.Topmost = true;

        // ── Impresión automática (después de mostrar el diálogo) ─────────────
        try
        {
            var app = (CasaCejaRemake.App)Avalonia.Application.Current!;
            var configService = app.GetConfigService();
            var printService  = app.GetPrintService();

            if (configService == null || printService == null)
            {
                printStatusBar.Background = new SolidColorBrush(Color.Parse("#B71C1C"));
                printStatusText.Text = "✗ Servicio de impresión no disponible";
            }
            else if (!configService.PosTerminalConfig.AutoPrint)
            {
                // AutoPrint desactivado — el usuario puede usar Reimprimir si quiere
                printStatusBar.Background = new SolidColorBrush(Color.Parse("#455A64"));
                printStatusText.Text = "Impresión automática desactivada";
                Console.WriteLine("[DialogHelper] AutoPrint desactivado");
            }
            else
            {
                Console.WriteLine("[DialogHelper] Imprimiendo automáticamente...");
                var result = await printService.PrintAsync(ticketText);
                UpdateStatusBar(result, isAuto: true);
                Console.WriteLine($"[DialogHelper] Auto-print: {(result.Success ? "OK" : result.ErrorMessage)}");
            }
        }
        catch (Exception ex)
        {
            printStatusBar.Background = new SolidColorBrush(Color.Parse("#B71C1C"));
            printStatusText.Text = $"✗ Error inesperado: {ex.Message}";
            Console.WriteLine($"[DialogHelper] Excepción en impresión automática: {ex.Message}");
        }

        await tcs.Task;
    }

    public static async Task<string?> ShowInputDialog(Window parentWindow, string title, string prompt, string defaultValue = "")
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#232323"))
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(24, 20, 24, 20), Spacing = 14 };

        panel.Children.Add(new TextBlock
        {
            Text = prompt,
            FontSize = 14,
            Foreground = new SolidColorBrush(Colors.White),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var textBox = new TextBox
        {
            Text = defaultValue,
            Background = new SolidColorBrush(Color.Parse("#2E2E2E")),
            Foreground = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Color.Parse("#555")),
            CaretBrush = new SolidColorBrush(Colors.White),
            Padding = new Avalonia.Thickness(10, 7),
            FontSize = 14,
            CornerRadius = new Avalonia.CornerRadius(4)
        };
        panel.Children.Add(textBox);

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var cancelButton = new Button
        {
            Content = "Cancelar (Esc)",
            Padding = new Avalonia.Thickness(16, 7),
            Background = new SolidColorBrush(Color.Parse("#3A3A3A")),
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new Avalonia.CornerRadius(4)
        };
        cancelButton.Click += (s, e) => { dialog.Tag = null; dialog.Close(); };

        var okButton = new Button
        {
            Content = "Guardar (Enter)",
            Padding = new Avalonia.Thickness(16, 7),
            Background = new SolidColorBrush(Color.Parse("#2E7D32")),
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new Avalonia.CornerRadius(4)
        };
        okButton.Click += (s, e) => { dialog.Tag = textBox.Text; dialog.Close(); };

        buttonsPanel.Children.Add(cancelButton);
        buttonsPanel.Children.Add(okButton);
        panel.Children.Add(buttonsPanel);

        dialog.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) { dialog.Tag = textBox.Text; dialog.Close(); e.Handled = true; }
            else if (e.Key == Avalonia.Input.Key.Escape) { dialog.Tag = null; dialog.Close(); e.Handled = true; }
        };

        dialog.Content = panel;
        dialog.Opened += (s, e) => { textBox.Focus(); textBox.SelectAll(); };

        await dialog.ShowDialog(parentWindow);
        return dialog.Tag as string;
    }

    public static async Task<bool> ShowConfirmDialog(Window parentWindow, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Colors.DimGray)
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(Colors.White)
        });

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 10
        };

        var yesButton = new Button
        {
            Content = "Sí (Enter)",
            Padding = new Avalonia.Thickness(20, 5),
            Width = 120
        };
        yesButton.Click += (s, e) =>
        {
            dialog.Tag = true;
            dialog.Close();
        };

        var noButton = new Button
        {
            Content = "No (Esc)",
            Padding = new Avalonia.Thickness(20, 5),
            Width = 120
        };
        noButton.Click += (s, e) =>
        {
            dialog.Tag = false;
            dialog.Close();
        };

        buttonsPanel.Children.Add(noButton);
        buttonsPanel.Children.Add(yesButton);
        panel.Children.Add(buttonsPanel);

        // Shortcuts: Enter para Sí, Esc para No
        dialog.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                dialog.Tag = true;
                dialog.Close();
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Tag = false;
                dialog.Close();
                e.Handled = true;
            }
        };

        dialog.Content = panel;
        await dialog.ShowDialog(parentWindow);

        return dialog.Tag is bool result && result;
    }

    /// <summary>
    /// Acción a tomar cuando se detecta un archivo duplicado.
    /// </summary>
    public enum DuplicateFileAction
    {
        Replace,
        Duplicate,
        Cancel
    }

    /// <summary>
    /// Muestra un diálogo de archivo duplicado con 3 opciones:
    /// Reemplazar, Duplicar o Cancelar.
    /// </summary>
    public static async Task<DuplicateFileAction> ShowDuplicateFileDialog(
        Window parentWindow, string fileName)
    {
        var dialog = new Window
        {
            Title = "Archivo existente",
            Width = 420,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Colors.DimGray),
            Topmost = true,
            ShowInTaskbar = false,
            Focusable = true
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15
        };

        panel.Children.Add(new TextBlock
        {
            Text = $"Ya existe un archivo con el nombre:\n\"{fileName}\"\n\n¿Qué desea hacer?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 13,
            Foreground = new SolidColorBrush(Colors.White)
        });

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8
        };

        var replaceButton = new Button
        {
            Content = "Reemplazar (R)",
            Padding = new Avalonia.Thickness(15, 5),
            Background = new SolidColorBrush(Color.Parse("#FF9800")),
            Foreground = new SolidColorBrush(Colors.White)
        };
        replaceButton.Click += (s, e) =>
        {
            dialog.Tag = DuplicateFileAction.Replace;
            dialog.Close();
        };

        var duplicateButton = new Button
        {
            Content = "Duplicar (D)",
            Padding = new Avalonia.Thickness(15, 5),
            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
            Foreground = new SolidColorBrush(Colors.White)
        };
        duplicateButton.Click += (s, e) =>
        {
            dialog.Tag = DuplicateFileAction.Duplicate;
            dialog.Close();
        };

        var cancelButton = new Button
        {
            Content = "Cancelar (Esc)",
            Padding = new Avalonia.Thickness(15, 5),
            Background = new SolidColorBrush(Color.Parse("#555555")),
            Foreground = new SolidColorBrush(Colors.White)
        };
        cancelButton.Click += (s, e) =>
        {
            dialog.Tag = DuplicateFileAction.Cancel;
            dialog.Close();
        };

        buttonsPanel.Children.Add(replaceButton);
        buttonsPanel.Children.Add(duplicateButton);
        buttonsPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonsPanel);

        // Shortcuts: R=Reemplazar, D=Duplicar, Esc=Cancelar
        dialog.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.R)
            {
                dialog.Tag = DuplicateFileAction.Replace;
                dialog.Close();
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.D)
            {
                dialog.Tag = DuplicateFileAction.Duplicate;
                dialog.Close();
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Tag = DuplicateFileAction.Cancel;
                dialog.Close();
                e.Handled = true;
            }
        };

        dialog.Content = panel;
        dialog.Tag = DuplicateFileAction.Cancel; // Default
        await dialog.ShowDialog(parentWindow);

        return dialog.Tag is DuplicateFileAction action ? action : DuplicateFileAction.Cancel;
    }

    public static async Task ShowStockDialog(Window parent, Product product, List<ProductStockItem> items, bool isFromCache, List<Branch> allBranches)
    {
        var dialog = new StockByBranchDialog(product, items, isFromCache, allBranches);
        await dialog.ShowDialog(parent);
    }

    public static async Task<bool> ShowEntryConfirmDialog(
        Window parent, string branchName, string supplierName, int productCount, decimal total)
    {
        var dialog = new Window
        {
            Title = "Confirmar Entrada",
            Width = 440,
            Height = 340,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        mainGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        // Header
        var header = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 14)
        };
        var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        headerStack.Children.Add(new Border
        {
            Width = 4, Height = 22, CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.Parse("#FF9800")),
            VerticalAlignment = VerticalAlignment.Center
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = "Confirmar Entrada de Mercancía",
            FontSize = 15, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
        });
        header.Child = headerStack;
        Grid.SetRow(header, 0);
        mainGrid.Children.Add(header);

        // Body
        var body = new StackPanel { Margin = new Thickness(24, 20, 24, 0), Spacing = 0 };

        body.Children.Add(new TextBlock
        {
            Text = "Se dará de alta la siguiente mercancía:",
            FontSize = 13, Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
            Margin = new Thickness(0, 0, 0, 16)
        });

        void AddRow(StackPanel container, string label, string value, string valueColor = "#E0E0E0", double valueFontSize = 14)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.Margin = new Thickness(0, 0, 0, 10);

            row.Children.Add(new TextBlock
            {
                Text = label, FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#999999")),
                VerticalAlignment = VerticalAlignment.Center
            });
            var valBlock = new TextBlock
            {
                Text = value, FontSize = valueFontSize, FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse(valueColor)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valBlock, 1);
            row.Children.Add(valBlock);
            container.Children.Add(row);
        }

        AddRow(body, "Sucursal", branchName, "#FF9800");
        AddRow(body, "Proveedor", supplierName);
        AddRow(body, "Productos", $"{productCount} {(productCount == 1 ? "artículo" : "artículos")}");

        body.Children.Add(new Border
        {
            Height = 1, Background = new SolidColorBrush(Color.Parse("#2E2E2E")),
            Margin = new Thickness(0, 4, 0, 12)
        });

        AddRow(body, "Total de la entrada", total.ToString("C"), "#4CAF50", 17);

        Grid.SetRow(body, 1);
        mainGrid.Children.Add(body);

        // Footer
        var footer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252525")),
            BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(20, 14)
        };
        var footerBtns = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var cancelBtn = new Button
        {
            Content = "Cancelar (Esc)",
            Padding = new Thickness(18, 9),
            Background = new SolidColorBrush(Color.Parse("#3A3A3A")),
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(5),
            FontSize = 13, Cursor = new Cursor(StandardCursorType.Hand)
        };
        cancelBtn.Click += (_, __) => { dialog.Tag = false; dialog.Close(); };

        var confirmBtn = new Button
        {
            Content = "Confirmar (Enter)",
            Padding = new Thickness(18, 9),
            Background = new SolidColorBrush(Color.Parse("#2E7D32")),
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(5),
            FontSize = 13, FontWeight = FontWeight.SemiBold,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        confirmBtn.Click += (_, __) => { dialog.Tag = true; dialog.Close(); };

        footerBtns.Children.Add(cancelBtn);
        footerBtns.Children.Add(confirmBtn);
        footer.Child = footerBtns;
        Grid.SetRow(footer, 2);
        mainGrid.Children.Add(footer);

        dialog.Content = mainGrid;
        dialog.Tag = false;

        dialog.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) { dialog.Tag = true; dialog.Close(); e.Handled = true; }
            else if (e.Key == Avalonia.Input.Key.Escape) { dialog.Tag = false; dialog.Close(); e.Handled = true; }
        };

        await dialog.ShowDialog(parent);
        return dialog.Tag is true;
    }

    public static async Task<bool> ShowOutputConfirmDialog(
        Window parent, string destinationName, int productCount, decimal total)
    {
        var dialog = new Window
        {
            Title = "Confirmar Salida",
            Width = 440,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        mainGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        // Header
        var header = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 14)
        };
        var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        headerStack.Children.Add(new Border
        {
            Width = 4, Height = 22, CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.Parse("#42A5F5")),
            VerticalAlignment = VerticalAlignment.Center
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = "Confirmar Salida de Mercancía",
            FontSize = 15, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
        });
        header.Child = headerStack;
        Grid.SetRow(header, 0);
        mainGrid.Children.Add(header);

        // Body
        var body = new StackPanel { Margin = new Thickness(24, 20, 24, 0), Spacing = 0 };

        body.Children.Add(new TextBlock
        {
            Text = "Se registrará la salida con los siguientes datos:",
            FontSize = 13, Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
            Margin = new Thickness(0, 0, 0, 16)
        });

        void AddRow(StackPanel container, string label, string value, string valueColor = "#E0E0E0", double valueFontSize = 14)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.Margin = new Thickness(0, 0, 0, 10);

            row.Children.Add(new TextBlock
            {
                Text = label, FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#999999")),
                VerticalAlignment = VerticalAlignment.Center
            });
            var valBlock = new TextBlock
            {
                Text = value, FontSize = valueFontSize, FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse(valueColor)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valBlock, 1);
            row.Children.Add(valBlock);
            container.Children.Add(row);
        }

        AddRow(body, "Sucursal destino", destinationName, "#42A5F5");
        AddRow(body, "Productos", $"{productCount} {(productCount == 1 ? "artículo" : "artículos")}");

        body.Children.Add(new Border
        {
            Height = 1, Background = new SolidColorBrush(Color.Parse("#2E2E2E")),
            Margin = new Thickness(0, 4, 0, 12)
        });

        AddRow(body, "Total de la salida", total.ToString("C"), "#4CAF50", 17);

        body.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1A1200")),
            BorderBrush = new SolidColorBrush(Color.Parse("#4A3800")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4, 0, 0),
            Child = new TextBlock
            {
                Text = "⚠ La sucursal destino deberá confirmar la recepción para que su inventario se actualice.",
                FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#FFC107")),
                TextWrapping = TextWrapping.Wrap
            }
        });

        Grid.SetRow(body, 1);
        mainGrid.Children.Add(body);

        // Footer
        var footer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252525")),
            BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3A")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(20, 14)
        };
        var footerBtns = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var cancelBtn = new Button
        {
            Content = "Cancelar (Esc)",
            Padding = new Thickness(18, 9),
            Background = new SolidColorBrush(Color.Parse("#3A3A3A")),
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(5),
            FontSize = 13, Cursor = new Cursor(StandardCursorType.Hand)
        };
        cancelBtn.Click += (_, __) => { dialog.Tag = false; dialog.Close(); };

        var confirmBtn = new Button
        {
            Content = "Confirmar (Enter)",
            Padding = new Thickness(18, 9),
            Background = new SolidColorBrush(Color.Parse("#1565C0")),
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(5),
            FontSize = 13, FontWeight = FontWeight.SemiBold,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        confirmBtn.Click += (_, __) => { dialog.Tag = true; dialog.Close(); };

        footerBtns.Children.Add(cancelBtn);
        footerBtns.Children.Add(confirmBtn);
        footer.Child = footerBtns;
        Grid.SetRow(footer, 2);
        mainGrid.Children.Add(footer);

        dialog.Content = mainGrid;
        dialog.Tag = false;

        dialog.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) { dialog.Tag = true; dialog.Close(); e.Handled = true; }
            else if (e.Key == Avalonia.Input.Key.Escape) { dialog.Tag = false; dialog.Close(); e.Handled = true; }
        };

        await dialog.ShowDialog(parent);
        return dialog.Tag is true;
    }
}

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using CasaCejaRemake.Services;

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
        void UpdateStatusBar(CasaCejaRemake.Services.PrintResult result, bool isAuto)
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
                    CasaCejaRemake.Services.PrintFailReason.NoPrinterConfigured =>
                        ("#B71C1C", " Sin impresora configurada — ve a Configuración → Impresora"),
                    CasaCejaRemake.Services.PrintFailReason.AutoPrintDisabled =>
                        ("#455A64", "ℹImpresión automática desactivada — usa Reimprimir si necesitas"),
                    CasaCejaRemake.Services.PrintFailReason.DriverError =>
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
}

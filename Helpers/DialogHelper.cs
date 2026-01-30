using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

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
        var dialog = new Window
        {
            Title = $"Ticket - Folio: {folio}",
            Width = 400,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Background = new SolidColorBrush(Colors.White),
            Topmost = true,
            ShowInTaskbar = false
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var textBlock = new TextBlock
        {
            Text = ticketText,
            FontFamily = new FontFamily("Courier New"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.Black),
            Margin = new Avalonia.Thickness(10)
        };

        scrollViewer.Content = textBlock;
        dialog.Content = scrollViewer;

        // Shortcut: Esc para cerrar  
        dialog.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };

        // TaskCompletionSource para esperar que se cierre la ventana
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        dialog.Closed += (s, e) => tcs.TrySetResult(true);

        // Usar Show() en lugar de ShowDialog() para permitir Topmost real
        dialog.Show(parentWindow);
        
        // Forzar activación y traer al frente
        dialog.Activate();
        dialog.Topmost = true; // Forzar de nuevo después de Show
        
        // Esperar a que se cierre la ventana
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
}

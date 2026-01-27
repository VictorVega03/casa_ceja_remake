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
            Content = "Aceptar",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Avalonia.Thickness(20, 5)
        };
        okButton.Click += (s, e) => dialog.Close();
        panel.Children.Add(okButton);

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
            Background = new SolidColorBrush(Colors.White)
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

        await dialog.ShowDialog(parentWindow);
    }
}

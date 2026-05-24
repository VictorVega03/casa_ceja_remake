using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CasaCejaRemake.Services;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.Helpers
{
    public static class AdminOperationHelper
    {
        public static async Task<bool> ExecuteAsync(
            Window parentWindow,
            ApiClient apiClient,
            Func<Task<(bool Success, string Message)>> operation,
            string successMessage,
            Action? onBusy = null,
            Action? onIdle = null)
        {
            if (parentWindow == null) throw new ArgumentNullException(nameof(parentWindow));
            if (apiClient == null) throw new ArgumentNullException(nameof(apiClient));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            onBusy?.Invoke();

            try
            {
                if (!await apiClient.IsServerAvailableAsync())
                {
                    await ShowResultDialogAsync(parentWindow, "Error", "Sin conexión al servidor.", "#B71C1C", "✗");
                    return false;
                }

                var result = await operation();
                if (!result.Success)
                {
                    var message = string.IsNullOrWhiteSpace(result.Message)
                        ? "No se pudo completar la operación."
                        : result.Message;

                    await ShowResultDialogAsync(parentWindow, "Error", message, "#B71C1C", "✗");
                    return false;
                }

                await ShowResultDialogAsync(parentWindow, "Éxito", successMessage, "#1B5E20", "✓");
                return true;
            }
            catch (Exception ex)
            {
                await ShowResultDialogAsync(parentWindow, "Error", $"Error inesperado: {ex.Message}", "#B71C1C", "✗");
                return false;
            }
            finally
            {
                onIdle?.Invoke();
            }
        }

        private static async Task ShowResultDialogAsync(Window parentWindow, string title, string message, string backgroundColor, string icon)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 360,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
                Background = new SolidColorBrush(Color.Parse(backgroundColor)),
            };

            var root = new Border
            {
                Background = new SolidColorBrush(Color.Parse(backgroundColor)),
                Padding = new Thickness(18),
                CornerRadius = new CornerRadius(10)
            };

            var content = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                ColumnDefinitions = new ColumnDefinitions("Auto,*")
            };

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 30,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };
            Grid.SetRowSpan(iconText, 2);
            content.Children.Add(iconText);

            var messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetColumn(messageText, 1);
            Grid.SetRow(messageText, 0);
            Grid.SetRowSpan(messageText, 2);
            content.Children.Add(messageText);

            var button = new Button
            {
                Content = "Aceptar",
                Width = 92,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Colors.White),
                Foreground = new SolidColorBrush(Color.Parse(backgroundColor)),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(14, 6)
            };
            button.Click += (_, _) => dialog.Close();
            Grid.SetColumn(button, 1);
            Grid.SetRow(button, 2);
            content.Children.Add(button);

            root.Child = content;
            dialog.Content = root;

            dialog.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    dialog.Close();
                    e.Handled = true;
                }
            };

            await dialog.ShowDialog(parentWindow);
        }
    }
}
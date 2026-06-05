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

        public static async Task ShowResultDialogAsync(Window parentWindow, string title, string message, string accentColor, string icon)
        {
            var accent = Color.Parse(accentColor);

            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 210,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
                Background = new SolidColorBrush(Color.Parse("#1A1A1A")),
            };

            // Card border with accent outline
            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#242424")),
                BorderBrush = new SolidColorBrush(accent),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24),
                Margin = new Thickness(16)
            };

            var layout = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };

            // Header row: circle icon + title
            var headerRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*")
            };

            var iconCircle = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconCircle.Child = new TextBlock
            {
                Text = icon,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconCircle, 0);
            headerRow.Children.Add(iconCircle);

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };
            Grid.SetColumn(titleText, 1);
            headerRow.Children.Add(titleText);

            Grid.SetRow(headerRow, 0);
            layout.Children.Add(headerRow);

            // Message
            var messageText = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 14, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(messageText, 1);
            layout.Children.Add(messageText);

            // Accept button
            var button = new Button
            {
                Content = "Aceptar",
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(accent),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(20, 8),
                Margin = new Thickness(0, 14, 0, 0)
            };
            button.Click += (_, _) => dialog.Close();
            Grid.SetRow(button, 2);
            layout.Children.Add(button);

            card.Child = layout;
            dialog.Content = card;

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
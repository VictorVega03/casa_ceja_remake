using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace casa_ceja_remake.Helpers;

/// <summary>
/// Helper para verificar el PIN de administrador.
/// </summary>
public static class AdminPinVerificationHelper
{
    /// <summary>
    /// Muestra un diálogo solicitando el PIN de administrador.
    /// Retorna true si el PIN es correcto o no hay PIN configurado.
    /// </summary>
    public static async Task<bool> VerifyPinAsync(Window parentWindow, AppConfig config)
    {
        if (string.IsNullOrEmpty(config.AdminModulePin))
            return true; // No hay PIN configurado

        var dialog = new Window
        {
            Title = "Verificación de Autorización",
            Width = 350,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Topmost = true,
            ShowInTaskbar = false
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(30),
            Spacing = 15
        };

        // Título
        var titleText = new TextBlock
        {
            Text = "🔒 Autorización Requerida",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#FFC107")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

        var descText = new TextBlock
        {
            Text = "Ingrese el PIN de autorización:",
            FontSize = 13,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        // Campo de contraseña
        var txtPassword = new TextBox
        {
            Watermark = "PIN",
            PasswordChar = '•',
            Height = 35,
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#404040")),
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(10, 8),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        // Mensaje de error
        var errorText = new TextBlock
        {
            Text = "",
            Foreground = new SolidColorBrush(Color.Parse("#FF6B6B")),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsVisible = false
        };

        // Botones
        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };

        var btnVerify = new Button
        {
            Content = "Verificar",
            Width = 120,
            Height = 35,
            Background = new SolidColorBrush(Color.Parse("#2E7D32")),
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        var btnCancel = new Button
        {
            Content = "Cancelar",
            Width = 120,
            Height = 35,
            Background = new SolidColorBrush(Color.Parse("#757575")),
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        bool? result = null;

        void VerifyCredentials()
        {
            var password = txtPassword.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(password))
            {
                errorText.Text = "⚠️ Ingrese el PIN";
                errorText.IsVisible = true;
                return;
            }

            if (password == config.AdminModulePin)
            {
                result = true;
                dialog.Close();
            }
            else
            {
                errorText.Text = "❌ PIN incorrecto";
                errorText.IsVisible = true;
                txtPassword.Text = "";
                txtPassword.Focus();
            }
        }

        btnVerify.Click += (s, e) => VerifyCredentials();
        btnCancel.Click += (s, e) =>
        {
            result = false;
            dialog.Close();
        };

        // Enter para verificar, Esc para cancelar
        dialog.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                VerifyCredentials();
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                result = false;
                dialog.Close();
                e.Handled = true;
            }
        };

        buttonsPanel.Children.Add(btnCancel);
        buttonsPanel.Children.Add(btnVerify);

        panel.Children.Add(titleText);
        panel.Children.Add(descText);
        panel.Children.Add(txtPassword);
        panel.Children.Add(errorText);
        panel.Children.Add(buttonsPanel);

        dialog.Content = panel;

        // Focus inicial
        dialog.Opened += (s, e) => txtPassword.Focus();

        await dialog.ShowDialog(parentWindow);

        return result == true;
    }
}

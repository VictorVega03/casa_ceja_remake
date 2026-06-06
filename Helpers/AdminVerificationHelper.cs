using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using CasaCejaRemake.Services;

namespace casa_ceja_remake.Helpers;

/// <summary>
/// Helper para verificar credenciales de administrador sin cambiar la sesión actual.
/// </summary>
public static class AdminVerificationHelper
{
    /// <summary>
    /// Muestra un diálogo de verificación de administrador.
    /// Retorna true si las credenciales son correctas y el usuario es admin.
    /// No cambia la sesión actual del sistema.
    /// </summary>
    public static async Task<bool> VerifyAdminAsync(Window parentWindow, UserService userService)
    {
        var dialog = new Window
        {
            Title = "Verificación de Administrador",
            Width = 400,
            Height = 380,
            MinWidth = 340,
            MinHeight = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Topmost = true,
            ShowInTaskbar = false
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 15
        };

        // Título
        var titleText = new TextBlock
        {
            Text = "🔒 Autorización Requerida",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#FFC107")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

        var descText = new TextBlock
        {
            Text = "Ingrese credenciales de administrador para continuar:",
            FontSize = 13,
            Foreground = Brushes.White,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        // Campo de usuario
        var userLabel = new TextBlock
        {
            Text = "USUARIO",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#B0B0B0")),
            FontWeight = FontWeight.SemiBold
        };

        var txtUsername = new TextBox
        {
            Watermark = "Nombre de usuario",
            Height = 35,
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#404040")),
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(10, 8)
        };

        // Campo de contraseña
        var passLabel = new TextBlock
        {
            Text = "CONTRASEÑA",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#B0B0B0")),
            FontWeight = FontWeight.SemiBold
        };

        var txtPassword = new TextBox
        {
            Watermark = "Contraseña",
            PasswordChar = '•',
            Height = 35,
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#404040")),
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(10, 8)
        };

        // Mensaje de error
        var errorText = new TextBlock
        {
            Text = "",
            Foreground = new SolidColorBrush(Color.Parse("#FF6B6B")),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false
        };

        // Botones
        var buttonsPanel = new Grid
        {
            ColumnSpacing = 10,
            Margin = new Avalonia.Thickness(20, 14),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        buttonsPanel.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        buttonsPanel.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var btnVerify = new Button
        {
            Content = "Verificar (Enter)",
            Height = 35,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Color.Parse("#2E7D32")),
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        var btnCancel = new Button
        {
            Content = "Cancelar (Esc)",
            Height = 35,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Color.Parse("#757575")),
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        bool? result = null;

        async Task VerifyCredentials()
        {
            var username = txtUsername.Text?.Trim() ?? "";
            var password = txtPassword.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                errorText.Text = "⚠️ Complete todos los campos";
                errorText.IsVisible = true;
                return;
            }

            var authResult = await userService.AuthenticateAsync(username, password);

            if (authResult.Success && authResult.User != null)
            {
                // Verificar que sea administrador
                var isAdmin = await userService.IsAdminAsync(authResult.User.Id);
                
                if (isAdmin)
                {
                    result = true;
                    dialog.Close();
                }
                else
                {
                    errorText.Text = "❌ Solo administradores pueden realizar esta acción";
                    errorText.IsVisible = true;
                    txtPassword.Text = "";
                    txtPassword.Focus();
                }
            }
            else
            {
                errorText.Text = "❌ Usuario o contraseña incorrectos";
                errorText.IsVisible = true;
                txtPassword.Text = "";
                txtPassword.Focus();
            }
        }

        btnVerify.Click += async (s, e) => await VerifyCredentials();
        btnCancel.Click += (s, e) =>
        {
            result = false;
            dialog.Close();
        };

        // Enter para verificar, Esc para cancelar
        dialog.KeyDown += async (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                await VerifyCredentials();
                e.Handled = true;
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                result = false;
                dialog.Close();
                e.Handled = true;
            }
        };

        Grid.SetColumn(btnCancel, 0);
        Grid.SetColumn(btnVerify, 1);
        buttonsPanel.Children.Add(btnCancel);
        buttonsPanel.Children.Add(btnVerify);

        panel.Children.Add(titleText);
        panel.Children.Add(descText);
        panel.Children.Add(userLabel);
        panel.Children.Add(txtUsername);
        panel.Children.Add(passLabel);
        panel.Children.Add(txtPassword);
        panel.Children.Add(errorText);

        var scrollViewer = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetRow(scrollViewer, 0);
        Grid.SetRow(buttonsPanel, 1);
        root.Children.Add(scrollViewer);
        root.Children.Add(buttonsPanel);

        dialog.Content = root;

        // Focus inicial en el campo de usuario
        dialog.Opened += (s, e) =>
        {
            var screen = dialog.Screens.ScreenFromWindow(dialog) ?? dialog.Screens.Primary;
            if (screen != null)
            {
                var area = screen.WorkingArea;
                dialog.Width = Math.Max(340, Math.Min(440, area.Width * 0.9));
                dialog.Height = Math.Max(300, Math.Min(400, area.Height * 0.75));
            }

            txtUsername.Focus();
        };

        await dialog.ShowDialog(parentWindow);

        return result == true;
    }
}

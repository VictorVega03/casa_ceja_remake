using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace CasaCejaRemake.Views.Shared;

public enum AppDialogTone
{
    Information,
    Success,
    Warning,
    Error
}

public partial class AppDialog : Window
{
    private bool _confirmed;
    private bool _isConfirmation;

    public AppDialog()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Opened += OnOpened;
    }

    public static async Task ShowMessageAsync(
        Window owner,
        string title,
        string message,
        AppDialogTone? tone = null)
    {
        var dialog = Create(title, message, tone ?? InferTone(title), false, "Aceptar", "Cerrar");
        await dialog.ShowDialog(owner);
    }

    public static async Task<bool> ShowConfirmAsync(
        Window owner,
        string title,
        string message,
        string confirmText = "Sí",
        string cancelText = "No",
        AppDialogTone? tone = null)
    {
        var dialog = Create(title, message, tone ?? InferTone(title), true, confirmText, cancelText);
        await dialog.ShowDialog(owner);
        return dialog._confirmed;
    }

    private static AppDialog Create(
        string title,
        string message,
        AppDialogTone tone,
        bool isConfirmation,
        string confirmText,
        string cancelText)
    {
        var dialog = new AppDialog
        {
            _isConfirmation = isConfirmation
        };

        var accent = new SolidColorBrush(Color.Parse(GetAccentColor(tone)));
        dialog.Title = title;
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.AccentBorder.Background = accent;
        dialog.ConfirmButton.Background = accent;
        dialog.ConfirmButton.Content = $"{StripShortcut(confirmText)} (Enter)";
        dialog.CancelButton.Content = $"{StripShortcut(cancelText)} (Esc)";
        dialog.CancelButton.IsVisible = isConfirmation;
        dialog.ShortcutText.Text = isConfirmation
            ? $"Enter: {StripShortcut(confirmText)}   |   Esc: {StripShortcut(cancelText)}"
            : "Enter o Esc: Cerrar";

        return dialog;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Activate();

        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen != null)
        {
            var area = screen.WorkingArea;
            MaxWidth = Math.Max(340, Math.Min(560, area.Width * 0.9));
            MaxHeight = Math.Max(210, Math.Min(520, area.Height * 0.8));
            Width = Math.Min(440, MaxWidth);
            Height = Math.Min(250, MaxHeight);
        }

        if (_isConfirmation)
            CancelButton.Focus();
        else
            ConfirmButton.Focus();
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Confirm();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Confirm();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Cancel();

    private void Confirm()
    {
        _confirmed = true;
        Close();
    }

    private void Cancel()
    {
        _confirmed = false;
        Close();
    }

    private static AppDialogTone InferTone(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();

        if (normalized.Contains("error") ||
            normalized.Contains("sin conexión") ||
            normalized.Contains("eliminar") ||
            normalized.Contains("eliminación") ||
            normalized.Contains("desactivar") ||
            normalized.Contains("dar de baja"))
            return AppDialogTone.Error;
        if (normalized.Contains("éxito") || normalized.Contains("completado"))
            return AppDialogTone.Success;
        if (normalized.Contains("advertencia") ||
            normalized.Contains("aviso") ||
            normalized.Contains("cancelar") ||
            normalized.Contains("confirmar") ||
            normalized.Contains("salir") ||
            normalized.Contains("pendiente"))
            return AppDialogTone.Warning;

        return AppDialogTone.Information;
    }

    private static string GetAccentColor(AppDialogTone tone) => tone switch
    {
        AppDialogTone.Success => "#2E7D32",
        AppDialogTone.Warning => "#EF6C00",
        AppDialogTone.Error => "#C62828",
        _ => "#1976D2"
    };

    private static string StripShortcut(string text)
    {
        var shortcutIndex = text.LastIndexOf(" (", StringComparison.Ordinal);
        return shortcutIndex > 0 ? text[..shortcutIndex] : text;
    }
}

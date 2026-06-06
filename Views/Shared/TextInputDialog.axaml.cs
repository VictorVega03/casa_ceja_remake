using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace CasaCejaRemake.Views.Shared;

public partial class TextInputDialog : Window
{
    private string? _result;

    public TextInputDialog()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Opened += (_, _) =>
        {
            ValueInput.Focus();
            ValueInput.SelectAll();
        };
    }

    public static async Task<string?> ShowAsync(
        Window owner,
        string title,
        string prompt,
        string defaultValue)
    {
        var dialog = new TextInputDialog();
        var accent = new SolidColorBrush(Color.Parse(GetAccentColor(title)));

        dialog.Title = title;
        dialog.TitleText.Text = title;
        dialog.PromptText.Text = prompt;
        dialog.ValueInput.Text = defaultValue;
        dialog.AccentBorder.Background = accent;
        dialog.SaveButton.Background = accent;

        await dialog.ShowDialog(owner);
        return dialog._result;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Save();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) => Save();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    private void Save()
    {
        _result = ValueInput.Text;
        Close();
    }

    private static string GetAccentColor(string title)
    {
        var normalized = title.ToLowerInvariant();
        return normalized.Contains("unidad") || normalized.Contains("medida")
            ? "#6A1B9A"
            : "#1565C0";
    }
}

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CasaCejaRemake.Views.POS;

public partial class QuantityDialog : Window
{
    private int? _result;

    public QuantityDialog()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Opened += (_, _) => QuantityInput.Focus();
    }

    public static async Task<int?> ShowAsync(
        Window owner,
        string productName,
        int currentQuantity)
    {
        var dialog = new QuantityDialog();
        dialog.ProductText.Text = productName;
        dialog.QuantityInput.Value = currentQuantity;

        await dialog.ShowDialog(owner);
        return dialog._result;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Apply();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e) => Apply();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    private void Apply()
    {
        _result = Math.Clamp((int)(QuantityInput.Value ?? 1), 1, 9999);
        Close();
    }
}

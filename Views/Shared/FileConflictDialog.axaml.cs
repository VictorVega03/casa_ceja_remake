using System.Threading.Tasks;
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CasaCejaRemake.Views.Shared;

public enum FileConflictResult
{
    Replace,
    Duplicate,
    Cancel
}

public partial class FileConflictDialog : Window
{
    private FileConflictResult _result = FileConflictResult.Cancel;

    public FileConflictDialog()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Activate();
        CancelButton.Focus();
    }

    public static async Task<FileConflictResult> ShowAsync(Window owner, string fileName)
    {
        var dialog = new FileConflictDialog();
        dialog.FileNameText.Text = fileName;
        await dialog.ShowDialog(owner);
        return dialog._result;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnReplaceClick(object? sender, RoutedEventArgs e) =>
        Complete(FileConflictResult.Replace);

    private void OnDuplicateClick(object? sender, RoutedEventArgs e) =>
        Complete(FileConflictResult.Duplicate);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    private void Complete(FileConflictResult result)
    {
        _result = result;
        Close();
    }
}

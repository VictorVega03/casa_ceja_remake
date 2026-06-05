using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace CasaCejaRemake.Views.Shared
{
    public partial class ModuleExitDialog : Window
    {
        private bool _confirmed;

        public ModuleExitDialog()
        {
            InitializeComponent();
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            Opened += (_, _) => CancelButton.Focus();
        }

        public static async Task<bool> ShowAsync(
            Window owner,
            string title,
            string message,
            string accentColor,
            string confirmText = "Salir",
            string cancelText = "Cancelar")
        {
            var accentBrush = new SolidColorBrush(Color.Parse(accentColor));
            var dialog = new ModuleExitDialog();

            dialog.TitleText.Text = title;
            dialog.MessageText.Text = message;
            dialog.AccentBorder.Background = accentBrush;
            dialog.ExitButton.Background = accentBrush;
            dialog.ExitButton.Content = $"{confirmText} (Enter)";
            dialog.CancelButton.Content = $"{cancelText} (Esc)";
            dialog.ShortcutText.Text = $"Enter: {confirmText}   |   Esc: {cancelText}";

            await dialog.ShowDialog(owner);
            return dialog._confirmed;
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmExit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Cancel();
                e.Handled = true;
            }
        }

        private void OnExitClick(object? sender, RoutedEventArgs e) => ConfirmExit();

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Cancel();

        private void ConfirmExit()
        {
            _confirmed = true;
            Close();
        }

        private void Cancel()
        {
            _confirmed = false;
            Close();
        }
    }
}

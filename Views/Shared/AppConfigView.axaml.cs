using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Shared;

namespace CasaCejaRemake.Views.Shared
{
    public partial class AppConfigView : Window
    {
        public AppConfigView()
        {
            InitializeComponent();

            // Shortcut: Esc para cerrar
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is AppConfigViewModel vm)
            {
                vm.CloseRequested += (s, args) => Close();
            }
        }
    }
}

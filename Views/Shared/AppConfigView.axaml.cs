using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Shared;
using casa_ceja_remake.Helpers;
using System.Collections.Generic;

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
            KeyboardShortcutHelper.HandleShortcut(e, new Dictionary<Key, System.Action>
            {
                { Key.Escape, Close }
            });
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

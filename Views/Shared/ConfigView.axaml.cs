using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Shared;
using casa_ceja_remake.Helpers;
using System.Collections.Generic;

namespace CasaCejaRemake.Views.Shared
{
    public partial class PosTerminalConfigView : Window
    {
        public PosTerminalConfigView()
        {
            InitializeComponent();
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, System.EventArgs e)
        {
            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            KeyboardShortcutHelper.HandleShortcut(e, new Dictionary<Key, System.Action>
            {
                { Key.Escape, Close }
            });

            base.OnKeyDown(e);
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is PosTerminalConfigViewModel vm)
            {
                vm.CloseRequested += (s, args) => Close();
            }
        }
    }
}

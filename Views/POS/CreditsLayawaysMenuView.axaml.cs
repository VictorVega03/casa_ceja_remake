using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreditsLayawaysMenuView : Window
    {
        public CreditsLayawaysMenuView()
        {
            InitializeComponent();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is CreditsLayawaysMenuViewModel vm)
            {
                vm.HandleKeyPress(e.Key.ToString());
            }
        }
    }
}

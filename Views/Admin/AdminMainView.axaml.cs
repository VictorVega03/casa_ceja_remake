using Avalonia.Controls;
using CasaCejaRemake.ViewModels.Admin;
using System;

namespace CasaCejaRemake.Views.Admin
{
    public partial class AdminMainView : Window
    {
        public AdminMainView()
        {
            InitializeComponent();
            this.Closed += OnClosed;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (DataContext is AdminMainViewModel viewModel)
            {
                viewModel.StopConnectivityMonitor();
            }
        }
    }
}

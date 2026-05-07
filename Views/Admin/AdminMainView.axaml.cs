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
            this.Opened += OnOpened;
        }

        private async void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is AdminMainViewModel viewModel)
            {
                await viewModel.CheckConnectivityCommand.ExecuteAsync(null);
            }
        }
    }
}

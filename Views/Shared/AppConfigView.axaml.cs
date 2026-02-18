using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Shared;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.Shared
{
    public partial class AppConfigView : Window
    {
        private AppConfigViewModel? _viewModel;

        public AppConfigView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as AppConfigViewModel;

            if (_viewModel != null)
            {
                _viewModel.CloseRequested += (s, args) => Close();

                _viewModel.AdminVerificationRequested += async () =>
                {
                    var app = (CasaCejaRemake.App)Avalonia.Application.Current!;
                    var userService = app.GetUserService();
                    if (userService == null) return false;
                    return await AdminVerificationHelper.VerifyAdminAsync(this, userService);
                };
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CloseCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                    return;
            }

            base.OnKeyDown(e);
        }
    }
}


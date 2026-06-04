using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.ViewModels.Admin;

namespace CasaCejaRemake.Views.Admin
{
    public partial class BranchFormView : Window
    {
        private BranchFormViewModel? _viewModel;

        public BranchFormView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is BranchFormViewModel vm)
            {
                _viewModel = vm;
                _viewModel.SetParentWindow(this);
                _viewModel.CloseRequested += (_, _) => Close();

                Dispatcher.UIThread.Post(() =>
                {
                    NameTextBox.Focus();
                    NameTextBox.SelectAll();
                }, DispatcherPriority.Loaded);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            KeyboardShortcutHelper.HandleShortcut(e, new Dictionary<Key, Action>
            {
                { Key.Escape, () => _viewModel?.CancelCommand.Execute(null) },
                { Key.F5,    () => { if (_viewModel?.IsSaving == false) _viewModel.SaveCommand.Execute(null); } },
            });
            base.OnKeyDown(e);
        }
    }
}

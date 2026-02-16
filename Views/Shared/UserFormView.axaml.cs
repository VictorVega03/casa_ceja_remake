using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.ViewModels.Shared;
using System;
using System.Collections.Generic;

namespace CasaCejaRemake.Views.Shared
{
    public partial class UserFormView : Window
    {
        private UserFormViewModel? _viewModel;

        public UserFormView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is UserFormViewModel vm)
            {
                _viewModel = vm;
                _viewModel.CloseRequested += OnCloseRequested;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is UserFormViewModel vm)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => vm.CancelCommand.Execute(null) },
                    { Key.F5, () => 
                        { 
                            if (vm.SaveCommand.CanExecute(null)) 
                                vm.SaveCommand.Execute(null); 
                        } 
                    }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreateCreditView : Window
    {
        private CreateCreditViewModel? _viewModel;

        public CreateCreditView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CreateCreditViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CreditCreated += OnCreditCreated;
                _viewModel.Cancelled += OnCancelled;
            }
        }

        private void OnCreditCreated(object? sender, Credit e)
        {
            Tag = e;
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is CreateCreditViewModel vm)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Enter, () => vm.ConfirmCommand.Execute(null) },
                    { Key.Escape, () => vm.CancelCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // Delegar otros atajos al ViewModel
                vm.HandleKeyPress(e.Key.ToString());
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CreditCreated -= OnCreditCreated;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

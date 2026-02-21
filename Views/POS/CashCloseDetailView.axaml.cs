using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CashCloseDetailView : Window
    {
        private CashCloseDetailViewModel? _viewModel;

        public CashCloseDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            // Asegurar que la ventana tenga focus para recibir eventos de teclado
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CashCloseDetailViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.PrintRequested += OnPrintRequested;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private async void OnPrintRequested(object? sender, (string Folio, string TicketText) args)
        {
            // ShowTicketDialog auto-prints internally — just show the dialog
            await DialogHelper.ShowTicketDialog(this, args.Folio, args.TicketText);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => _viewModel.CloseCommand.Execute(null) },
                    { Key.Enter, () => _viewModel.CloseCommand.Execute(null) },
                    { Key.F7, () => _viewModel.PrintCommand.Execute(null) }
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
                _viewModel.PrintRequested -= OnPrintRequested;
            }
            base.OnClosed(e);
        }
    }
}

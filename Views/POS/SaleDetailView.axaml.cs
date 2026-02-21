using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class SaleDetailView : Window
    {
        private SaleDetailViewModel? _viewModel;

        public SaleDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as SaleDetailViewModel;

            if (_viewModel != null)
            {
                _viewModel.CloseRequested += OnCloseRequested;
                _viewModel.ReprintRequested += OnReprintRequested;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel == null)
            {
                base.OnKeyDown(e);
                return;
            }

            var shortcuts = new Dictionary<Key, Action>
            {
                { Key.Escape, () => Close() },
                { Key.F5, () => _viewModel.ReprintCommand.Execute(null) }
            };

            if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private async void OnReprintRequested(object? sender, string ticketText)
        {
            // Imprimir directamente sin mostrar vista previa (ya se ve en la vista de detalle)
            try
            {
                var app = Avalonia.Application.Current as CasaCejaRemake.App;
                var printService = app?.GetPrintService();
                if (printService != null)
                {
                    Console.WriteLine("[SaleDetailView] Reimprimiendo ticket directamente...");
                    var result = await printService.PrintAsync(ticketText);
                    Console.WriteLine(result.Success
                        ? "[SaleDetailView] ✓ Ticket impreso correctamente"
                        : $"[SaleDetailView] ✗ {result.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine("[SaleDetailView] PrintService no disponible");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaleDetailView] Error al reimprimir: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= OnCloseRequested;
                _viewModel.ReprintRequested -= OnReprintRequested;
            }
            base.OnClosed(e);
        }
    }
}

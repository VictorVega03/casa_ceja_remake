using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Views.POS
{
    public partial class CreateLayawayView : Window
    {
        private CreateLayawayViewModel? _viewModel;

        public CreateLayawayView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CreateLayawayViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.LayawayCreated += OnLayawayCreated;
                _viewModel.Cancelled += OnCancelled;
            }
        }

        private void OnLayawayCreated(object? sender, Layaway e)
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
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    _viewModel?.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.F5:
                    _viewModel?.ConfirmCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.LayawayCreated -= OnLayawayCreated;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

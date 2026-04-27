using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Views.POS
{
    public partial class QuickCustomerView : Window
    {
        private QuickCustomerViewModel? _viewModel;

        public QuickCustomerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnPhoneTextChanging(object? sender, TextChangingEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            var newText = textBox.Text ?? string.Empty;
            if (!string.IsNullOrEmpty(newText) && !newText.All(char.IsDigit))
            {
                var filtered = new string(newText.Where(char.IsDigit).ToArray());
                var cursor = textBox.CaretIndex;
                textBox.Text = filtered;
                textBox.CaretIndex = Math.Min(cursor, filtered.Length);
            }
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as QuickCustomerViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CustomerCreated += OnCustomerCreated;
                _viewModel.Cancelled += OnCancelled;
            }
            
            NameBox?.Focus();
        }

        private void OnCustomerCreated(object? sender, Customer e)
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
            if (DataContext is QuickCustomerViewModel vm)
            {
                vm.HandleKeyPress(e.Key.ToString());
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CustomerCreated -= OnCustomerCreated;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}

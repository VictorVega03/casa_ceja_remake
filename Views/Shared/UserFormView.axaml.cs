using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using casa_ceja_remake.Helpers;
using CasaCejaRemake.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CasaCejaRemake.Views.Shared
{
    public partial class UserFormView : Window
    {
        private UserFormViewModel? _viewModel;
        private TextBox? _phoneTextBox;

        public UserFormView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is UserFormViewModel vm)
            {
                _viewModel = vm;
                _viewModel.CloseRequested += OnCloseRequested;
            }

            // Encontrar el TextBox de teléfono y agregar validación
            _phoneTextBox = this.FindControl<TextBox>("PhoneTextBox");
            if (_phoneTextBox != null)
            {
                _phoneTextBox.TextChanging += OnPhoneTextChanging;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnPhoneTextChanging(object? sender, TextChangingEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            var newText = textBox.Text ?? string.Empty;
            
            // Verificar si el nuevo texto contiene solo números
            if (!string.IsNullOrEmpty(newText) && !newText.All(char.IsDigit))
            {
                // Filtrar solo los números
                var filtered = new string(newText.Where(char.IsDigit).ToArray());
                
                // Guardar la posición del cursor
                var cursorPosition = textBox.CaretIndex;
                
                // Actualizar el texto
                textBox.Text = filtered;
                
                // Restaurar la posición del cursor (ajustada si es necesario)
                textBox.CaretIndex = Math.Min(cursorPosition, filtered.Length);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is UserFormViewModel vm)
            {
                // F5 para guardar
                if (e.Key == Key.F5 && !vm.IsReadOnly && vm.SaveCommand.CanExecute(null))
                {
                    vm.SaveCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // Esc para cancelar (solo cierra el dialog, no la vista padre)
                if (e.Key == Key.Escape)
                {
                    vm.CancelCommand.Execute(null);
                    e.Handled = true;
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

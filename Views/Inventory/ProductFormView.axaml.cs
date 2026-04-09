using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;
using casa_ceja_remake.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class ProductFormView : Window
    {
        public ProductFormView()
        {
            InitializeComponent();
            this.Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is ProductFormViewModel vm)
            {
                vm.StartSaveConfirmation += async (s, args) =>
                {
                    bool res = await DialogHelper.ShowConfirmDialog(this, "Guardar", "¿Confirmar guardado?");
                    if (res)
                    {
                        await vm.ConfirmSaveAsync();
                    }
                };

                vm.CancelRequested += async (s, args) =>
                {
                    if (vm.IsReadOnlyView)
                    {
                        Close();
                        return;
                    }

                    bool res = await DialogHelper.ShowConfirmDialog(this, "Cancelar", "¿Estás seguro de salir sin guardar?");
                    if (res)
                    {
                        Close();
                    }
                };
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (DataContext is ProductFormViewModel vm)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    { Key.Escape, () => vm.CancelCommand.Execute(null) },
                    { Key.F2, () => { if (vm.IsMultipleMode) vm.AddToListCommand.Execute(null); } },
                    { Key.F3, () => vm.SaveCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }

                // La navegación con Enter no es trivial en Avalonia 11 sin romper Comboboxes o Multilines. Tab es el avance nativo.
            }
            base.OnKeyDown(e);
        }

        private void DecimalTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                var text = textBox.Text;
                var validChars = text.Where(c => char.IsDigit(c) || c == '.').ToArray();
                var newText = new string(validChars);
                
                // Solo permitir un punto decimal
                int decimalCount = newText.Count(c => c == '.');
                if (decimalCount > 1)
                {
                    int firstDecimal = newText.IndexOf('.');
                    newText = newText.Substring(0, firstDecimal + 1) + newText.Substring(firstDecimal + 1).Replace(".", "");
                }

                if (text != newText)
                {
                    textBox.Text = newText;
                    textBox.CaretIndex = newText.Length;
                }
            }
        }

        private void IntTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                var text = textBox.Text;
                var validChars = text.Where(c => char.IsDigit(c)).ToArray();
                var newText = new string(validChars);

                if (text != newText)
                {
                    textBox.Text = newText;
                    textBox.CaretIndex = newText.Length;
                }
            }
        }
        
        private void NoSpecialCharsTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                var text = textBox.Text;
                // Permitir letras, números, espacios y los caracteres - _ .
                var validChars = text.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '.').ToArray();
                var newText = new string(validChars);
                if (text != newText)
                {
                    textBox.Text = newText;
                    textBox.CaretIndex = newText.Length;
                }
            }
        }
    }
}

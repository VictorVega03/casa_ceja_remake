using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;
using CasaCejaRemake.ViewModels.Shared;
using CasaCejaRemake.Views.Shared;
using casa_ceja_remake.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class ProductEditView : Window
    {
        public ProductEditView()
        {
            InitializeComponent();
            this.Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is ProductFormViewModel vm)
            {
                vm.SetParentWindow(this);

                vm.StartSaveConfirmation += async (s, args) =>
                {
                    bool res = await ModuleExitDialog.ShowAsync(
                        this,
                        "Guardar cambios",
                        "¿Desea guardar los cambios realizados al producto?",
                        "#7D4A1E",
                        "Guardar");
                    if (res) await vm.ConfirmSaveAsync();
                };

                vm.CancelRequested += async (s, args) =>
                {
                    bool res = await DialogHelper.ShowConfirmDialog(this, "Cancelar", "¿Salir sin guardar los cambios?");
                    if (res) Close();
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
                    { Key.F3, () => vm.SaveCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts)) return;
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
                var newText = new string(text.Where(char.IsDigit).ToArray());
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
                var newText = new string(text.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '.').ToArray());
                if (text != newText)
                {
                    textBox.Text = newText;
                    textBox.CaretIndex = newText.Length;
                }
            }
        }
    }
}

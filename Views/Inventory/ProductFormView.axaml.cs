using Avalonia.Controls;
using Avalonia.Input;
using CasaCejaRemake.ViewModels.Inventory;
using casa_ceja_remake.Helpers;
using System;
using System.Collections.Generic;

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
    }
}

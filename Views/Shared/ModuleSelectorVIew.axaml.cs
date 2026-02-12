using Avalonia.Controls;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.Shared;
using CasaCejaRemake.Helpers;
using System;

namespace CasaCejaRemake.Views.Shared
{
    /// <summary>
    /// Vista del Selector de Módulos - Code Behind
    /// </summary>
    public partial class ModuleSelectorView : Window
    {
        public ModuleSelectorView()
        {
            InitializeComponent();
            
            // Suscribirse al evento de error al abrir carpeta
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ModuleSelectorViewModel viewModel)
            {
                viewModel.FolderOpenError += OnFolderOpenError;
            }
        }

        private async void OnFolderOpenError(object? sender, string errorMessage)
        {
            await casa_ceja_remake.Helpers.DialogHelper.ShowMessageDialog(
                this,
                "Error al abrir carpeta",
                errorMessage);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Desuscribirse del evento
            if (DataContext is ModuleSelectorViewModel viewModel)
            {
                viewModel.FolderOpenError -= OnFolderOpenError;
            }
            
            base.OnClosed(e);
        }
    }
}
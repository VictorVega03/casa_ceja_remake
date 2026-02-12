using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Services;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    /// <summary>
    /// Helper estático para manejar el flujo de exportación a Excel desde las vistas.
    /// Centraliza la lógica de: verificar duplicados → preguntar al usuario → exportar → mostrar resultado.
    /// Compatible con Windows y macOS.
    /// </summary>
    public static class ExportHelper
    {
        /// <summary>
        /// Flujo completo de exportación para una sola hoja.
        /// </summary>
        public static async Task ExportSingleSheetAsync<T>(
            Window parentWindow,
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle,
            string fileBaseName)
        {
            try
            {
                if (App.ExportService == null)
                {
                    await DialogHelper.ShowMessageDialog(parentWindow,
                        "El servicio de exportación no está disponible.", "Error");
                    return;
                }

                var filePath = await ResolveFilePathAsync(parentWindow, fileBaseName);
                if (filePath == null) return; // Cancelado

                var result = await App.ExportService.ExportToExcelAsync(
                    data, columns, sheetName, reportTitle, filePath);

                await ShowResultAsync(parentWindow, result);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageDialog(parentWindow,
                    $"Error al exportar: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Flujo completo de exportación para múltiples hojas.
        /// </summary>
        public static async Task ExportMultiSheetAsync(
            Window parentWindow,
            List<ExportSheetData> sheets,
            string fileBaseName)
        {
            try
            {
                if (App.ExportService == null)
                {
                    await DialogHelper.ShowMessageDialog(parentWindow,
                        "El servicio de exportación no está disponible.", "Error");
                    return;
                }

                var filePath = await ResolveFilePathAsync(parentWindow, fileBaseName);
                if (filePath == null) return; // Cancelado

                var result = await App.ExportService.ExportMultiSheetAsync(sheets, filePath);

                await ShowResultAsync(parentWindow, result);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageDialog(parentWindow,
                    $"Error al exportar: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Resuelve la ruta final del archivo, manejando duplicados.
        /// Retorna null si el usuario cancela.
        /// </summary>
        private static async Task<string?> ResolveFilePathAsync(
            Window parentWindow, string fileBaseName)
        {
            var existingFile = FileHelper.FindExistingFile(DocumentModule.POS, fileBaseName);

            if (existingFile != null)
            {
                // Archivo existe, preguntar al usuario
                var fileName = Path.GetFileName(existingFile);
                var action = await DialogHelper.ShowDuplicateFileDialog(parentWindow, fileName);

                switch (action)
                {
                    case DialogHelper.DuplicateFileAction.Replace:
                        return existingFile; // Sobrescribir
                    case DialogHelper.DuplicateFileAction.Duplicate:
                        return FileHelper.GetNextDuplicatePath(DocumentModule.POS, fileBaseName);
                    case DialogHelper.DuplicateFileAction.Cancel:
                    default:
                        return null; // Cancelar
                }
            }

            // No existe, usar ruta normal
            return FileHelper.GetReadableFilePath(DocumentModule.POS, fileBaseName);
        }

        /// <summary>
        /// Muestra el resultado de la exportación al usuario.
        /// </summary>
        private static async Task ShowResultAsync(Window parentWindow, ExportResult result)
        {
            if (result.Success)
            {
                var fileName = Path.GetFileName(result.FilePath);
                await DialogHelper.ShowMessageDialog(parentWindow,
                    $"Archivo exportado exitosamente:\n{fileName}\n\nUbicación: CasaCejaDocs/POS/",
                    "Exportación completada");
            }
            else
            {
                await DialogHelper.ShowMessageDialog(parentWindow,
                    result.ErrorMessage ?? "Error desconocido al exportar",
                    "Error de exportación");
            }
        }
    }
}

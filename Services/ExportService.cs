using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CasaCejaRemake.Helpers;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Resultado de una operación de exportación.
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }

        public static ExportResult Ok(string filePath) =>
            new() { Success = true, FilePath = filePath };

        public static ExportResult Error(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Definición de una columna para exportación.
    /// Permite mapear propiedades de cualquier objeto a columnas de Excel.
    /// </summary>
    public class ExportColumn<T>
    {
        public string Header { get; set; } = string.Empty;
        public Func<T, object?> ValueSelector { get; set; } = _ => null;
        public string Format { get; set; } = string.Empty; // Ej: "C2" para moneda, "$#,##0.00"
        public double Width { get; set; } = 15;
    }

    /// <summary>
    /// Servicio de exportación a Excel (.xlsx) usando ClosedXML.
    /// Toma datos de cualquier colección y genera archivos formateados.
    /// Los archivos se guardan en CasaCejaDocs/{módulo}/.
    /// </summary>
    public class ExportService
    {
        /// <summary>
        /// Exporta una colección de datos a Excel con columnas personalizadas.
        /// Método genérico que funciona con cualquier tipo de dato.
        /// </summary>
        /// <typeparam name="T">Tipo de los datos a exportar</typeparam>
        /// <param name="data">Colección de datos</param>
        /// <param name="columns">Definición de columnas</param>
        /// <param name="sheetName">Nombre de la hoja</param>
        /// <param name="reportTitle">Título del reporte (fila superior)</param>
        /// <param name="module">Módulo destino (POS, Inventario, Administrador)</param>
        /// <param name="fileBaseName">Nombre base del archivo</param>
        public async Task<ExportResult> ExportToExcelAsync<T>(
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle,
            DocumentModule module,
            string fileBaseName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Asegurar que las carpetas existan
                    FileHelper.EnsureDirectoriesExist();

                    var filePath = FileHelper.GetFilePath(module, fileBaseName);

                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    int row = 1;

                    // ===== TÍTULO =====
                    worksheet.Cell(row, 1).Value = reportTitle;
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                    worksheet.Range(row, 1, row, columns.Count).Merge();
                    row++;

                    // ===== FECHA DE GENERACIÓN =====
                    worksheet.Cell(row, 1).Value =
                        $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                    worksheet.Cell(row, 1).Style.Font.Italic = true;
                    worksheet.Range(row, 1, row, columns.Count).Merge();
                    row += 2; // Línea en blanco

                    // ===== ENCABEZADOS =====
                    for (int col = 0; col < columns.Count; col++)
                    {
                        var cell = worksheet.Cell(row, col + 1);
                        cell.Value = columns[col].Header;
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2196F3");
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    row++;

                    // ===== DATOS =====
                    var dataList = data.ToList();
                    foreach (var item in dataList)
                    {
                        for (int col = 0; col < columns.Count; col++)
                        {
                            var cell = worksheet.Cell(row, col + 1);
                            var value = columns[col].ValueSelector(item);

                            if (value is decimal decVal)
                                cell.Value = decVal;
                            else if (value is double dblVal)
                                cell.Value = dblVal;
                            else if (value is int intVal)
                                cell.Value = intVal;
                            else if (value is long longVal)
                                cell.Value = longVal;
                            else if (value is DateTime dtVal)
                                cell.Value = dtVal;
                            else if (value is bool boolVal)
                                cell.Value = boolVal ? "Sí" : "No";
                            else
                                cell.Value = value?.ToString() ?? string.Empty;

                            // Aplicar formato
                            if (!string.IsNullOrEmpty(columns[col].Format))
                                cell.Style.NumberFormat.Format = columns[col].Format;
                        }
                        row++;
                    }

                    // ===== FILA DE RESUMEN =====
                    worksheet.Cell(row + 1, 1).Value = $"Total de registros: {dataList.Count}";
                    worksheet.Cell(row + 1, 1).Style.Font.Italic = true;

                    // ===== AJUSTAR ANCHOS =====
                    for (int col = 0; col < columns.Count; col++)
                    {
                        worksheet.Column(col + 1).Width = columns[col].Width;
                    }

                    // ===== BORDES =====
                    if (dataList.Count > 0)
                    {
                        var headerRow = 4; // Fila de encabezados (1=título, 2=fecha, 3=vacía, 4=headers)
                        var dataRange = worksheet.Range(headerRow, 1, row - 1, columns.Count);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    }

                    // Guardar
                    workbook.SaveAs(filePath);

                    Console.WriteLine($"[ExportService] Archivo exportado: {filePath}");
                    return ExportResult.Ok(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExportService] Error exportando: {ex.Message}");
                    return ExportResult.Error($"Error al exportar: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Exporta datos simples (lista de diccionarios string→object).
        /// Útil para exportaciones rápidas desde DataGrids.
        /// </summary>
        public async Task<ExportResult> ExportSimpleAsync(
            List<Dictionary<string, object>> rows,
            string[] headers,
            string sheetName,
            string reportTitle,
            DocumentModule module,
            string fileBaseName)
        {
            var columns = headers.Select(h => new ExportColumn<Dictionary<string, object>>
            {
                Header = h,
                ValueSelector = dict => dict.TryGetValue(h, out var val) ? val : "",
                Width = 18
            }).ToList();

            return await ExportToExcelAsync(rows, columns, sheetName, reportTitle, module, fileBaseName);
        }
    }
}

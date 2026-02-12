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
    /// Datos de una hoja para exportación multi-hoja (wrapper no genérico).
    /// Permite almacenar hojas con diferentes tipos de datos en una misma lista.
    /// </summary>
    public class ExportSheetData
    {
        public string SheetName { get; set; } = string.Empty;
        public string ReportTitle { get; set; } = string.Empty;

        /// <summary>
        /// Función que recibe un XLWorkbook y agrega la hoja con datos formateados.
        /// Se usa internamente para encapsular la lógica tipada.
        /// </summary>
        internal Action<XLWorkbook>? BuildAction { get; set; }
    }

    /// <summary>
    /// Servicio de exportación a Excel (.xlsx) usando ClosedXML.
    /// Toma datos de cualquier colección y genera archivos formateados.
    /// Los archivos se guardan en CasaCejaDocs/{módulo}/.
    /// Compatible con Windows y macOS.
    /// </summary>
    public class ExportService
    {
        // ============ COLORES DEL TEMA ============
        private const string HEADER_BG_COLOR = "#2196F3";
        private const string ALTERNATE_ROW_COLOR = "#F5F5F5";
        private const string BORDER_COLOR = "#BDBDBD";

        /// <summary>
        /// Exporta una colección de datos a Excel con columnas personalizadas (una sola hoja).
        /// Método genérico que funciona con cualquier tipo de dato.
        /// </summary>
        public async Task<ExportResult> ExportToExcelAsync<T>(
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle,
            string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Asegurar que las carpetas existan
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using var workbook = new XLWorkbook();

                    BuildSheet(workbook, data, columns, sheetName, reportTitle);

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
        /// Exporta múltiples hojas a un mismo archivo Excel.
        /// Útil para reportes compuestos como Créditos + Abonos.
        /// </summary>
        public async Task<ExportResult> ExportMultiSheetAsync(
            List<ExportSheetData> sheets,
            string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using var workbook = new XLWorkbook();

                    foreach (var sheet in sheets)
                    {
                        sheet.BuildAction?.Invoke(workbook);
                    }

                    workbook.SaveAs(filePath);

                    Console.WriteLine($"[ExportService] Archivo multi-hoja exportado: {filePath}");
                    return ExportResult.Ok(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExportService] Error exportando multi-hoja: {ex.Message}");
                    return ExportResult.Error($"Error al exportar: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Crea un ExportSheetData tipado para usar con ExportMultiSheetAsync.
        /// Encapsula los datos y columnas en una función de construcción.
        /// </summary>
        public ExportSheetData CreateSheetData<T>(
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle)
        {
            return new ExportSheetData
            {
                SheetName = sheetName,
                ReportTitle = reportTitle,
                BuildAction = workbook => BuildSheet(workbook, data, columns, sheetName, reportTitle)
            };
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
            string filePath)
        {
            var columns = headers.Select(h => new ExportColumn<Dictionary<string, object>>
            {
                Header = h,
                ValueSelector = dict => dict.TryGetValue(h, out var val) ? val : "",
                Width = 18
            }).ToList();

            return await ExportToExcelAsync(rows, columns, sheetName, reportTitle, filePath);
        }

        // ============ MÉTODO PRIVADO: CONSTRUCCIÓN DE HOJA ============

        /// <summary>
        /// Construye una hoja individual con formato profesional.
        /// Reutilizado por exports de una y múltiples hojas.
        /// </summary>
        private void BuildSheet<T>(
            XLWorkbook workbook,
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            int row = 1;

            // ===== TÍTULO =====
            worksheet.Cell(row, 1).Value = reportTitle;
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#1565C0");
            worksheet.Range(row, 1, row, columns.Count).Merge();
            row++;

            // ===== FECHA DE GENERACIÓN =====
            worksheet.Cell(row, 1).Value =
                $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            worksheet.Cell(row, 1).Style.Font.Italic = true;
            worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#757575");
            worksheet.Range(row, 1, row, columns.Count).Merge();
            row += 2; // Línea en blanco

            // ===== ENCABEZADOS =====
            int headerRow = row;
            for (int col = 0; col < columns.Count; col++)
            {
                var cell = worksheet.Cell(row, col + 1);
                cell.Value = columns[col].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HEADER_BG_COLOR);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#1565C0");
            }
            row++;

            // ===== DATOS =====
            var dataList = data.ToList();
            bool alternate = false;
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

                    // Aplicar formato numérico
                    if (!string.IsNullOrEmpty(columns[col].Format))
                        cell.Style.NumberFormat.Format = columns[col].Format;

                    // Color alterno para filas
                    if (alternate)
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ALTERNATE_ROW_COLOR);
                }
                alternate = !alternate;
                row++;
            }

            // ===== FILA DE RESUMEN =====
            row++; // Línea en blanco
            var summaryCell = worksheet.Cell(row, 1);
            summaryCell.Value = $"Total de registros: {dataList.Count}";
            summaryCell.Style.Font.Italic = true;
            summaryCell.Style.Font.FontColor = XLColor.FromHtml("#616161");
            summaryCell.Style.Font.FontSize = 10;

            // ===== AJUSTAR ANCHOS =====
            for (int col = 0; col < columns.Count; col++)
            {
                worksheet.Column(col + 1).Width = columns[col].Width;
            }

            // ===== BORDES EN DATOS =====
            if (dataList.Count > 0)
            {
                var dataRange = worksheet.Range(headerRow, 1, row - 2, columns.Count);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml(BORDER_COLOR);
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml(BORDER_COLOR);
            }

            // ===== CONFIGURAR IMPRESIÓN =====
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 0); // Ajustar ancho a 1 página
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models.Results;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de exportación a Excel.
    /// </summary>
    public interface IExportService
    {
        Task<ExportResult> ExportToExcelAsync<T>(
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle,
            string filePath);

        Task<ExportResult> ExportMultiSheetAsync(
            List<ExportSheetData> sheets,
            string filePath);

        ExportSheetData CreateSheetData<T>(
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle);

        Task<ExportResult> ExportSimpleAsync(
            List<Dictionary<string, object>> rows,
            string[] headers,
            string sheetName,
            string reportTitle,
            string filePath);
    }
}

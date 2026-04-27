using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using CasaCejaRemake.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CasaCejaRemake.Services
{
    public class OutputProductInfo
    {
        public string Barcode      { get; set; } = string.Empty;
        public string ProductName  { get; set; } = string.Empty;
        public int    Quantity     { get; set; }
        public decimal UnitCost   { get; set; }
        public decimal LineTotal  { get; set; }
    }

    public class OutputRemissionData
    {
        public string  Folio              { get; set; } = string.Empty;
        public string  OriginBranchName   { get; set; } = string.Empty;
        public string  DestinationBranchName { get; set; } = string.Empty;
        public DateTime OutputDate        { get; set; }
        public string  UserName          { get; set; } = string.Empty;
        public List<OutputProductInfo> Lines { get; set; } = new();
        public decimal TotalAmount       { get; set; }
        public string? Notes             { get; set; }
        public byte[]? LogoBytes         { get; set; }
    }

    public class OutputRemissionPdfService
    {
        private const string BLUE      = "#1565C0";
        private const string DARK_BLUE = "#0D47A1";
        private const string LIGHT_BG  = "#E3F2FD";
        private const string GRAY      = "#757575";
        private const string LINE      = "#BDBDBD";

        public string GenerateAndSave(OutputRemissionData data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Guardar en CasaCejaDocs/Inventario/Remisiones/
            var moduleDir = FileHelper.GetModulePath(DocumentModule.Inventario);
            var remDir    = Path.Combine(moduleDir, "Salidas");
            Directory.CreateDirectory(remDir);

            var safeFolio = data.Folio.Replace("/", "-").Replace("\\", "-");
            var fileName  = $"Remision_{safeFolio}_{data.OutputDate:yyyyMMdd_HHmm}.pdf";
            var filePath  = Path.Combine(remDir, fileName);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginHorizontal(1.5f, Unit.Centimetre);
                    page.MarginVertical(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    page.Header().Element(h => BuildHeader(h, data));
                    page.Content().Element(c => BuildContent(c, data));
                    page.Footer().Element(f => BuildFooter(f, data));
                });
            }).GeneratePdf(filePath);

            return filePath;
        }

        public static void OpenInNativeViewer(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName       = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OutputRemissionPdfService] No se pudo abrir el visor: {ex.Message}");
            }
        }

        // ── Header ────────────────────────────────────────────────────────────
        private void BuildHeader(IContainer header, OutputRemissionData data)
        {
            header.BorderBottom(1).BorderColor(BLUE).PaddingBottom(10).Row(row =>
            {
                // Logo (si existe)
                if (data.LogoBytes?.Length > 0)
                {
                    row.AutoItem().Width(70).Image(data.LogoBytes).FitArea();
                }
                else
                {
                    row.AutoItem().Width(70).Text("").FontSize(8);
                }

                row.RelativeItem().PaddingLeft(16).Column(col =>
                {
                    col.Item().Text("REMISIÓN DE SALIDA")
                        .FontSize(20).Bold().FontColor(DARK_BLUE);
                    col.Item().Text($"Folio: {data.Folio}")
                        .FontSize(11).FontColor(BLUE).Bold();
                    col.Item().Text($"Fecha: {data.OutputDate:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor(GRAY);
                });
            });
        }

        // ── Content ───────────────────────────────────────────────────────────
        private void BuildContent(IContainer content, OutputRemissionData data)
        {
            content.PaddingTop(14).Column(col =>
            {
                // Sección: datos del movimiento
                col.Item().Background(LIGHT_BG).Padding(10).Column(info =>
                {
                    info.Item().Text("INFORMACIÓN DEL MOVIMIENTO")
                        .FontSize(9).Bold().FontColor(DARK_BLUE);
                    info.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            InfoRow(c, "Sucursal Origen:",  data.OriginBranchName);
                            InfoRow(c, "Sucursal Destino:", data.DestinationBranchName);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            InfoRow(c, "Registrado por:", data.UserName);
                            if (!string.IsNullOrWhiteSpace(data.Notes))
                                InfoRow(c, "Notas:", data.Notes!);
                        });
                    });
                });

                col.Item().PaddingTop(14).Column(prod =>
                {
                    prod.Item().BorderBottom(1).BorderColor(BLUE).PaddingBottom(4)
                        .Text("PRODUCTOS").FontSize(9).Bold().FontColor(DARK_BLUE);

                    prod.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(90);  // Código
                            cols.RelativeColumn();     // Descripción
                            cols.ConstantColumn(55);  // Cantidad
                            cols.ConstantColumn(80);  // Costo Unit.
                            cols.ConstantColumn(80);  // Total
                        });

                        // Encabezado de tabla
                        table.Header(h =>
                        {
                            void HeaderCell(string text)
                            {
                                h.Cell().Background(BLUE).Padding(5)
                                    .Text(text).FontColor(Colors.White).Bold().FontSize(8.5f);
                            }
                            HeaderCell("Código");
                            HeaderCell("Descripción");
                            HeaderCell("Cant.");
                            HeaderCell("Costo Unit.");
                            HeaderCell("Total");
                        });

                        // Filas de productos
                        bool alt = false;
                        foreach (var line in data.Lines)
                        {
                            string bg = alt ? "#F5F5F5" : "#FFFFFF";
                            alt = !alt;

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LINE).Padding(5)
                                .Text(line.Barcode).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LINE).Padding(5)
                                .Text(line.ProductName).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LINE).Padding(5)
                                .Text(line.Quantity.ToString()).FontSize(9).AlignCenter();
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LINE).Padding(5)
                                .Text(line.UnitCost.ToString("C2")).FontSize(9).AlignRight();
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(LINE).Padding(5)
                                .Text(line.LineTotal.ToString("C2")).FontSize(9).AlignRight();
                        }
                    });

                    // Total
                    prod.Item().AlignRight().PaddingTop(8).PaddingRight(2)
                        .Background(DARK_BLUE).Padding(8)
                        .Text($"TOTAL:  {data.TotalAmount:C2}")
                        .FontSize(12).Bold().FontColor(Colors.White);
                });
            });
        }

        // ── Footer: firmas ────────────────────────────────────────────────────
        private void BuildFooter(IContainer footer, OutputRemissionData data)
        {
            footer.PaddingTop(20).BorderTop(1).BorderColor(LINE).PaddingTop(14).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("ENTREGÓ:").Bold().FontSize(9).FontColor(DARK_BLUE);
                    c.Item().PaddingTop(28).BorderBottom(1).BorderColor(Colors.Black).Text("").FontSize(9);
                    c.Item().PaddingTop(4).Text("Nombre y Firma").FontSize(8).FontColor(GRAY);
                    c.Item().PaddingTop(2).Text(data.OriginBranchName).FontSize(8).FontColor(GRAY);
                });

                row.ConstantItem(40);

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("RECIBIÓ:").Bold().FontSize(9).FontColor(DARK_BLUE);
                    c.Item().PaddingTop(28).BorderBottom(1).BorderColor(Colors.Black).Text("").FontSize(9);
                    c.Item().PaddingTop(4).Text("Nombre y Firma").FontSize(8).FontColor(GRAY);
                    c.Item().PaddingTop(2).Text(data.DestinationBranchName).FontSize(8).FontColor(GRAY);
                });
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static void InfoRow(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(3).Row(r =>
            {
                r.ConstantItem(110).Text(label).FontSize(9).Bold().FontColor(GRAY);
                r.RelativeItem().Text(value).FontSize(9);
            });
        }
    }
}

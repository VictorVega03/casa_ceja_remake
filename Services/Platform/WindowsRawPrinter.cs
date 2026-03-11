using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CasaCejaRemake.Services.Platform
{
    /// <summary>
    /// Envía bytes crudos directamente a una impresora en Windows mediante P/Invoke a winspool.drv.
    /// Funciona con cualquier impresora que tenga driver instalado (incluida Xprinter).
    /// El driver recibe el contenido tal cual — sin intervención del spooler de Windows
    /// ni conversión de fuentes — igual que lp en macOS.
    /// 
    /// Solo se instancia/usa en tiempo de ejecución cuando el SO es Windows.
    /// No contiene directivas de compilación condicional para mantener la compilación
    /// cruzada (macOS build), pero los P/Invoke solo se llaman si RuntimeInformation
    /// confirma que es Windows.
    /// </summary>
    internal static class WindowsRawPrinter
    {
        // ============================================================
        // P/Invoke — winspool.drv
        // ============================================================

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true)]
        private static extern int StartDocPrinter(IntPtr hPrinter, int level, ref DocInfo1 pDocInfo);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential)]
        private struct DocInfo1
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string? pOutputFile;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        // ============================================================
        // API pública
        // ============================================================

        // ── Comandos ESC/POS ──────────────────────────────────────────────
        private static readonly byte[] ESC_INIT = { 0x1B, 0x40 };       // ESC @ — inicializar impresora
        private static readonly byte[] CUT_FEED = { 0x0A, 0x0A, 0x0A, 0x0A, 0x0A }; // 5 líneas de avance
        private static readonly byte[] GS_CUT   = { 0x1D, 0x56, 0x01 }; // GS V 1 — corte parcial

        /// <summary>
        /// Envía texto plano directamente a la impresora usando el driver instalado.
        /// Normaliza saltos de línea (\r\n → \n) para compatibilidad con impresoras
        /// térmicas en modo RAW, e inyecta comandos ESC/POS de inicialización y corte.
        /// </summary>
        /// <param name="printerName">Nombre exacto de la impresora tal como aparece en Windows.</param>
        /// <param name="text">Texto del ticket (ya formateado con el ancho correcto).</param>
        /// <returns>true si se envió correctamente al spooler.</returns>
        public static bool SendText(string printerName, string text)
        {
            // Normalizar \r\n → \n para impresoras térmicas (evita doble interlineado)
            var normalized = text.Replace("\r\n", "\n");

            var encoding = Encoding.UTF8;
            byte[] textBytes = encoding.GetBytes(normalized);

            // Construir payload: ESC@ + texto + avance + corte
            byte[] payload = new byte[ESC_INIT.Length + textBytes.Length + CUT_FEED.Length + GS_CUT.Length];
            int offset = 0;

            Buffer.BlockCopy(ESC_INIT, 0, payload, offset, ESC_INIT.Length);
            offset += ESC_INIT.Length;

            Buffer.BlockCopy(textBytes, 0, payload, offset, textBytes.Length);
            offset += textBytes.Length;

            Buffer.BlockCopy(CUT_FEED, 0, payload, offset, CUT_FEED.Length);
            offset += CUT_FEED.Length;

            Buffer.BlockCopy(GS_CUT, 0, payload, offset, GS_CUT.Length);

            return SendRawBytes(printerName, payload);
        }

        /// <summary>
        /// Envía bytes crudos (ESC/POS u otro) directamente a la impresora.
        /// </summary>
        public static bool SendRawBytes(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            {
                Console.WriteLine($"[WindowsRawPrinter] No se pudo abrir la impresora '{printerName}'. " +
                                  $"Error: {Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                var docInfo = new DocInfo1
                {
                    pDocName  = "Ticket Casa Ceja",
                    pOutputFile = null,
                    pDataType = "RAW"   // RAW = el driver recibe los bytes sin transformación
                };

                if (StartDocPrinter(hPrinter, 1, ref docInfo) == 0)
                {
                    Console.WriteLine($"[WindowsRawPrinter] StartDocPrinter falló. " +
                                      $"Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                try
                {
                    if (!StartPagePrinter(hPrinter))
                    {
                        Console.WriteLine($"[WindowsRawPrinter] StartPagePrinter falló. " +
                                          $"Error: {Marshal.GetLastWin32Error()}");
                        return false;
                    }

                    try
                    {
                        // Copiar bytes a memoria no administrada y enviar
                        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
                        try
                        {
                            Marshal.Copy(bytes, 0, ptr, bytes.Length);
                            bool written = WritePrinter(hPrinter, ptr, bytes.Length, out int dwWritten);

                            if (!written || dwWritten != bytes.Length)
                            {
                                Console.WriteLine($"[WindowsRawPrinter] WritePrinter escribió {dwWritten}/{bytes.Length} bytes. " +
                                                  $"Error: {Marshal.GetLastWin32Error()}");
                                return false;
                            }

                            Console.WriteLine($"[WindowsRawPrinter] {dwWritten} bytes enviados a '{printerName}'.");
                            return true;
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                    }
                    finally
                    {
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}

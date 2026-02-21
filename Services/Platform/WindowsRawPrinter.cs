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

        /// <summary>
        /// Envía texto plano directamente a la impresora usando el driver instalado.
        /// El texto se codifica en CP850 (codepage DOS español) para compatibilidad
        /// con drivers de impresoras térmicas que esperan ese encoding.
        /// </summary>
        /// <param name="printerName">Nombre exacto de la impresora tal como aparece en Windows.</param>
        /// <param name="text">Texto del ticket (ya formateado con el ancho correcto).</param>
        /// <returns>true si se envió correctamente al spooler.</returns>
        public static bool SendText(string printerName, string text)
        {
            // CP850: codepage DOS, compatible con la mayoría de drivers de impresoras térmicas
            // en español. Si el driver espera UTF-8, cambiar a Encoding.UTF8.
            var encoding = Encoding.GetEncoding(850);
            byte[] bytes = encoding.GetBytes(text);
            return SendRawBytes(printerName, bytes);
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

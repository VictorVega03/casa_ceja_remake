using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Tipo de módulo para determinar la subcarpeta de destino.
    /// </summary>
    public enum DocumentModule
    {
        POS,
        Inventario,
        Administrador
    }

    /// <summary>
    /// Helper multiplataforma para gestión de directorios de documentos.
    /// Crea y gestiona la estructura CasaCejaDocs/{POS,Inventario,Administrador}.
    /// </summary>
    public static class FileHelper
    {
        private const string ROOT_FOLDER = "CasaCejaDocs";

        private static readonly string[] SUB_FOLDERS = { "POS", "Inventario", "Administrador" };

        /// <summary>
        /// Obtiene la ruta raíz de documentos según el SO.
        ///   Windows: %USERPROFILE%\Documents\CasaCejaDocs
        ///   macOS:   ~/Documents/CasaCejaDocs
        /// </summary>
        public static string GetRootPath()
        {
            string documentsPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                documentsPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                documentsPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);

                // Fallback si devuelve ruta vacía
                if (string.IsNullOrEmpty(documentsPath))
                    documentsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Documents");
            }
            else
            {
                documentsPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);
            }

            return Path.Combine(documentsPath, ROOT_FOLDER);
        }

        /// <summary>
        /// Obtiene la ruta completa de un módulo específico.
        /// Ejemplo: ~/Documents/CasaCejaDocs/POS
        /// </summary>
        public static string GetModulePath(DocumentModule module)
        {
            string subFolder = module switch
            {
                DocumentModule.POS => "POS",
                DocumentModule.Inventario => "Inventario",
                DocumentModule.Administrador => "Administrador",
                _ => "POS"
            };

            return Path.Combine(GetRootPath(), subFolder);
        }

        /// <summary>
        /// Inicializa toda la estructura de carpetas.
        /// Verifica si existen antes de crearlas.
        /// Llamar una vez al iniciar la aplicación.
        /// </summary>
        /// <returns>true si todas las carpetas existen/fueron creadas correctamente</returns>
        public static bool EnsureDirectoriesExist()
        {
            try
            {
                var rootPath = GetRootPath();

                // Crear raíz si no existe
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                // Crear subcarpetas
                foreach (var subFolder in SUB_FOLDERS)
                {
                    var subPath = Path.Combine(rootPath, subFolder);
                    if (!Directory.Exists(subPath))
                        Directory.CreateDirectory(subPath);
                }

                Console.WriteLine($"[FileHelper] Directorios verificados en: {rootPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileHelper] Error creando directorios: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Genera un nombre de archivo con timestamp para evitar colisiones.
        /// Ejemplo: "Reporte_Ventas_20260210_143025.xlsx"
        /// </summary>
        public static string GenerateFileName(string baseName, string extension = ".xlsx")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // Sanitizar nombre base
            var safeName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
            return $"{safeName}_{timestamp}{extension}";
        }

        /// <summary>
        /// Obtiene la ruta completa para un archivo nuevo en el módulo indicado.
        /// </summary>
        public static string GetFilePath(DocumentModule module, string baseName, string extension = ".xlsx")
        {
            EnsureDirectoriesExist();
            var fileName = GenerateFileName(baseName, extension);
            return Path.Combine(GetModulePath(module), fileName);
        }
    }
}

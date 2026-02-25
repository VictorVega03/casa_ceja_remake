using System;
using System.Diagnostics;
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
                Console.WriteLine($"[FileHelper] Verificando estructura de carpetas en: {rootPath}");

                // Crear raíz si no existe
                if (!Directory.Exists(rootPath))
                {
                    Console.WriteLine($"[FileHelper] Creando carpeta raíz: {rootPath}");
                    Directory.CreateDirectory(rootPath);
                }
                else
                {
                    Console.WriteLine($"[FileHelper] Carpeta raíz ya existe: {rootPath}");
                }

                // Crear subcarpetas
                foreach (var subFolder in SUB_FOLDERS)
                {
                    var subPath = Path.Combine(rootPath, subFolder);
                    if (!Directory.Exists(subPath))
                    {
                        Console.WriteLine($"[FileHelper] Creando subcarpeta: {subFolder}");
                        Directory.CreateDirectory(subPath);
                    }
                    else
                    {
                        Console.WriteLine($"[FileHelper] Subcarpeta ya existe: {subFolder}");
                    }
                }

                Console.WriteLine($"[FileHelper] ✓ Estructura de directorios verificada correctamente");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileHelper] ✗ Error creando directorios: {ex.Message}");
                Console.WriteLine($"[FileHelper] Stack trace: {ex.StackTrace}");
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

        /// <summary>
        /// Genera un nombre de archivo legible con fecha.
        /// Ejemplo: "Reporte de Ventas 12_02_2026.xlsx"
        /// </summary>
        public static string GenerateReadableFileName(string baseName, string extension = ".xlsx")
        {
            var dateStr = DateTime.Now.ToString("dd_MM_yyyy");
            var safeName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
            return $"{safeName} {dateStr}{extension}";
        }

        /// <summary>
        /// Obtiene la ruta completa para un archivo con nombre legible.
        /// Ejemplo: ~/Documents/CasaCejaDocs/POS/Reporte de Ventas 12_02_2026.xlsx
        /// </summary>
        public static string GetReadableFilePath(DocumentModule module, string baseName, string extension = ".xlsx")
        {
            EnsureDirectoriesExist();
            var fileName = GenerateReadableFileName(baseName, extension);
            return Path.Combine(GetModulePath(module), fileName);
        }

        /// <summary>
        /// Busca archivos existentes que coincidan con el nombre base en la carpeta del módulo.
        /// Retorna la ruta del archivo existente o null si no existe.
        /// </summary>
        public static string? FindExistingFile(DocumentModule module, string baseName, string extension = ".xlsx")
        {
            var filePath = GetReadableFilePath(module, baseName, extension);
            return File.Exists(filePath) ? filePath : null;
        }

        /// <summary>
        /// Genera la ruta para un archivo duplicado con sufijo numérico.
        /// Ejemplo: "Reporte de Ventas 12_02_2026 (2).xlsx"
        /// </summary>
        public static string GetNextDuplicatePath(DocumentModule module, string baseName, string extension = ".xlsx")
        {
            EnsureDirectoriesExist();
            var dateStr = DateTime.Now.ToString("dd_MM_yyyy");
            var safeName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
            var modulePath = GetModulePath(module);

            int counter = 2;
            string filePath;
            do
            {
                filePath = Path.Combine(modulePath, $"{safeName} {dateStr} ({counter}){extension}");
                counter++;
            } while (File.Exists(filePath));

            return filePath;
        }

        /// <summary>
        /// Abre la carpeta raíz de documentos en el explorador de archivos del sistema operativo.
        /// Windows: Explorador de Archivos
        /// macOS: Finder
        /// Linux: Administrador de archivos predeterminado
        /// </summary>
        /// <returns>true si se abrió correctamente, false si hubo algún error</returns>
        public static bool OpenFolderInExplorer()
        {
            try
            {
                var rootPath = GetRootPath();
                
                // Verificar que la carpeta exista
                if (!Directory.Exists(rootPath))
                {
                    Console.WriteLine($"[FileHelper] La carpeta no existe: {rootPath}");
                    Console.WriteLine($"[FileHelper] Intentando crear la carpeta...");
                    
                    if (!EnsureDirectoriesExist())
                    {
                        Console.WriteLine($"[FileHelper] No se pudo crear la carpeta");
                        return false;
                    }
                }

                Console.WriteLine($"[FileHelper] Abriendo carpeta: {rootPath}");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows: usar explorer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer",
                        Arguments = $"\"{rootPath}\"",
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS: usar open
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{rootPath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Linux: usar xdg-open
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{rootPath}\"",
                        UseShellExecute = true
                    });
                }

                Console.WriteLine($"[FileHelper] ✓ Carpeta abierta correctamente");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileHelper] ✗ Error al abrir carpeta: {ex.Message}");
                Console.WriteLine($"[FileHelper] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Abre la carpeta donde reside la base de datos (ApplicationData/CasaCeja) en el explorador de archivos.
        /// Exclusivo para desarrollo.
        /// </summary>
        public static bool OpenDatabaseFolderInExplorer()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dbFolderPath = Path.Combine(appDataPath, "CasaCeja");

                // Verificar si existe la carpeta
                if (!Directory.Exists(dbFolderPath))
                {
                    Console.WriteLine($"[FileHelper] La carpeta de DB no existe: {dbFolderPath}");
                    return false;
                }

                Console.WriteLine($"[FileHelper] Abriendo carpeta de DB: {dbFolderPath}");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer",
                        Arguments = $"\"{dbFolderPath}\"",
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{dbFolderPath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{dbFolderPath}\"",
                        UseShellExecute = true
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileHelper] Error al abrir carpeta DB: {ex.Message}");
                return false;
            }
        }
    }
}

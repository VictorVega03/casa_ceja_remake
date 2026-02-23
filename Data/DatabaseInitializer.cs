using System;
using System.IO;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Data
{
    /// <summary>
    /// Maneja la inicialización de la base de datos con datos por defecto
    /// </summary>
    public class DatabaseInitializer
    {
        // =====================================================
        // 🚩 FLAG DE DESARROLLO — cambiar a false para producción
        //
        //   true  → ejecuta ScriptInicial.sql automáticamente al arrancar
        //           (solo si la BD está vacía: sin roles, users ni categorías)
        //   false → comportamiento normal, no toca la BD
        // =====================================================
        private const bool AUTO_RUN_SEED_SCRIPT = true;

        private readonly DatabaseService _databaseService;

        public DatabaseInitializer(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Verifica que la BD tenga los datos mínimos esperados del script inicial.
        /// No crea datos de catálogo — esos vienen del ScriptInicial.sql.
        /// Solo loguea advertencias si faltan tablas críticas.
        /// </summary>
        public async Task InitializeDefaultDataAsync()
        {
            // Si ya hay productos (BD precargada con el script), no hacer nada
            if (_databaseService.IsCatalogPreloaded)
            {
                Console.WriteLine("✅ BD precargada detectada");
                return;
            }

            Console.WriteLine("🔧 Verificando datos de la BD...");

            var roleCount     = await _databaseService.Table<Role>().CountAsync();
            var unitCount     = await _databaseService.Table<Unit>().CountAsync();
            var categoryCount = await _databaseService.Table<Category>().CountAsync();
            var branchCount   = await _databaseService.Table<Branch>().CountAsync();
            var userCount     = await _databaseService.Table<User>().CountAsync();

            Console.WriteLine($"   Roles: {roleCount} | Unidades: {unitCount} | Categorías: {categoryCount} | Sucursales: {branchCount} | Usuarios: {userCount}");

            bool bdVacia = roleCount == 0 && unitCount == 0 && categoryCount == 0 && userCount == 0;

            // ── Ejecución automática del script inicial (solo con flag activo) ──
            if (AUTO_RUN_SEED_SCRIPT && bdVacia)
            {
                await RunSeedScriptAsync();
                return;
            }

            if (roleCount == 0)
                Console.WriteLine("⚠️  Sin roles — ejecuta ScriptInicial.sql en la BD");
            if (unitCount == 0)
                Console.WriteLine("⚠️  Sin unidades de medida — ejecuta ScriptInicial.sql en la BD");
            if (categoryCount == 0)
                Console.WriteLine("⚠️  Sin categorías — ejecuta ScriptInicial.sql en la BD");
            if (branchCount == 0)
                Console.WriteLine("⚠️  Sin sucursales — ejecuta ScriptInicial.sql en la BD");
            if (userCount == 0)
                Console.WriteLine("⚠️  Sin usuarios — ejecuta ScriptInicial.sql en la BD");

            if (roleCount > 0 && unitCount > 0 && categoryCount > 0 && branchCount > 0 && userCount > 0)
                Console.WriteLine("✅ BD verificada correctamente");
        }

        /// <summary>
        /// Localiza ScriptInicial.sql y lo ejecuta contra la BD activa.
        /// Funciona en macOS y Windows buscando el archivo relativo al ejecutable.
        /// </summary>
        private async Task RunSeedScriptAsync()
        {
            Console.WriteLine("🌱 AUTO_RUN_SEED_SCRIPT = true — ejecutando ScriptInicial.sql...");

            // Buscar el script en varias ubicaciones posibles
            var scriptPath = FindSeedScript(out var searchedPaths);
            if (scriptPath == null)
            {
                Console.WriteLine("❌ ScriptInicial.sql no encontrado. Rutas buscadas:");
                foreach (var p in searchedPaths)
                    Console.WriteLine($"   - {p}");
                return;
            }

            Console.WriteLine($"📄 Script encontrado: {scriptPath}");

            try
            {
                var sql = await File.ReadAllTextAsync(scriptPath);

                // Dividir en sentencias individuales (ignorar comentarios y líneas vacías)
                var statements = SplitSqlStatements(sql);
                int executed = 0;
                int errors   = 0;

                foreach (var stmt in statements)
                {
                    try
                    {
                        await _databaseService.ExecuteAsync(stmt);
                        executed++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        Console.WriteLine($"⚠️  Error en sentencia: {ex.Message}");
                        Console.WriteLine($"   SQL: {stmt[..Math.Min(80, stmt.Length)]}...");
                    }
                }

                Console.WriteLine($"✅ Script ejecutado: {executed} sentencias OK, {errors} errores");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error leyendo/ejecutando el script: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca ScriptInicial.sql relativo al ejecutable.
        /// En desarrollo (dotnet run) el ejecutable está en bin/Debug/net8.0/,
        /// por lo que sube 3 niveles para llegar a la raíz del proyecto.
        /// En producción el script debe copiarse junto a los binarios en Data/Database/.
        /// </summary>
        private static string? FindSeedScript(out string[] searchedPaths)
        {
            const string relativePath = "Data/Database/ScriptInicial.sql";

            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            // Candidatos en orden de preferencia (siempre con Path.Combine — no importa el SO)
            var candidates = new[]
            {
                // 1. Junto al ejecutable en Data/Database/ (producción / publish)
                Path.GetFullPath(Path.Combine(basePath, relativePath)),
                // 2. Subiendo 3 niveles desde bin/Debug/net8.0/ o bin/Release/net8.0/ (dev)
                Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", relativePath)),
                // 3. Subiendo 4 niveles (por si publish está en una subcarpeta extra)
                Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", relativePath)),
                // 4. Directorio de trabajo actual (por si se ejecuta desde la raíz del proyecto)
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath)),
            };

            searchedPaths = candidates;

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Divide el contenido SQL en sentencias individuales,
        /// ignorando líneas de comentarios (--) y bloques vacíos.
        /// </summary>
        private static string[] SplitSqlStatements(string sql)
        {
            var results = new System.Collections.Generic.List<string>();
            var lines   = sql.Split('\n');
            var current = new System.Text.StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();

                // Ignorar líneas de puro comentario
                if (line.TrimStart().StartsWith("--"))
                    continue;

                current.AppendLine(line);

                // Una sentencia termina cuando hay un ; al final de la línea
                if (line.TrimEnd().EndsWith(';'))
                {
                    var stmt = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stmt))
                        results.Add(stmt);
                    current.Clear();
                }
            }

            return results.ToArray();
        }
    }
}
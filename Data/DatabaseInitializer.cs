using System;
using System.IO;
using System.Threading.Tasks;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.Data
{
    public class DatabaseInitializer
    {
        private static readonly bool AUTO_RUN_SEED_SCRIPT = true;
        private const string SEED_SCRIPT_NAME   = "ScriptInicial_CasaCeja.sql";

        private readonly DatabaseService _databaseService;
        private readonly ConfigService   _configService;

        public DatabaseInitializer(DatabaseService databaseService, ConfigService configService)
        {
            _databaseService = databaseService;
            _configService   = configService;
        }

        public async Task InitializeDefaultDataAsync()
        {
            var unitCount     = await _databaseService.Table<Unit>().CountAsync();
            var categoryCount = await _databaseService.Table<Category>().CountAsync();
            var branchCount   = await _databaseService.Table<Branch>().CountAsync();
            var userCount     = await _databaseService.Table<User>().CountAsync();
            var productCount  = await _databaseService.Table<Product>().CountAsync();

            Console.WriteLine($"📊 Estado BD → Unidades: {unitCount} | Categorías: {categoryCount} | Sucursales: {branchCount} | Usuarios: {userCount} | Productos: {productCount}");

            // Los roles NO se incluyen aquí — vienen exclusivamente del servidor via sync
            bool hasData = branchCount > 0 || userCount > 0 || productCount > 0;
            if (hasData)
            {
                Console.WriteLine("✅ BD con datos existentes — no se ejecuta seed");
                return;
            }

            Console.WriteLine("📭 BD vacía detectada");

            if (AUTO_RUN_SEED_SCRIPT)
            {
                var scriptPath = FindSeedScript();
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    Console.WriteLine($"🌱 Auto-seed habilitado — ejecutando: {SEED_SCRIPT_NAME}");
                    await RunScriptAsync(scriptPath);

                    // Después de cargar el script, establecer LastSyncTimestamp
                    // basándonos en el updated_at más reciente de los productos cargados.
                    // Esto evita que el primer Pull descargue los 8,799 productos
                    // que ya tenemos — solo traerá los modificados DESPUÉS del script.
                    await SetLastSyncFromScriptDataAsync();
                }
                else
                {
                    Console.WriteLine($"⚠️  AUTO_RUN_SEED_SCRIPT=true pero no se encontró: {SEED_SCRIPT_NAME}");
                }
            }
            else
            {
                Console.WriteLine("ℹ️  AUTO_RUN_SEED_SCRIPT=false — carga el script manualmente:");
                Console.WriteLine($"   sqlite3 ~/Library/Application\\ Support/CasaCeja/casaceja.db < Data/Database/{SEED_SCRIPT_NAME}");
            }
        }

        /// <summary>
        /// Al recrear BD desde script, fuerza LastSyncTimestamp=0
        /// para que el siguiente Pull sea completo.
        /// </summary>
        private async Task SetLastSyncFromScriptDataAsync()
        {
            try
            {
                Console.WriteLine("🔄 BD recreada desde script — reseteando LastSyncTimestamp a 0 para Pull completo");
                await _configService.UpdateAppConfigAsync(c => c.LastSyncTimestamp = 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error estableciendo LastSyncTimestamp desde script: {ex.Message}");
                // No es crítico — el Pull fallback hará since=0 si LastSyncTimestamp=0
            }
        }

        private string? FindSeedScript()
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Database", SEED_SCRIPT_NAME),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database", SEED_SCRIPT_NAME),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "Database", SEED_SCRIPT_NAME),
            };

            foreach (var path in possiblePaths)
            {
                var normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    Console.WriteLine($"📁 Script encontrado: {normalizedPath}");
                    return normalizedPath;
                }
            }

            return null;
        }

        public async Task<(int executed, int errors)> RunScriptAsync(string scriptPath)
        {
            Console.WriteLine($"▶️  Ejecutando script: {scriptPath}");

            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"❌ Archivo no encontrado: {scriptPath}");
                return (0, 1);
            }

            try
            {
                var sql        = await File.ReadAllTextAsync(scriptPath);
                var statements = SplitSqlStatements(sql);
                int executed   = 0;
                int errors     = 0;

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
                return (executed, errors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error leyendo/ejecutando el script: {ex.Message}");
                return (0, 1);
            }
        }

        private static string[] SplitSqlStatements(string sql)
        {
            var results = new System.Collections.Generic.List<string>();
            var lines   = sql.Split('\n');
            var current = new System.Text.StringBuilder();

            var ignoredPrefixes = new[]
            {
                "BEGIN",
                "COMMIT",
                "ROLLBACK",
                "PRAGMA",
            };

            foreach (var rawLine in lines)
            {
                var line    = rawLine.TrimEnd();
                var trimmed = line.TrimStart();

                if (trimmed.StartsWith("--"))
                    continue;

                current.AppendLine(line);

                if (line.TrimEnd().EndsWith(';'))
                {
                    var stmt = current.ToString().Trim();
                    current.Clear();

                    if (string.IsNullOrWhiteSpace(stmt))
                        continue;

                    var upper = stmt.TrimStart().ToUpperInvariant();
                    bool skip = false;
                    foreach (var prefix in ignoredPrefixes)
                    {
                        if (upper.StartsWith(prefix))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    results.Add(stmt);
                }
            }

            return results.ToArray();
        }
    }
}
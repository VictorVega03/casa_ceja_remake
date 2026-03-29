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
            var roleCount     = await _databaseService.Table<Role>().CountAsync();
            var unitCount     = await _databaseService.Table<Unit>().CountAsync();
            var categoryCount = await _databaseService.Table<Category>().CountAsync();
            var branchCount   = await _databaseService.Table<Branch>().CountAsync();
            var userCount     = await _databaseService.Table<User>().CountAsync();
            var productCount  = await _databaseService.Table<Product>().CountAsync();

            Console.WriteLine($"📊 Estado BD → Roles: {roleCount} | Unidades: {unitCount} | Categorías: {categoryCount} | Sucursales: {branchCount} | Usuarios: {userCount} | Productos: {productCount}");

            bool hasData = roleCount > 0 || branchCount > 0 || userCount > 0 || productCount > 0;
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
        /// Lee el updated_at más reciente de los productos recién insertados
        /// y lo guarda como LastSyncTimestamp. Así el primer Pull del login
        /// solo descarga productos modificados DESPUÉS de que se generó el script.
        /// </summary>
        private async Task SetLastSyncFromScriptDataAsync()
        {
            try
            {
                // Ya cargamos ConfigService antes de llamar a InitializeDefaultDataAsync
                // así que podemos usarlo directamente
                if (_configService.AppConfig.LastSyncTimestamp > 0)
                {
                    Console.WriteLine("ℹ️  LastSyncTimestamp ya tiene valor — no se sobreescribe");
                    return;
                }

                // Obtener el updated_at más reciente de los productos del script
                var products = await _databaseService.Table<Product>().ToListAsync();
                if (products.Count == 0)
                {
                    Console.WriteLine("⚠️  No se encontraron productos para establecer timestamp");
                    return;
                }

                // Buscar el updated_at más reciente
                DateTime maxUpdatedAt = DateTime.MinValue;
                foreach (var p in products)
                {
                    if (p.UpdatedAt > maxUpdatedAt)
                        maxUpdatedAt = p.UpdatedAt;
                }

                // Si el updated_at más reciente es anterior al año 2000,
                // el script no tenía fechas válidas — usar "hace 7 días" como fallback
                if (maxUpdatedAt < new DateTime(2000, 1, 1))
                {
                    Console.WriteLine("⚠️  updated_at del script inválido — usando fallback de 7 días");
                    maxUpdatedAt = DateTime.UtcNow.AddDays(-7);
                }

                // Convertir a Unix timestamp UTC
                var unixTimestamp = new DateTimeOffset(maxUpdatedAt, TimeSpan.Zero).ToUnixTimeSeconds();

                await _configService.UpdateAppConfigAsync(config =>
                {
                    config.LastSyncTimestamp = unixTimestamp;
                });

                Console.WriteLine($"✅ LastSyncTimestamp establecido desde script: {unixTimestamp} ({maxUpdatedAt:dd/MM/yyyy HH:mm:ss})");
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
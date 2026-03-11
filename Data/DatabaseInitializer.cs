using System;
using System.IO;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Data
{
    /// <summary>
    /// Maneja la inicialización de la base de datos.
    /// 
    /// COMPORTAMIENTO:
    /// - La BD siempre se crea con tablas VACÍAS.
    /// - Si AUTO_RUN_SEED_SCRIPT = true Y la BD está vacía → ejecuta SEED_SCRIPT_NAME automáticamente.
    /// - Si AUTO_RUN_SEED_SCRIPT = false → la BD queda vacía (para pruebas manuales).
    /// 
    /// En desarrollo: deja AUTO_RUN_SEED_SCRIPT = false y carga el script manualmente con sqlite3.
    /// En producción: pon AUTO_RUN_SEED_SCRIPT = true y SEED_SCRIPT_NAME al script del cliente.
    /// </summary>
    public class DatabaseInitializer
    {
        // =====================================================
        // 🚩 CONFIGURACIÓN DE AUTO-SEED
        // 
        // AUTO_RUN_SEED_SCRIPT:
        //   false → BD vacía (desarrollo/pruebas)
        //   true  → ejecuta SEED_SCRIPT_NAME al detectar BD vacía (producción)
        //
        // SEED_SCRIPT_NAME:
        //   Nombre del archivo .sql en Data/Database/ que se ejecutará.
        //   Ejemplos: "ScriptInicial.sql", "ScriptInicial_BellaCosmeticos.sql"
        // =====================================================
        private const bool AUTO_RUN_SEED_SCRIPT = true;
        private const string SEED_SCRIPT_NAME = "ScriptInicial_CasaCeja.sql";

        private readonly DatabaseService _databaseService;

        public DatabaseInitializer(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Verifica el estado de la BD y opcionalmente ejecuta el script inicial.
        /// </summary>
        public async Task InitializeDefaultDataAsync()
        {
            // Verificar conteos actuales
            var roleCount     = await _databaseService.Table<Role>().CountAsync();
            var unitCount     = await _databaseService.Table<Unit>().CountAsync();
            var categoryCount = await _databaseService.Table<Category>().CountAsync();
            var branchCount   = await _databaseService.Table<Branch>().CountAsync();
            var userCount     = await _databaseService.Table<User>().CountAsync();
            var productCount  = await _databaseService.Table<Product>().CountAsync();

            Console.WriteLine($"📊 Estado BD → Roles: {roleCount} | Unidades: {unitCount} | Categorías: {categoryCount} | Sucursales: {branchCount} | Usuarios: {userCount} | Productos: {productCount}");

            // Si ya hay datos, no hacer nada más
            bool hasData = roleCount > 0 || branchCount > 0 || userCount > 0 || productCount > 0;
            if (hasData)
            {
                Console.WriteLine("✅ BD con datos existentes — no se ejecuta seed");
                return;
            }

            // BD vacía detectada
            Console.WriteLine("📭 BD vacía detectada");

            if (AUTO_RUN_SEED_SCRIPT)
            {
                // Producción: ejecutar script automáticamente
                var scriptPath = FindSeedScript();
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    Console.WriteLine($"🌱 Auto-seed habilitado — ejecutando: {SEED_SCRIPT_NAME}");
                    await RunScriptAsync(scriptPath);
                }
                else
                {
                    Console.WriteLine($"⚠️  AUTO_RUN_SEED_SCRIPT=true pero no se encontró: {SEED_SCRIPT_NAME}");
                }
            }
            else
            {
                // Desarrollo: solo mostrar advertencia
                Console.WriteLine("ℹ️  AUTO_RUN_SEED_SCRIPT=false — carga el script manualmente:");
                Console.WriteLine($"   sqlite3 ~/Library/Application\\ Support/CasaCeja/casaceja.db < Data/Database/{SEED_SCRIPT_NAME}");
            }
        }

        /// <summary>
        /// Busca el script seed en ubicaciones conocidas.
        /// </summary>
        private string? FindSeedScript()
        {
            var possiblePaths = new[]
            {
                // Junto al ejecutable
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Database", SEED_SCRIPT_NAME),
                // En el directorio de trabajo
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database", SEED_SCRIPT_NAME),
                // Ruta absoluta desde el proyecto (desarrollo)
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

        /// <summary>
        /// Ejecuta un script SQL arbitrario contra la BD activa.
        /// </summary>
        /// <param name="scriptPath">Ruta absoluta al archivo .sql</param>
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
                var sql = await File.ReadAllTextAsync(scriptPath);
                var statements = SplitSqlStatements(sql);
                int executed = 0;
                int errors = 0;

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

        /// <summary>
        /// Divide el contenido SQL en sentencias individuales,
        /// ignorando líneas de comentarios (--), bloques vacíos,
        /// y sentencias de control de transacción/pragma que sqlite-net
        /// no soporta ejecutar como sentencias sueltas.
        /// </summary>
        private static string[] SplitSqlStatements(string sql)
        {
            var results = new System.Collections.Generic.List<string>();
            var lines   = sql.Split('\n');
            var current = new System.Text.StringBuilder();

            // Prefijos de sentencias que se deben ignorar
            var ignoredPrefixes = new[]
            {
                "BEGIN",
                "COMMIT",
                "ROLLBACK",
                "PRAGMA",
            };

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();
                var trimmed = line.TrimStart();

                // Ignorar líneas de puro comentario
                if (trimmed.StartsWith("--"))
                    continue;

                current.AppendLine(line);

                // Una sentencia termina cuando hay un ; al final de la línea
                if (line.TrimEnd().EndsWith(';'))
                {
                    var stmt = current.ToString().Trim();
                    current.Clear();

                    if (string.IsNullOrWhiteSpace(stmt))
                        continue;

                    // Ignorar sentencias de transacción y pragma
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
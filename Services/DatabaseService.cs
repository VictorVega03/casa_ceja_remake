using CasaCejaRemake.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace CasaCejaRemake.Data
{
    
    /// Servicio principal para acceso a la base de datos SQLite
    /// Adaptado de LocaldataManager.cs del sistema original
    
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        private readonly string _preloadedDbPath;

        // Propiedad para saber si se usó catálogo precargado
        public bool IsCatalogPreloaded { get; private set; }

        public DatabaseService()
        {
            // Path de la BD principal (ApplicationData/CasaCeja/casaceja.db)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var casaCejaFolder = Path.Combine(appDataPath, "CasaCeja");
            _dbPath = Path.Combine(casaCejaFolder, "casaceja.db");

            // Path de la BD precargada (junto al ejecutable en Data/PreLoadedCatalog.db)
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            _preloadedDbPath = Path.Combine(basePath, "Data", "PreLoadedCatalog.db");
        }

        
        /// Inicializa la base de datos
        /// Si existe BD precargada, la copia. Si no, crea una nueva vacía.
        
        public async Task InitializeAsync()
        {
            // Si la BD ya existe, solo conectar
            if (File.Exists(_dbPath))
            {
                await ConnectToDatabaseAsync();
                await EnsureAllTablesExistAsync();
                IsCatalogPreloaded = await CheckIfCatalogPreloadedAsync();
                return;
            }

            // Si no existe, verificar si hay BD precargada
            if (File.Exists(_preloadedDbPath))
            {
                await UsePreloadedDatabaseAsync();
            }
            else
            {
                await CreateNewDatabaseAsync();
            }
        }

        
        /// Usa la base de datos precargada (con +7K productos)
        
        private async Task UsePreloadedDatabaseAsync()
        {
            try
            {
                // Crear directorio si no existe
                var directory = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Copiar BD precargada
                File.Copy(_preloadedDbPath, _dbPath);
                IsCatalogPreloaded = true;

                // Conectar y verificar tablas adicionales
                await ConnectToDatabaseAsync();
                await EnsureAllTablesExistAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al usar BD precargada: {ex.Message}");
                // Si falla, crear nueva BD vacía
                await CreateNewDatabaseAsync();
            }
        }

        
        /// Crea una base de datos nueva vacía
        
        private async Task CreateNewDatabaseAsync()
        {
            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            IsCatalogPreloaded = false;

            // Conectar (esto crea el archivo automáticamente)
            await ConnectToDatabaseAsync();

            // Crear todas las tablas
            await CreateAllTablesAsync();
        }

        
        /// Conecta a la base de datos
        
        private async Task ConnectToDatabaseAsync()
        {
            _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            
            // Habilitar foreign keys
            await _database.ExecuteAsync("PRAGMA foreign_keys = ON");
        }

        
        /// Crea todas las tablas del sistema
        
        private async Task CreateAllTablesAsync()
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            // Ejecutar en una transacción para que sea atómico
            await _database.RunInTransactionAsync((transaction) =>
            {
                // Tablas de sistema
                transaction.CreateTable<Role>();

                // Tablas principales
                transaction.CreateTable<User>();
                transaction.CreateTable<Branch>();
                transaction.CreateTable<Category>();
                transaction.CreateTable<Unit>();
                transaction.CreateTable<Product>();
                transaction.CreateTable<Customer>();
                transaction.CreateTable<Supplier>();

                // Tablas de transacciones
                transaction.CreateTable<Sale>();
                transaction.CreateTable<SaleProduct>();
                transaction.CreateTable<Credit>();
                transaction.CreateTable<CreditProduct>();
                transaction.CreateTable<CreditPayment>();
                transaction.CreateTable<Layaway>();
                transaction.CreateTable<LayawayProduct>();
                transaction.CreateTable<LayawayPayment>();

                // Tablas de inventario
                transaction.CreateTable<StockEntry>();
                transaction.CreateTable<EntryProduct>();
                transaction.CreateTable<StockOutput>();
                transaction.CreateTable<OutputProduct>();

                // Otras tablas
                transaction.CreateTable<CashClose>();
                transaction.CreateTable<CashMovement>();
            });

            Console.WriteLine("Todas las tablas creadas correctamente");
        }

        
        /// Verifica que todas las tablas necesarias existan
        /// Si faltan, las crea (útil para migraciones)
        
        private async Task EnsureAllTablesExistAsync()
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            // Verificar y crear tablas que falten
            await _database.CreateTableAsync<Role>();
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Branch>();
            await _database.CreateTableAsync<Category>();
            await _database.CreateTableAsync<Unit>();
            await _database.CreateTableAsync<Product>();
            await _database.CreateTableAsync<Customer>();
            await _database.CreateTableAsync<Supplier>();
            await _database.CreateTableAsync<Sale>();
            await _database.CreateTableAsync<SaleProduct>();
            await _database.CreateTableAsync<Credit>();
            await _database.CreateTableAsync<CreditProduct>();
            await _database.CreateTableAsync<CreditPayment>();
            await _database.CreateTableAsync<Layaway>();
            await _database.CreateTableAsync<LayawayProduct>();
            await _database.CreateTableAsync<LayawayPayment>();
            await _database.CreateTableAsync<StockEntry>();
            await _database.CreateTableAsync<EntryProduct>();
            await _database.CreateTableAsync<StockOutput>();
            await _database.CreateTableAsync<OutputProduct>();
            await _database.CreateTableAsync<CashClose>();
            await _database.CreateTableAsync<CashMovement>();

            Console.WriteLine("Verificación de tablas completada");
        }

        
        /// Verifica si la BD tiene el catálogo precargado
        
        private async Task<bool> CheckIfCatalogPreloadedAsync()
        {
            if (_database == null)
                return false;

            try
            {
                var count = await _database.Table<Product>().CountAsync();
                Console.WriteLine($"Productos en BD: {count}");
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        // ========== MÉTODOS DE ACCESO A DATOS ==========

        
        /// Obtiene una tabla para queries
        
        public AsyncTableQuery<T> Table<T>() where T : new()
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");

            return _database.Table<T>();
        }

        
        /// Inserta un registro
        
        public async Task<int> InsertAsync<T>(T entity)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.InsertAsync(entity);
        }

        
        /// Inserta múltiples registros
        
        public async Task<int> InsertAllAsync<T>(IEnumerable<T> entities)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.InsertAllAsync(entities);
        }

        
        /// Actualiza un registro
        
        public async Task<int> UpdateAsync<T>(T entity)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.UpdateAsync(entity);
        }

        
        /// Actualiza múltiples registros        
        public async Task<int> UpdateAllAsync<T>(IEnumerable<T> entities)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.UpdateAllAsync(entities);
        }

        
        /// Elimina un registro        
        public async Task<int> DeleteAsync<T>(T entity)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.DeleteAsync(entity);
        }

        
        /// Obtiene un registro por ID        
        public async Task<T?> GetAsync<T>(int id) where T : new()
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.GetAsync<T>(id);
        }

        
        /// Ejecuta una query SQL personalizada        
        public async Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.QueryAsync<T>(sql, args);
        }

        
        /// Ejecuta un comando SQL (INSERT, UPDATE, DELETE)        
        public async Task<int> ExecuteAsync(string sql, params object[] args)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.ExecuteAsync(sql, args);
        }

        /// <summary>
        /// Ejecuta una query SQL que retorna un valor escalar
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            return await _database.ExecuteScalarAsync<T>(sql, args);
        }

        
        /// Ejecuta operaciones en una transacción    
        public async Task RunInTransactionAsync(Action<SQLiteConnection> action)
        {
            if (_database == null)
                throw new InvalidOperationException("Database not initialized");

            await _database.RunInTransactionAsync(action);
        }

        
        /// Cierra la conexión a la base de datos        
        public async Task CloseAsync()
        {
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
            }
        }
    }
}
using System;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Data
{
    /// <summary>
    /// Maneja la inicialización de la base de datos con datos por defecto
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly DatabaseService _databaseService;

        public DatabaseInitializer(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Inicializa la BD con datos por defecto si es una BD nueva
        /// </summary>
        public async Task InitializeDefaultDataAsync()
        {
            // Si ya hay productos (BD precargada), no hacer nada
            if (_databaseService.IsCatalogPreloaded)
            {
                Console.WriteLine("✅ BD precargada detectada, omitiendo datos iniciales");
                return;
            }

            // Verificar si ya hay usuarios (para no duplicar datos)
            var userCount = await _databaseService.Table<User>().CountAsync();
            if (userCount > 0)
            {
                Console.WriteLine("✅ BD ya tiene datos, omitiendo inicialización");
                return;
            }

            Console.WriteLine("🔧 Inicializando BD con datos por defecto...");

            await CreateDefaultUserAsync();
            await CreateDefaultBranchAsync();
            await CreateDefaultUnitsAsync();
            await CreateDefaultCategoriesAsync();

            Console.WriteLine("✅ Datos por defecto creados correctamente");
        }

        /// <summary>
        /// Crea el usuario administrador por defecto
        /// </summary>
        private async Task CreateDefaultUserAsync()
        {
            var adminUser = new User
            {
                Username = "admin",
                Password = "admin", // ⚠️ En producción, usar hash
                Name = "Administrador",
                Email = "admin@casaceja.com",
                UserType = 1, // Nivel 1 = Administrador               
                Active = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _databaseService.InsertAsync(adminUser);
            Console.WriteLine("✅ Usuario admin creado");
        }

        /// <summary>
        /// Crea la sucursal principal por defecto
        /// </summary>
        private async Task CreateDefaultBranchAsync()
        {
            var mainBranch = new Branch
            {
                Name = "Casa Ceja - Sucursal Principal",
                Address = "Dirección por configurar",
                Email = "sucursal@casaceja.com",
                RazonSocial = "Casa Ceja S.A de C.V",
                Active = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _databaseService.InsertAsync(mainBranch);
            Console.WriteLine("Sucursal principal creada");
        }

        /// <summary>
        /// Crea unidades de medida por defecto
        /// </summary>
        private async Task CreateDefaultUnitsAsync()
        {
            var units = new[]
            {
                new Unit { Name = "Pieza", Active = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Unit { Name = "Caja", Active = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Unit { Name = "Paquete", Active = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Unit { Name = "Metro", Active = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Unit { Name = "Kilogramo", Active = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Unit { Name = "Litro", Active = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            };

            await _databaseService.InsertAllAsync(units);
            Console.WriteLine($" {units.Length} unidades de medida creadas");
        }

        /// <summary>
        /// Crea categorías por defecto
        /// </summary>
        private async Task CreateDefaultCategoriesAsync()
        {
            var categories = new[]
            {
                new Category { Name = "Papelería", Active = true, HasDiscount = false, Discount = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Category { Name = "Oficina", Active = true, HasDiscount = false, Discount = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Category { Name = "Escolar", Active = true, HasDiscount = false, Discount = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Category { Name = "Arte", Active = true, HasDiscount = false, Discount = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Category { Name = "Tecnología", Active = true, HasDiscount = false, Discount = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            };

            await _databaseService.InsertAllAsync(categories);
            Console.WriteLine($" {categories.Length} categorías creadas");
        }
    }
}
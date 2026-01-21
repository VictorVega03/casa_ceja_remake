using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using SQLite;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Implementación base del patrón Repository
    /// Usa DatabaseService y sqlite-net-pcl para acceso a datos
    /// Maneja automáticamente campos de auditoría (created_at, updated_at)
    /// </summary>
    /// <typeparam name="T">Tipo de entidad (debe tener atributos SQLite)</typeparam>
    public class BaseRepository<T> : IRepository<T> where T : new()
    {
        protected readonly DatabaseService _databaseService;
        private readonly PropertyInfo? _createdAtProperty;
        private readonly PropertyInfo? _updatedAtProperty;

        public BaseRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Detectar si la entidad tiene propiedades de auditoría
            var type = typeof(T);
            _createdAtProperty = type.GetProperty("CreatedAt");
            _updatedAtProperty = type.GetProperty("UpdatedAt");
        }


        // ============================================
        // CONSULTAS (READ)
        // ============================================

        public virtual async Task<List<T>> GetAllAsync()
        {
            try
            {
                var table = _databaseService.Table<T>();
                return await table.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting all {typeof(T).Name} records", ex);
            }
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            try
            {
                return await _databaseService.GetAsync<T>(id);
            }
            catch (Exception ex)
            {
                // sqlite-net-pcl lanza excepción si no encuentra el registro
                // Retornamos null para mantener la interfaz consistente
                if (ex.Message.Contains("not found"))
                    return default;
                
                throw new InvalidOperationException($"Error getting {typeof(T).Name} with ID {id}", ex);
            }
        }

        public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var table = _databaseService.Table<T>();
                return await table.Where(predicate).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error finding {typeof(T).Name} records", ex);
            }
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var table = _databaseService.Table<T>();
                return await table.Where(predicate).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error finding first {typeof(T).Name} record", ex);
            }
        }

        public virtual async Task<int> CountAsync()
        {
            try
            {
                var table = _databaseService.Table<T>();
                return await table.CountAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error counting {typeof(T).Name} records", ex);
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var table = _databaseService.Table<T>();
                return await table.CountAsync(predicate);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error counting {typeof(T).Name} records", ex);
            }
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var count = await CountAsync(predicate);
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error checking existence of {typeof(T).Name}", ex);
            }
        }


        // ============================================
        // CREACIÓN (CREATE)
        // ============================================

        public virtual async Task<int> AddAsync(T entity)
        {
            try
            {
                // Actualizar timestamps de auditoría
                SetCreatedAt(entity);
                SetUpdatedAt(entity);

                return await _databaseService.InsertAsync(entity);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error adding {typeof(T).Name}", ex);
            }
        }

        public virtual async Task<int> AddRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                var entityList = entities.ToList();

                // Actualizar timestamps en cada entidad
                foreach (var entity in entityList)
                {
                    SetCreatedAt(entity);
                    SetUpdatedAt(entity);
                }

                return await _databaseService.InsertAllAsync(entityList);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error adding multiple {typeof(T).Name} records", ex);
            }
        }


        // ============================================
        // ACTUALIZACIÓN (UPDATE)
        // ============================================

        public virtual async Task<int> UpdateAsync(T entity)
        {
            try
            {
                // Actualizar timestamp de modificación
                SetUpdatedAt(entity);

                return await _databaseService.UpdateAsync(entity);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating {typeof(T).Name}", ex);
            }
        }

        public virtual async Task<int> UpdateRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                var entityList = entities.ToList();

                // Actualizar timestamps en cada entidad
                foreach (var entity in entityList)
                {
                    SetUpdatedAt(entity);
                }

                return await _databaseService.UpdateAllAsync(entityList);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating multiple {typeof(T).Name} records", ex);
            }
        }


        // ============================================
        // ELIMINACIÓN (DELETE)
        // ============================================

        public virtual async Task<int> DeleteAsync(T entity)
        {
            try
            {
                return await _databaseService.DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting {typeof(T).Name}", ex);
            }
        }

        public virtual async Task<int> DeleteByIdAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                    return 0;

                return await DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting {typeof(T).Name} with ID {id}", ex);
            }
        }

        public virtual async Task<int> DeleteRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                int totalDeleted = 0;
                foreach (var entity in entities)
                {
                    totalDeleted += await DeleteAsync(entity);
                }
                return totalDeleted;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting multiple {typeof(T).Name} records", ex);
            }
        }

        public virtual async Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var entitiesToDelete = await FindAsync(predicate);
                return await DeleteRangeAsync(entitiesToDelete);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting {typeof(T).Name} records by condition", ex);
            }
        }


        // ============================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ============================================

        /// <summary>
        /// Establece el valor de CreatedAt si la propiedad existe
        /// </summary>
        private void SetCreatedAt(T entity)
        {
            if (_createdAtProperty != null && _createdAtProperty.CanWrite)
            {
                // Solo establecer si no tiene valor o es DateTime.MinValue
                var currentValue = (DateTime?)_createdAtProperty.GetValue(entity);
                if (!currentValue.HasValue || currentValue.Value == DateTime.MinValue)
                {
                    _createdAtProperty.SetValue(entity, DateTime.Now);
                }
            }
        }

        /// <summary>
        /// Establece el valor de UpdatedAt si la propiedad existe
        /// </summary>
        private void SetUpdatedAt(T entity)
        {
            if (_updatedAtProperty != null && _updatedAtProperty.CanWrite)
            {
                _updatedAtProperty.SetValue(entity, DateTime.Now);
            }
        }
    }
}
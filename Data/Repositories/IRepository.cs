using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Interfaz genérica para repositorios con operaciones CRUD
    /// Compatible con sqlite-net-pcl y todos los modelos del sistema
    /// </summary>
    /// <typeparam name="T">Tipo de entidad (debe tener atributos SQLite)</typeparam>
    public interface IRepository<T> where T : new()
    {
        // ============================================
        // CONSULTAS (READ)
        // ============================================

        /// <summary>
        /// Obtiene todos los registros de la tabla
        /// </summary>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Obtiene un registro por su ID
        /// </summary>
        /// <param name="id">ID del registro</param>
        /// <returns>Entidad encontrada o null</returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Busca registros que cumplan una condición
        /// </summary>
        /// <param name="predicate">Expresión lambda con la condición</param>
        /// <returns>Lista de entidades que cumplen la condición</returns>
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Obtiene el primer registro que cumpla una condición
        /// </summary>
        /// <param name="predicate">Expresión lambda con la condición</param>
        /// <returns>Primera entidad encontrada o null</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Cuenta el total de registros en la tabla
        /// </summary>
        Task<int> CountAsync();

        /// <summary>
        /// Cuenta registros que cumplen una condición
        /// </summary>
        /// <param name="predicate">Expresión lambda con la condición</param>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Verifica si existe algún registro que cumpla la condición
        /// </summary>
        /// <param name="predicate">Expresión lambda con la condición</param>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);


        // ============================================
        // CREACIÓN (CREATE)
        // ============================================

        /// <summary>
        /// Agrega un nuevo registro a la tabla
        /// Actualiza automáticamente created_at y updated_at si existen
        /// </summary>
        /// <param name="entity">Entidad a insertar</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> AddAsync(T entity);

        /// <summary>
        /// Agrega múltiples registros en una transacción
        /// </summary>
        /// <param name="entities">Colección de entidades a insertar</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> AddRangeAsync(IEnumerable<T> entities);


        // ============================================
        // ACTUALIZACIÓN (UPDATE)
        // ============================================

        /// <summary>
        /// Actualiza un registro existente
        /// Actualiza automáticamente updated_at si existe
        /// </summary>
        /// <param name="entity">Entidad con los datos actualizados</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> UpdateAsync(T entity);

        /// <summary>
        /// Actualiza múltiples registros en una transacción
        /// </summary>
        /// <param name="entities">Colección de entidades a actualizar</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> UpdateRangeAsync(IEnumerable<T> entities);


        // ============================================
        // ELIMINACIÓN (DELETE)
        // ============================================

        /// <summary>
        /// Elimina un registro de la tabla
        /// </summary>
        /// <param name="entity">Entidad a eliminar</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> DeleteAsync(T entity);

        /// <summary>
        /// Elimina un registro por su ID
        /// </summary>
        /// <param name="id">ID del registro a eliminar</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> DeleteByIdAsync(int id);

        /// <summary>
        /// Elimina múltiples registros en una transacción
        /// </summary>
        /// <param name="entities">Colección de entidades a eliminar</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Elimina todos los registros que cumplan una condición
        /// </summary>
        /// <param name="predicate">Expresión lambda con la condición</param>
        /// <returns>Número de filas afectadas</returns>
        Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate);
    }
}
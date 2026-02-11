using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    /// <summary>
    /// Modelo de rol de usuario.
    /// Los roles se definen en la base de datos, no como constantes estáticas.
    /// Permite agregar nuevos roles sin modificar código.
    /// </summary>
    [Table("roles")]
    public class Role
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Nombre del rol (ej: "Admin", "Cajero")</summary>
        [Column("name")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Clave única del rol para uso interno (ej: "admin", "cashier").
        /// Se usa en el código para comparaciones sin depender del Id numérico.
        /// </summary>
        [Column("key")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Role_Key", Unique = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de acceso: menor número = mayor acceso.
        /// Admin = 1 (acceso total), Cajero = 2 (acceso restringido).
        /// </summary>
        [Column("access_level")]
        public int AccessLevel { get; set; }

        /// <summary>Descripción del rol</summary>
        [Column("description")]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

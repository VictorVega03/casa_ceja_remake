using Microsoft.Extensions.DependencyInjection;

namespace CasaCejaRemake.Infrastructure;

/// <summary>
/// Métodos de extensión para registrar los servicios de la aplicación en el contenedor DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra todos los servicios de Casa Ceja en el contenedor de inyección de dependencias.
    /// </summary>
    /// <param name="services">La colección de servicios a configurar.</param>
    /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenamiento.</returns>
    public static IServiceCollection AddCasaCejaServices(this IServiceCollection services)
    {
        // Se registrarán servicios en Fase 3
        return services;
    }
}

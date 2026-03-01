using System;
using Microsoft.Extensions.DependencyInjection;

namespace CasaCejaRemake.Infrastructure;

/// <summary>
/// Proveedor de servicios centralizado para acceso global al contenedor DI.
/// Permite resolver servicios en contextos donde la inyección por constructor no es posible
/// (por ejemplo, en arranque de la aplicación o inicialización de Avalonia).
/// </summary>
public static class AppServiceProvider
{
    /// <summary>
    /// Proveedor de servicios configurado durante el arranque de la aplicación.
    /// Es <c>null</c> hasta que se llame a <see cref="Initialize"/>.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Inicializa el proveedor con el contenedor DI construido al arrancar la aplicación.
    /// </summary>
    /// <param name="provider">El <see cref="IServiceProvider"/> construido por el host.</param>
    public static void Initialize(IServiceProvider provider)
    {
        Services = provider;
    }

    /// <summary>
    /// Resuelve un servicio requerido del contenedor DI.
    /// Lanza una excepción si el servicio no está registrado o el contenedor no fue inicializado.
    /// </summary>
    /// <typeparam name="T">Tipo del servicio a resolver.</typeparam>
    /// <returns>La instancia del servicio registrado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el contenedor no fue inicializado o el servicio no está registrado.
    /// </exception>
    public static T GetRequiredService<T>() where T : notnull
    {
        if (Services is null)
            throw new InvalidOperationException(
                "AppServiceProvider no ha sido inicializado. Llama a Initialize() al arrancar la aplicación.");

        return Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Intenta resolver un servicio opcional del contenedor DI.
    /// Retorna <c>null</c> si el servicio no está registrado o el contenedor no fue inicializado.
    /// </summary>
    /// <typeparam name="T">Tipo del servicio a resolver.</typeparam>
    /// <returns>La instancia del servicio, o <c>null</c> si no está disponible.</returns>
    public static T? GetService<T>() where T : class
    {
        return Services?.GetService<T>();
    }
}

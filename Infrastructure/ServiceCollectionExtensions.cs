using System;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CasaCejaRemake.Infrastructure;

/// <summary>
/// Métodos de extensión para registrar los servicios de la aplicación en el contenedor DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra todos los repos y servicios de Casa Ceja en el contenedor DI.
    /// Los Services internamente siguen creando repos con "new" (Fase 4 los migrará).
    /// </summary>
    public static IServiceCollection AddCasaCejaServices(this IServiceCollection services)
    {
        // ═══════════════════════════════════════════════════════════
        // INFRAESTRUCTURA
        // ═══════════════════════════════════════════════════════════
        services.AddSingleton<DatabaseService>();

        // IRepository<T> genérico — requerido por AuthService y UserService
        services.AddTransient<IRepository<User>>(sp =>
            new BaseRepository<User>(sp.GetRequiredService<DatabaseService>()));

        // ═══════════════════════════════════════════════════════════
        // REPOSITORIOS ESPECÍFICOS (Transient — nueva instancia por uso)
        // ═══════════════════════════════════════════════════════════
        services.AddTransient<ISaleRepository>(sp =>
            new SaleRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ISaleProductRepository>(sp =>
            new SaleProductRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<IProductRepository>(sp =>
            new ProductRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICashCloseRepository>(sp =>
            new CashCloseRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICashMovementRepository>(sp =>
            new CashMovementRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICreditRepository>(sp =>
            new CreditRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICreditPaymentRepository>(sp =>
            new CreditPaymentRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ILayawayRepository>(sp =>
            new LayawayRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ILayawayPaymentRepository>(sp =>
            new LayawayPaymentRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICustomerRepository>(sp =>
            new CustomerRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<IUserRepository>(sp =>
            new UserRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<IBranchRepository>(sp =>
            new BranchRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICategoryRepository>(sp =>
            new CategoryRepository(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<IUnitRepository>(sp =>
            new UnitRepository(sp.GetRequiredService<DatabaseService>()));

        // ═══════════════════════════════════════════════════════════
        // SERVICIOS SINGLETON (una instancia para toda la app)
        // ═══════════════════════════════════════════════════════════

        // ConfigService — sin dependencias
        services.AddSingleton<IConfigService, ConfigService>();

        // RoleService — requiere DatabaseService
        services.AddSingleton<IRoleService, RoleService>();

        // AuthService — requiere IRepository<User> + RoleService
        // Como RoleService es Singleton, se pide directamente
        services.AddSingleton<IAuthService>(sp => new AuthService(
            sp.GetRequiredService<IRepository<User>>(),
            (RoleService)sp.GetRequiredService<IRoleService>()));

        // UserService — misma firma que AuthService
        services.AddSingleton<IUserService>(sp => new UserService(
            sp.GetRequiredService<IRepository<User>>(),
            (RoleService)sp.GetRequiredService<IRoleService>()));

        // PrintService — requiere ConfigService concreto (no interfaz aún)
        services.AddSingleton<IPrintService>(sp => new PrintService(
            (ConfigService)sp.GetRequiredService<IConfigService>()));

        // ExportService — sin dependencias
        services.AddSingleton<IExportService, ExportService>();

        // FolioService — requiere DatabaseService
        services.AddSingleton<IFolioService, FolioService>();

        // CartService — sin dependencias
        services.AddSingleton<ICartService, CartService>();

        // TicketService — sin dependencias
        services.AddSingleton<ITicketService, TicketService>();

        // ═══════════════════════════════════════════════════════════
        // SERVICIOS DE NEGOCIO (Transient — nueva instancia por request)
        // Internamente siguen creando repos con "new" hasta Fase 4.
        // ═══════════════════════════════════════════════════════════
        services.AddTransient<ISalesService>(sp =>
            new SalesService(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICashCloseService>(sp =>
            new CashCloseService(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICreditService>(sp =>
            new CreditService(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ILayawayService>(sp =>
            new LayawayService(sp.GetRequiredService<DatabaseService>()));

        services.AddTransient<ICustomerService>(sp =>
            new CustomerService(sp.GetRequiredService<DatabaseService>()));

        return services;
    }
}

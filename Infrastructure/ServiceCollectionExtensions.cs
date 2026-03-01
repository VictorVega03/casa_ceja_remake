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
    /// Fase 4 completa: todos los Services reciben sus dependencias por constructor.
    /// </summary>
    public static IServiceCollection AddCasaCejaServices(this IServiceCollection services)
    {
        // ═══════════════════════════════════════════════════════════
        // INFRAESTRUCTURA
        // ═══════════════════════════════════════════════════════════
        services.AddSingleton<DatabaseService>();

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

        // PrintService — requiere ConfigService concreto
        services.AddSingleton<IPrintService>(sp => new PrintService(
            (ConfigService)sp.GetRequiredService<IConfigService>()));

        // ExportService — sin dependencias
        services.AddSingleton<IExportService, ExportService>();

        // TicketService — sin dependencias
        services.AddSingleton<ITicketService, TicketService>();

        // CartService — sin dependencias
        services.AddSingleton<ICartService, CartService>();

        // FolioService — requiere 6 repos + DatabaseService (para ExisteFolioAsync raw SQL)
        services.AddSingleton<IFolioService>(sp => new FolioService(
            sp.GetRequiredService<ICashCloseRepository>(),
            sp.GetRequiredService<ISaleRepository>(),
            sp.GetRequiredService<ICreditRepository>(),
            sp.GetRequiredService<ILayawayRepository>(),
            sp.GetRequiredService<ICreditPaymentRepository>(),
            sp.GetRequiredService<ILayawayPaymentRepository>(),
            sp.GetRequiredService<DatabaseService>()));

        // AuthService — requiere IUserRepository + IRoleService
        services.AddSingleton<IAuthService>(sp => new AuthService(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<IRoleService>()));

        // UserService — misma firma que AuthService
        services.AddSingleton<IUserService>(sp => new UserService(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<IRoleService>()));

        // ═══════════════════════════════════════════════════════════
        // SERVICIOS DE NEGOCIO (Transient — nueva instancia por request)
        // Fase 4: todos los Services reciben sus dependencias por constructor.
        // ═══════════════════════════════════════════════════════════

        services.AddTransient<ICustomerService>(sp => new CustomerService(
            sp.GetRequiredService<ICustomerRepository>()));

        services.AddTransient<ISalesService>(sp => new SalesService(
            sp.GetRequiredService<ISaleRepository>(),
            sp.GetRequiredService<ISaleProductRepository>(),
            sp.GetRequiredService<IProductRepository>(),
            sp.GetRequiredService<IBranchRepository>(),
            sp.GetRequiredService<ICategoryRepository>(),
            sp.GetRequiredService<IUnitRepository>(),
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<ITicketService>(),
            sp.GetRequiredService<PricingService>(),
            sp.GetRequiredService<IFolioService>(),
            sp.GetRequiredService<IConfigService>()));

        services.AddTransient<ICashCloseService>(sp => new CashCloseService(
            sp.GetRequiredService<ICashCloseRepository>(),
            sp.GetRequiredService<ICashMovementRepository>(),
            sp.GetRequiredService<ISaleRepository>(),
            sp.GetRequiredService<ICreditRepository>(),
            sp.GetRequiredService<ILayawayRepository>(),
            sp.GetRequiredService<ILayawayPaymentRepository>(),
            sp.GetRequiredService<ICreditPaymentRepository>(),
            sp.GetRequiredService<IFolioService>(),
            sp.GetRequiredService<IConfigService>()));

        services.AddTransient<ICreditService>(sp => new CreditService(
            sp.GetRequiredService<ICreditRepository>(),
            sp.GetRequiredService<DatabaseService>(),
            sp.GetRequiredService<ICreditPaymentRepository>(),
            sp.GetRequiredService<ICustomerRepository>(),
            sp.GetRequiredService<IBranchRepository>(),
            sp.GetRequiredService<ITicketService>(),
            sp.GetRequiredService<IFolioService>(),
            sp.GetRequiredService<IConfigService>()));

        services.AddTransient<ILayawayService>(sp => new LayawayService(
            sp.GetRequiredService<ILayawayRepository>(),
            sp.GetRequiredService<DatabaseService>(),
            sp.GetRequiredService<ILayawayPaymentRepository>(),
            sp.GetRequiredService<ICustomerRepository>(),
            sp.GetRequiredService<IBranchRepository>(),
            sp.GetRequiredService<ITicketService>(),
            sp.GetRequiredService<IFolioService>(),
            sp.GetRequiredService<IConfigService>()));

        return services;
    }
}

# Plan de Refactorización Arquitectónica — Casa Ceja POS
## Diseñado para ejecución con Claude Code (agente IA)

**Fecha:** 28 de Febrero, 2026  
**Repositorio:** `https://github.com/VictorVega03/casa_ceja_remake`  
**Documento de referencia:** `ANALISIS_ARQUITECTURA_POS_COMPLETO.md`

---

## Reglas Globales (aplican a TODAS las fases)

Estas reglas deben incluirse como contexto en cada prompt:

```
REGLAS OBLIGATORIAS PARA TODA LA REFACTORIZACIÓN:
1. Framework: .NET 8, Avalonia 11.3.0, SQLite (sqlite-net-pcl), CommunityToolkit.Mvvm 8.3.2
2. Desarrollo en macOS, target Windows 10+
3. NO modificar las vistas .axaml (diseño visual) a menos que se indique explícitamente
4. El proyecto DEBE compilar sin errores al terminar cada fase
5. NO eliminar funcionalidad existente — solo reorganizar y desacoplar
6. Mantener TODOS los namespaces existentes cuando sea posible
7. Cada archivo nuevo debe tener su namespace correcto según la carpeta
8. NO crear proyectos adicionales — todo en el mismo proyecto casa_ceja_remake
9. Usar inyección de dependencias vía constructor — nunca Service Locator
10. NO usar Entity Framework — seguimos con sqlite-net-pcl
11. Antes de empezar, hacer git clone del repositorio y verificar que compila
```

---

## Orden de Ejecución

```
FASE 0 → FASE 1 → FASE 2 → FASE 3 → FASE 4 → FASE 5 → FASE 6 → FASE 7
  DI       Repos    IService  Registrar  Refact.   Refact.   Refact.   DTOs
Container  Espec.   Intfcs    en DI      Services  ViewModels Views    (futuro)
```

Cada fase depende de la anterior. No saltar fases.

---

## FASE 0 — Infraestructura de Inyección de Dependencias

### Objetivo
Instalar el contenedor DI de Microsoft y crear la clase ServiceProvider que reemplazará los singletons estáticos de `App.axaml.cs`. Al terminar, el contenedor existe pero AÚN no se usa — solo se prepara la infraestructura.

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando el sistema POS "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite). 
Actualmente los servicios se crean con "new" en App.axaml.cs y se guardan como propiedades estáticas.
Necesito implementar inyección de dependencias para desacoplar el sistema.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 0: Crear infraestructura de DI

PASO 1: Instalar paquete NuGet
- Agregar al .csproj: Microsoft.Extensions.DependencyInjection (versión 8.x compatible con .NET 8)

PASO 2: Crear archivo Infrastructure/ServiceCollectionExtensions.cs
- Namespace: CasaCejaRemake.Infrastructure
- Clase estática con método de extensión: 
  public static IServiceCollection AddCasaCejaServices(this IServiceCollection services)
- Por ahora, el método solo retorna services (cuerpo vacío) — se llenará en Fase 3
- Agregar comentario: "// Se registrarán servicios en Fase 3"

PASO 3: Crear archivo Infrastructure/AppServiceProvider.cs  
- Namespace: CasaCejaRemake.Infrastructure
- Clase estática AppServiceProvider con:
  - Propiedad: public static IServiceProvider? Services { get; private set; }
  - Método: public static void Initialize(IServiceProvider provider)
  - Método genérico: public static T GetRequiredService<T>() where T : notnull
  - Método genérico: public static T? GetService<T>() where T : class

REGLAS:
- NO modificar App.axaml.cs todavía
- NO modificar ningún servicio existente
- NO modificar ninguna vista ni viewmodel
- Solo crear archivos nuevos en la carpeta Infrastructure/
- El proyecto debe compilar sin errores al terminar
- Hacer commit con mensaje: "feat: add DI container infrastructure (Phase 0)"

VERIFICACIÓN:
- dotnet build debe pasar sin errores
- Los archivos Infrastructure/ServiceCollectionExtensions.cs y Infrastructure/AppServiceProvider.cs existen
- La carpeta Infrastructure/ está creada
```

### Archivos a crear
```
Infrastructure/
├── AppServiceProvider.cs
└── ServiceCollectionExtensions.cs
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] Carpeta `Infrastructure/` con 2 archivos
- [ ] NuGet `Microsoft.Extensions.DependencyInjection` en .csproj

---

## FASE 1 — Interfaces e Implementaciones de Repositorios Específicos

### Objetivo
Crear interfaces para cada repositorio que necesite consultas especializadas, e implementar los repositorios con queries que actualmente están mezcladas en los Services. Los repositorios genéricos `BaseRepository<T>` se seguirán usando para operaciones CRUD simples.

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite).
Ya existe IRepository<T> y BaseRepository<T> funcionales.
Los 13 repositorios específicos en Data/Repositories/ están VACÍOS (0 líneas).
Actualmente los Services hacen queries complejas internamente (filtrado en memoria, 
GetAllAsync + LINQ, creación de repos ad-hoc). Necesito mover esas queries a repositorios.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake  
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 1: Crear interfaces y repositorios específicos

REGLAS CRÍTICAS:
- Cada interfaz HEREDA de IRepository<T> (para conservar CRUD genérico)
- Cada implementación HEREDA de BaseRepository<T> (reutilizar CRUD)
- Las interfaces van en Data/Repositories/Interfaces/
- Las implementaciones reemplazan los archivos vacíos en Data/Repositories/
- NO modificar IRepository.cs ni BaseRepository.cs
- NO modificar ningún Service todavía (eso es Fase 4)
- NO modificar ningún ViewModel ni View
- Usar los métodos de BaseRepository (FindAsync, GetAllAsync, etc.) donde sea posible
- Para queries complejas que necesiten SQL raw, usar _databaseService.QueryAsync o ExecuteScalarAsync
- Los métodos deben ser async/await
- El proyecto DEBE compilar al terminar

REPOSITORIOS A IMPLEMENTAR:

1. ISaleRepository / SaleRepository
   - GetByBranchAndDateRangeAsync(int branchId, DateTime start, DateTime end)
   - GetDailySalesAsync(int branchId, DateTime date)
   - GetPagedAsync(int branchId, int page, int pageSize, DateTime? start, DateTime? end)
   - CountByFiltersAsync(int branchId, DateTime? start, DateTime? end)
   - GetSalesSinceAsync(DateTime since)

2. ISaleProductRepository / SaleProductRepository
   - GetBySaleIdAsync(int saleId)

3. IProductRepository / ProductRepository
   - SearchAsync(string term, int? categoryId, int? unitId, bool onlyActive = true)
   - GetByBarcodeAsync(string barcode)
   - GetActiveCountAsync()

4. ICashCloseRepository / CashCloseRepository
   - GetOpenAsync(int branchId)
   - GetHistoryAsync(int branchId, int limit)
   - GetByDateRangeAsync(int branchId, DateTime start, DateTime end)

5. ICashMovementRepository / CashMovementRepository
   - GetByCashCloseIdAsync(int cashCloseId)

6. ICreditRepository / CreditRepository
   - GetPendingByCustomerAsync(int customerId)
   - GetPendingByBranchAsync(int branchId)
   - SearchAsync(int? customerId, int? status, int branchId)
   - GetByFolioAsync(string folio)

7. ICreditPaymentRepository / CreditPaymentRepository
   - GetByCreditIdAsync(int creditId)
   - GetPaymentsSinceAsync(DateTime since)

8. ILayawayRepository / LayawayRepository
   - GetPendingByCustomerAsync(int customerId)
   - GetPendingByBranchAsync(int branchId)
   - SearchAsync(int? customerId, int? status, int branchId)
   - GetByFolioAsync(string folio)

9. ILayawayPaymentRepository / LayawayPaymentRepository
   - GetByLayawayIdAsync(int layawayId)
   - GetPaymentsSinceAsync(DateTime since)

10. ICustomerRepository / CustomerRepository
    - SearchByTermAsync(string term)
    - GetByPhoneAsync(string phone)
    - ExistsByPhoneAsync(string phone)
    - GetAllActiveAsync()
    - CountActiveAsync()

11. IUserRepository / UserRepository
    - GetByUsernameAsync(string username)
    - GetCashiersAsync()
    - IsUsernameAvailableAsync(string username, int? excludeUserId)

12. IBranchRepository / BranchRepository
    - GetActiveAsync()

13. ICategoryRepository / CategoryRepository
    - GetActiveAsync()

14. IUnitRepository / UnitRepository
    - GetActiveAsync()

PARA CADA REPOSITORIO, seguir este patrón:

// Interfaz (Data/Repositories/Interfaces/ISaleRepository.cs)
namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<List<Sale>> GetDailySalesAsync(int branchId, DateTime date);
        // ... métodos especializados
    }
}

// Implementación (Data/Repositories/SaleRepository.cs)
namespace CasaCejaRemake.Data.Repositories
{
    public class SaleRepository : BaseRepository<Sale>, ISaleRepository
    {
        public SaleRepository(DatabaseService databaseService) : base(databaseService) { }
        
        public async Task<List<Sale>> GetDailySalesAsync(int branchId, DateTime date)
        {
            return await FindAsync(s => s.BranchId == branchId && s.CreatedAt >= date);
        }
    }
}

NOTA SOBRE QUERIES:
- Revisar los Services actuales para extraer las queries que hoy hacen ellos.
- Ejemplo: SalesService.SearchProductsAsync hace GetAllAsync + filtro en C#.
  Eso debe moverse a ProductRepository.SearchAsync usando FindAsync con predicado.
- Ejemplo: CashCloseService.CalculateTotalsAsync hace GetAllAsync de 5 tablas.
  Los repos deben ofrecer métodos filtrados (GetSalesSinceAsync, etc.)

VERIFICACIÓN:
- dotnet build sin errores
- Carpeta Data/Repositories/Interfaces/ con 14 interfaces
- 14 archivos de repositorio en Data/Repositories/ con implementaciones (no vacíos)
- Ningún Service, ViewModel o View fue modificado
- Hacer commit: "feat: implement repository interfaces and specific repositories (Phase 1)"
```

### Estructura resultante
```
Data/Repositories/
├── Interfaces/
│   ├── ISaleRepository.cs
│   ├── ISaleProductRepository.cs
│   ├── IProductRepository.cs
│   ├── ICashCloseRepository.cs
│   ├── ICashMovementRepository.cs
│   ├── ICreditRepository.cs
│   ├── ICreditPaymentRepository.cs
│   ├── ILayawayRepository.cs
│   ├── ILayawayPaymentRepository.cs
│   ├── ICustomerRepository.cs
│   ├── IUserRepository.cs
│   ├── IBranchRepository.cs
│   ├── ICategoryRepository.cs
│   └── IUnitRepository.cs
├── IRepository.cs          (sin cambios)
├── BaseRepository.cs       (sin cambios)
├── SaleRepository.cs       (implementado)
├── SaleProductRepository.cs
├── ProductRepository.cs
├── CashCloseRepository.cs
├── CashMovementRepository.cs
├── CreditRepository.cs
├── CreditPaymentRepository.cs
├── LayawayRepository.cs
├── LayawayPaymentRepository.cs
├── CustomerRepository.cs
├── UserRepository.cs
├── BranchRepository.cs
├── CategoryRepository.cs
├── UnitRepository.cs
└── StockEntry/OutputRepository.cs (opcional, vacíos ok por ahora)
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] 14 interfaces en `Data/Repositories/Interfaces/`
- [ ] 14 repositorios implementados (no vacíos)
- [ ] Ningún Service, ViewModel o View fue modificado

---

## FASE 2 — Interfaces de Servicios

### Objetivo
Crear interfaces para todos los Services del POS. Esto permite que ViewModels dependan de abstracciones y facilita la inyección de dependencias. También separar las clases Result en archivos propios.

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite).
En la Fase 1 se crearon interfaces de repositorios. Ahora necesito interfaces para los Services.
Actualmente ningún Service tiene interfaz — los ViewModels dependen de clases concretas.
También hay clases Result (SaleResult, CashCloseResult, etc.) definidas dentro de archivos de servicio
que necesitan moverse a su propia ubicación.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 2: Crear interfaces de servicios y extraer clases Result

PARTE A — Extraer clases Result a Models/Results/

Mover estas clases de los archivos de Service a archivos propios:
1. SaleResult (de SalesService.cs) → Models/Results/SaleResult.cs
2. StockValidationResult (de SalesService.cs) → Models/Results/StockValidationResult.cs
3. CashCloseResult (de CashCloseService.cs) → Models/Results/CashCloseResult.cs
4. CashMovementResult (de CashCloseService.cs) → Models/Results/CashMovementResult.cs
5. CashCloseTotals (de CashCloseService.cs) → Models/Results/CashCloseTotals.cs
6. PrintResult, PrintFailReason (de PrintService.cs) → Models/Results/PrintResult.cs
7. ExportResult (de ExportService.cs) → Models/Results/ExportResult.cs

Namespace: CasaCejaRemake.Models.Results
IMPORTANTE: Agregar "using CasaCejaRemake.Models.Results;" en los archivos originales
para que sigan compilando. NO eliminar las definiciones originales todavía — solo crear las nuevas.
Luego, eliminar las definiciones originales de los archivos de Service y verificar que compila.

PARTE B — Crear interfaces de servicios en Services/Interfaces/

Para cada Service, crear una interfaz que exponga SOLO los métodos públicos.
Revisar la API pública actual de cada Service y extraerla a la interfaz.

1. ISalesService (de SalesService)
   Incluir: ProcessSaleAsync, ProcessSaleWithMixedPaymentAsync, SearchProductsAsync,
   GetProductByCodeAsync, CreateCartItemAsync, CreateCartItemWithPriceTypeAsync,
   ApplySpecialPriceAsync, ApplyDealerPriceAsync, RevertToRetailPriceAsync,
   GetProductByIdAsync, GetDailySalesAsync, GetDailySalesTotalAsync,
   GetCategoriesAsync, GetUnitsAsync, SearchProductsWithUnitAsync,
   GetSalesHistoryPagedAsync, GetSalesCountAsync, GetSaleProductsAsync,
   GetUserNameAsync, RecoverTicketAsync, ValidateStockAsync, GetNextConsecutiveAsync

2. ICashCloseService (de CashCloseService)
   Incluir: OpenCashAsync, GetOpenCashAsync, CalculateTotalsAsync,
   CloseCashAsync, AddMovementAsync, GetMovementsAsync, GetHistoryAsync

3. ICreditService (de CreditService)
   Incluir: CreateCreditAsync, GetPendingByCustomerAsync, GetPendingByBranchAsync,
   SearchAsync, GetByIdAsync, GetByFolioAsync, GetProductsAsync, GetPaymentsAsync,
   AddPaymentAsync, AddPaymentWithMixedAsync, UpdateStatusAsync, RecoverTicketAsync

4. ILayawayService (de LayawayService)
   Incluir: CreateLayawayAsync, GetPendingByCustomerAsync, GetPendingByBranchAsync,
   SearchAsync, GetByIdAsync, GetByFolioAsync, GetProductsAsync, GetPaymentsAsync,
   AddPaymentAsync, AddPaymentWithMixedAsync, MarkAsDeliveredAsync, 
   UpdateStatusAsync, RecoverTicketAsync

5. ICustomerService (de CustomerService)
   Incluir: SearchAsync, GetByIdAsync, GetByPhoneAsync, ExistsByPhoneAsync,
   CreateAsync, UpdateAsync, DeactivateAsync, GetAllActiveAsync, CountActiveAsync

6. IAuthService (de AuthService)
   Incluir: LoginAsync, Logout, SetCurrentUser, SetCurrentBranch, 
   HasAccessLevel, GetCurrentRoleName, GetUserCashRegisterId, HasBranchAccess
   Propiedades: CurrentUser, IsAuthenticated, IsAdmin, IsCajero, 
   CurrentUserName, CurrentBranchId
   Eventos: UserLoggedIn, UserLoggedOut

7. IConfigService (de ConfigService)
   Incluir: LoadAsync, SaveAppConfigAsync, SavePosTerminalConfigAsync,
   UpdateAppConfigAsync, UpdatePosTerminalConfigAsync
   Propiedades: AppConfig, PosTerminalConfig
   Eventos: AppConfigChanged, PosTerminalConfigChanged

8. IPrintService (de PrintService)
   Incluir: GetAvailablePrinters, PrintAsync, PrintThermalAsync,
   PrintLetterAsync, PrintSaleTicketAsync, PrintCashCloseTicketAsync

9. IFolioService (de FolioService)
   Incluir: GenerarFolioVentaAsync, GenerarFolioApartadoAsync,
   GenerarFolioCreditoAsync, GenerarFolioPagoAsync, GenerarFolioCorteAsync,
   ParsearFolio

10. IUserService (de UserService)
    Incluir: GetAllUsersAsync, GetCashiersAsync, GetByIdAsync,
    CreateUserAsync, UpdateUserAsync, DeactivateUserAsync,
    IsUsernameAvailableAsync, GetAvailableRoles, GetCashierRoleId,
    IsAdminAsync, AuthenticateAsync

11. IRoleService (de RoleService)
    Incluir: LoadRolesAsync, GetByKey, GetById, GetAdminRoleId,
    GetCashierRoleId, IsAdminRole, IsCashierRole, GetAccessLevel,
    HasAccessLevel, GetRoleName, GetAllRoles
    Propiedad: Roles

12. IExportService (de ExportService)
    Incluir: ExportToExcelAsync, ExportMultiSheetAsync, ExportSimpleAsync

13. ITicketService (de TicketService)
    Incluir: GenerateTicket, GenerateTicketWithMixedPayment, SerializeTicket,
    DeserializeTicket, GenerateTicketText (todas las sobrecargas),
    GeneratePaymentTicketText, GenerateReprintWithHistoryText,
    GenerateHistoryTicketText, GenerateCashCloseTicketText

14. ICartService (de CartService)
    Incluir todas las propiedades y métodos públicos de CartService

PARTE C — Hacer que cada Service implemente su interfaz

Modificar la declaración de cada Service para implementar la interfaz:
  public class SalesService : ISalesService
  public class CashCloseService : ICashCloseService
  etc.

REGLAS:
- NO cambiar la lógica interna de ningún Service
- NO cambiar los constructores de los Services
- NO modificar ViewModels ni Views
- Las interfaces van en Services/Interfaces/
- Las clases Result van en Models/Results/
- El proyecto DEBE compilar sin errores al terminar
- Hacer commit: "feat: create service interfaces and extract Result classes (Phase 2)"

VERIFICACIÓN:
- dotnet build sin errores
- Carpeta Services/Interfaces/ con 14 interfaces
- Carpeta Models/Results/ con 7 archivos de Result
- Cada Service implementa su interfaz (: ISalesService, etc.)
- Las clases Result ya no están definidas dentro de los Services
- Ningún ViewModel ni View fue modificado
```

### Estructura resultante
```
Services/Interfaces/
├── ISalesService.cs
├── ICashCloseService.cs
├── ICreditService.cs
├── ILayawayService.cs
├── ICustomerService.cs
├── IAuthService.cs
├── IConfigService.cs
├── IPrintService.cs
├── IFolioService.cs
├── IUserService.cs
├── IRoleService.cs
├── IExportService.cs
├── ITicketService.cs
└── ICartService.cs

Models/Results/
├── SaleResult.cs
├── StockValidationResult.cs
├── CashCloseResult.cs
├── CashMovementResult.cs
├── CashCloseTotals.cs
├── PrintResult.cs
└── ExportResult.cs
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] 14 interfaces de servicios
- [ ] 7 archivos de Result extraídos
- [ ] Cada Service implementa su interfaz
- [ ] Ningún ViewModel ni View modificado

---

## FASE 3 — Registrar Todo en el Contenedor DI y Migrar App.axaml.cs

### Objetivo
Registrar todos los repositorios y servicios en el contenedor DI. Migrar `App.axaml.cs` para que use el contenedor en lugar de `new` manual y propiedades estáticas. Al final de esta fase, la aplicación funciona igual pero con DI real.

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite).
Fase 0: Creé Infrastructure/AppServiceProvider.cs y ServiceCollectionExtensions.cs
Fase 1: Creé interfaces e implementaciones de repositorios específicos
Fase 2: Creé interfaces de servicios, cada Service las implementa

Ahora necesito conectar todo: registrar en DI y migrar App.axaml.cs.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 3: Registrar dependencias y migrar App.axaml.cs

PASO 1: Llenar ServiceCollectionExtensions.cs

Registrar TODOS los repositorios y servicios con los lifetimes correctos:

// Singleton (una instancia para toda la app)
services.AddSingleton<DatabaseService>();
services.AddSingleton<IAuthService, AuthService>();
services.AddSingleton<IRoleService, RoleService>();
services.AddSingleton<IConfigService, ConfigService>();
services.AddSingleton<IPrintService, PrintService>();
services.AddSingleton<IExportService, ExportService>();
services.AddSingleton<IFolioService, FolioService>();
services.AddSingleton<IUserService, UserService>();
services.AddSingleton<ICartService, CartService>();
services.AddSingleton<ITicketService, TicketService>();
services.AddSingleton<IPricingService, PricingService>(); // si se crea interfaz

// Transient (nueva instancia cada vez — repos y servicios de negocio)
services.AddTransient<ISaleRepository, SaleRepository>();
services.AddTransient<ISaleProductRepository, SaleProductRepository>();
services.AddTransient<IProductRepository, ProductRepository>();
services.AddTransient<ICashCloseRepository, CashCloseRepository>();
services.AddTransient<ICashMovementRepository, CashMovementRepository>();
services.AddTransient<ICreditRepository, CreditRepository>();
services.AddTransient<ICreditPaymentRepository, CreditPaymentRepository>();
services.AddTransient<ILayawayRepository, LayawayRepository>();
services.AddTransient<ILayawayPaymentRepository, LayawayPaymentRepository>();
services.AddTransient<ICustomerRepository, CustomerRepository>();
services.AddTransient<IUserRepository, UserRepository>();
services.AddTransient<IBranchRepository, BranchRepository>();
services.AddTransient<ICategoryRepository, CategoryRepository>();
services.AddTransient<IUnitRepository, UnitRepository>();

services.AddTransient<ISalesService, SalesService>();
services.AddTransient<ICashCloseService, CashCloseService>();
services.AddTransient<ICreditService, CreditService>();
services.AddTransient<ILayawayService, LayawayService>();
services.AddTransient<ICustomerService, CustomerService>();

PASO 2: Modificar App.axaml.cs

El objetivo es que App.axaml.cs:
1. Configure el ServiceCollection y construya el ServiceProvider
2. Inicialice los servicios que necesitan inicialización async (DatabaseService, ConfigService, RoleService)
3. Use el provider para resolver servicios cuando cree ViewModels

IMPORTANTE — TRANSICIÓN GRADUAL:
Para no romper todo de golpe, en esta fase App.axaml.cs usará AMBOS:
- El nuevo ServiceProvider para resolver servicios
- Las propiedades estáticas existentes (App.DatabaseService, App.AuthService, etc.)
  PERO ahora apuntarán al mismo servicio resuelto del contenedor

Ejemplo de cómo debe quedar InitializeServicesAsync:

private async Task InitializeServicesAsync()
{
    var services = new ServiceCollection();
    services.AddCasaCejaServices();
    var provider = services.BuildServiceProvider();
    AppServiceProvider.Initialize(provider);

    // Inicializar servicios que necesitan async init
    var dbService = provider.GetRequiredService<DatabaseService>();
    await dbService.InitializeAsync();

    var initializer = new DatabaseInitializer(dbService);
    await initializer.InitializeDefaultDataAsync();

    var roleService = provider.GetRequiredService<IRoleService>();
    await roleService.LoadRolesAsync();

    var configService = provider.GetRequiredService<IConfigService>();
    await configService.LoadAsync();

    // COMPATIBILIDAD TEMPORAL: mantener propiedades estáticas apuntando a DI
    DatabaseService = dbService;
    AuthService = (AuthService)provider.GetRequiredService<IAuthService>();
    RoleService = (RoleService)provider.GetRequiredService<IRoleService>();
    ConfigService = (ConfigService)provider.GetRequiredService<IConfigService>();
    PrintService = (PrintService)provider.GetRequiredService<IPrintService>();
    ExportService = (ExportService)provider.GetRequiredService<IExportService>();
    FolioService = (FolioService)provider.GetRequiredService<IFolioService>();
    UserService = (UserService)provider.GetRequiredService<IUserService>();

    // POS services ahora vienen del contenedor
    _cartService = (CartService)provider.GetRequiredService<ICartService>();
    _salesService = (SalesService)provider.GetRequiredService<ISalesService>();
    _customerService = (CustomerService)provider.GetRequiredService<ICustomerService>();
    _creditService = (CreditService)provider.GetRequiredService<ICreditService>();
    _layawayService = (LayawayService)provider.GetRequiredService<ILayawayService>();
    _cashCloseService = (CashCloseService)provider.GetRequiredService<ICashCloseService>();
}

NOTA: Los Services aún crean repos internamente con "new BaseRepository<T>".
Eso se arreglará en Fase 4. En esta fase solo conectamos el contenedor DI
sin cambiar la lógica interna de los Services.

REGLAS:
- La aplicación DEBE funcionar igual que antes después de esta fase
- Mantener las propiedades estáticas de App como puente temporal
- NO cambiar constructores de Services todavía
- NO cambiar ViewModels ni Views
- El proyecto DEBE compilar Y ejecutar sin errores
- Hacer commit: "feat: register services in DI container and migrate App.axaml.cs (Phase 3)"

VERIFICACIÓN:
- dotnet build sin errores
- dotnet run debe mostrar la ventana de login correctamente
- Los servicios se resuelven desde el contenedor
- Las propiedades estáticas siguen funcionando (compatibilidad temporal)
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] `dotnet run` funciona — login, ventas, cortes siguen operando
- [ ] ServiceCollectionExtensions registra todos los servicios
- [ ] App.axaml.cs usa el contenedor DI

---

## FASE 4 — Refactorizar Services (Inyectar Repositorios, Eliminar App.*)

### Objetivo
Esta es la fase más importante y grande. Cada Service debe:
1. Recibir repositorios por constructor (interfaces, no concretos)
2. Recibir otros servicios por constructor (interfaces, no concretos)
3. Eliminar todo uso de `new BaseRepository<T>()`
4. Eliminar todo uso de `App.ConfigService`, `App.FolioService`, etc.
5. Eliminar repos ad-hoc creados dentro de métodos

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite).
Fases 0-3 completadas: DI container, repos con interfaces, services con interfaces,
todo registrado en DI. Los Services todavía crean BaseRepository<T> internamente
con "new" y acceden a App.* para obtener dependencias.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 4: Refactorizar constructores de Services para usar inyección

PRINCIPIO: Cada Service recibe TODAS sus dependencias vía constructor.
Nunca crea repositorios con "new". Nunca accede a App.*.

SERVICIO POR SERVICIO (hacer en este orden):

1. SalesService
   ANTES:
   public SalesService(DatabaseService databaseService)
   {
       _saleRepository = new BaseRepository<Sale>(databaseService);
       _ticketService = new TicketService();
       _pricingService = new PricingService();
       // ... y repos ad-hoc en métodos
   }
   
   DESPUÉS:
   public SalesService(
       ISaleRepository saleRepository,
       ISaleProductRepository saleProductRepository,
       IProductRepository productRepository,
       IBranchRepository branchRepository,
       ICategoryRepository categoryRepository,
       IUnitRepository unitRepository,
       IUserRepository userRepository,
       ITicketService ticketService,
       IPricingService pricingService,
       IFolioService folioService,
       IConfigService configService)
   {
       _saleRepository = saleRepository;
       _saleProductRepository = saleProductRepository;
       // ... todo inyectado
   }
   
   CAMBIOS INTERNOS:
   - Reemplazar _productRepository.GetAllAsync() + filtro manual 
     por _productRepository.SearchAsync() del nuevo repositorio
   - Reemplazar App.ConfigService?.PosTerminalConfig.TerminalId 
     por _configService.PosTerminalConfig.TerminalId
   - Reemplazar App.FolioService!.GenerarFolioVentaAsync 
     por _folioService.GenerarFolioVentaAsync
   - Eliminar TODOS los "new BaseRepository<Unit>(_databaseService)" ad-hoc
   - Eliminar el campo _databaseService (ya no se necesita)
   - Usar _unitRepository inyectado en vez de crear repos en cada método

2. CashCloseService
   DESPUÉS:
   public CashCloseService(
       ICashCloseRepository cashCloseRepository,
       ICashMovementRepository movementRepository,
       ISaleRepository saleRepository,
       ICreditRepository creditRepository,
       ILayawayRepository layawayRepository,
       ILayawayPaymentRepository layawayPaymentRepository,
       ICreditPaymentRepository creditPaymentRepository,
       IFolioService folioService,
       IConfigService configService)
   
   CAMBIOS INTERNOS:
   - En CalculateTotalsAsync: reemplazar GetAllAsync() de 5 tablas
     por métodos filtrados del repositorio (GetSalesSinceAsync, etc.)
   - Eliminar App.ConfigService y App.FolioService

3. CreditService
   DESPUÉS:
   public CreditService(
       ICreditRepository creditRepository,
       ICreditProductRepository creditProductRepository,
       ICreditPaymentRepository creditPaymentRepository,
       ICustomerRepository customerRepository,
       IBranchRepository branchRepository,
       ITicketService ticketService,
       IFolioService folioService,
       IConfigService configService)
   
   CAMBIOS INTERNOS:
   - Eliminar App.ConfigService y App.FolioService

4. LayawayService
   DESPUÉS:
   public LayawayService(
       ILayawayRepository layawayRepository,
       ILayawayProductRepository layawayProductRepository,
       ILayawayPaymentRepository layawayPaymentRepository,
       ICustomerRepository customerRepository,
       IBranchRepository branchRepository,
       ITicketService ticketService,
       IFolioService folioService,
       IConfigService configService)

5. CustomerService
   DESPUÉS:
   public CustomerService(ICustomerRepository customerRepository)

6. AuthService
   DESPUÉS:
   public AuthService(IUserRepository userRepository, IRoleService roleService)

7. UserService
   DESPUÉS:
   public UserService(IUserRepository userRepository, IRoleService roleService)

8. FolioService
   DESPUÉS:
   public FolioService(
       ICashCloseRepository cashCloseRepository,
       ISaleRepository saleRepository,
       ICreditRepository creditRepository,
       ILayawayRepository layawayRepository,
       ICreditPaymentRepository creditPaymentRepository,
       ILayawayPaymentRepository layawayPaymentRepository)
   - Eliminar _databaseService y todo SQL raw
   - Usar los repos para las consultas

9. RoleService
   DESPUÉS:
   public RoleService(DatabaseService databaseService)
   (RoleService puede seguir usando DatabaseService directamente 
   porque solo hace Table<Role>().ToListAsync() en la inicialización)

10. PrintService, ExportService, ConfigService, TicketService, PricingService, CartService
    — Estos no necesitan repos. Revisar si necesitan cambios menores.

DESPUÉS DE CAMBIAR LOS CONSTRUCTORES:
Actualizar ServiceCollectionExtensions.cs — el contenedor DI resolverá 
automáticamente las dependencias porque ya están registradas.

ACTUALIZAR App.axaml.cs:
Ya no necesita crear servicios con "new SalesService(DatabaseService)".
Los obtiene del contenedor que resuelve todo automáticamente.

REGLAS CRÍTICAS:
- NO cambiar la LÓGICA de negocio — solo las DEPENDENCIAS
- Si un Service usa App.ConfigService.PosTerminalConfig.TerminalId,
  reemplazar por _configService.PosTerminalConfig.TerminalId (misma lógica)
- Los campos deben ser de tipo INTERFAZ (ISaleRepository, no SaleRepository)
- NO modificar ViewModels ni Views todavía
- El proyecto DEBE compilar Y funcionar al terminar
- Hacer commit: "refactor: inject dependencies into all Services (Phase 4)"

VERIFICACIÓN:
- dotnet build sin errores
- dotnet run funciona — toda la funcionalidad POS sigue operando
- grep -rn "new BaseRepository" Services/ → CERO resultados
- grep -rn "App\." Services/ → CERO resultados (excepto namespace App)
- Todos los campos de repositorio son tipo interfaz (IXxxRepository)
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] `dotnet run` funciona — login, ventas, cortes operan
- [ ] CERO `new BaseRepository<T>` en Services/
- [ ] CERO `App.*` en Services/
- [ ] Todos los campos son interfaces

---

## FASE 5 — Refactorizar ViewModels

### Objetivo
Los ViewModels deben recibir interfaces de servicios, nunca concretos. Eliminar accesos a `DatabaseService`, `App.*`, y creación de servicios con `new`.

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite).
Fases 0-4 completadas: DI, repos, interfaces, services refactorizados.
Los ViewModels todavía reciben Services concretos y algunos acceden a DatabaseService.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 5: Refactorizar ViewModels para usar interfaces

CAMBIOS NECESARIOS:

1. SalesViewModel
   - Recibe ISalesService, ICartService, IAuthService (interfaces, no concretos)
   - Eliminar línea 173: new CashCloseService(App.DatabaseService!)

2. CashCloseHistoryViewModel
   - Recibe ICashCloseService, IAuthService, IUserRepository, IBranchRepository
   - ELIMINAR DatabaseService del constructor
   - ELIMINAR new BaseRepository<User> y new BaseRepository<Branch>
   - Usar _userRepository y _branchRepository inyectados

3. CashCloseViewModel
   - Recibe ICashCloseService, IAuthService (interfaces)

4. CashCloseDetailViewModel
   - Recibe ICashCloseService (interfaz)

5. CashMovementViewModel
   - Recibe ICashCloseService (interfaz)

6. AppConfigViewModel
   - Recibe IConfigService, IBranchRepository (interfaz)
   - ELIMINAR DatabaseService del constructor
   - Reemplazar _databaseService.Table<Branch>() por _branchRepository.GetActiveAsync()

7. Todos los demás ViewModels POS y Shared
   - Cambiar tipos concretos a interfaces en constructores
   - Ejemplo: SalesService → ISalesService, CustomerService → ICustomerService

PATRÓN PARA CADA VIEWMODEL:
   ANTES:
   public SalesViewModel(CartService cart, SalesService sales, AuthService auth, int branchId)
   
   DESPUÉS:
   public SalesViewModel(ICartService cart, ISalesService sales, IAuthService auth, int branchId)

REGLAS:
- NO cambiar lógica interna de ViewModels — solo tipos de dependencias
- Los ViewModels siguen recibiendo dependencias por constructor
- NO modificar Views (.axaml ni code-behind) todavía
- Si un ViewModel necesita un servicio que antes creaba con "new", agregarlo al constructor
- El proyecto DEBE compilar al terminar
- Hacer commit: "refactor: inject service interfaces into ViewModels (Phase 5)"

NOTA: Después de cambiar los constructores de ViewModels, los lugares que crean
ViewModels (App.axaml.cs y Views code-behind) necesitarán ajustarse.
Por ahora, actualizar SOLO App.axaml.cs para que pase las interfaces correctas.
Las Views se arreglarán en Fase 6.

VERIFICACIÓN:
- dotnet build sin errores
- grep -rn "DatabaseService" ViewModels/ → CERO resultados
- grep -rn "new.*Service" ViewModels/ → CERO resultados
- Todos los campos en ViewModels son tipo interfaz
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] CERO `DatabaseService` en ViewModels/
- [ ] CERO `new *Service()` en ViewModels/
- [ ] Todos los campos son interfaces

---

## FASE 6 — Limpiar Views Code-Behind

### Objetivo
Reducir el code-behind de las Views. Mover la creación de ViewModels y la orquestación de diálogos al ViewModel padre o a un servicio de navegación. Eliminar accesos directos a datos y creación de servicios desde Views.

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite).
Fases 0-5 completadas. Los Services y ViewModels ya usan interfaces e inyección.
Las Views code-behind todavía crean ViewModels con "new", crean Services,
y en un caso crean "new DatabaseService()" (CustomerCreditsLayawaysView.axaml.cs).
SalesView.axaml.cs tiene 1,860 líneas de code-behind con lógica de orquestación.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 6: Limpiar Views code-behind

IMPORTANTE: NO modificar archivos .axaml (diseño visual). Solo .axaml.cs (code-behind).

PASO 1: Crear Services/Interfaces/INavigationService.cs

public interface INavigationService
{
    // Crear ViewModels con dependencias resueltas
    T CreateViewModel<T>() where T : class;
    T CreateViewModel<T>(params object[] parameters) where T : class;
}

Implementación: Services/NavigationService.cs
Usa AppServiceProvider internamente para resolver dependencias de los ViewModels.
Para parámetros como branchId, userId, etc. que no están en DI,
usar ActivatorUtilities.CreateInstance para mezclar DI con parámetros manuales.

PASO 2: Limpiar Views — eliminar accesos directos

A. CustomerCreditsLayawaysView.axaml.cs (CRÍTICO):
   - ELIMINAR: new Services.CreditService(new Data.DatabaseService())
   - ELIMINAR: new Services.LayawayService(new Data.DatabaseService())
   - REEMPLAZAR: Recibir ICreditService e ILayawayService vía el ViewModel
     o resolverlos desde AppServiceProvider.GetRequiredService<>()

B. Todas las Views que crean TicketService:
   - ELIMINAR: new TicketService() en AddPaymentView, CashCloseView, 
     CreateCreditView, CreateLayawayView, CreditLayawayDetailView, 
     CreditsLayawaysMenuView, CustomerCreditsLayawaysView
   - REEMPLAZAR: Resolver ITicketService desde AppServiceProvider
     o pasarlo a través del ViewModel

C. Views que acceden a App.ExportService, App.DatabaseService:
   - REEMPLAZAR: Usar AppServiceProvider.GetRequiredService<>()

D. Views que crean ViewModels con "new":
   - REEMPLAZAR: Usar INavigationService o AppServiceProvider para crearlos
   - Los ViewModels que necesitan parámetros (branchId, etc.) 
     se crean con ActivatorUtilities.CreateInstance

PASO 3: Reducir SalesView.axaml.cs

La lógica de orquestación de diálogos puede quedarse en el code-behind
(es aceptable en Avalonia para manejar diálogos modales), PERO:
- Usar INavigationService para crear ViewModels de diálogos
- No crear Services directamente
- No acceder a App.* estáticos

El code-behind PUEDE mantener:
- Suscripción a eventos del ViewModel
- Mostrar diálogos modales (ShowDialog)
- Manejo de focus y teclado
- Timer para reloj

El code-behind NO DEBE tener:
- new *Service()
- new DatabaseService()
- App.DatabaseService
- Lógica de negocio

REGLAS:
- NO modificar archivos .axaml (solo .axaml.cs)
- NO cambiar la experiencia de usuario — todo debe verse y funcionar igual
- Es ACEPTABLE que el code-behind use AppServiceProvider.GetRequiredService<>() 
  para obtener servicios cuando no se pueden pasar por constructor
- El proyecto DEBE compilar Y funcionar
- Hacer commit: "refactor: clean up Views code-behind, add NavigationService (Phase 6)"

VERIFICACIÓN:
- dotnet build sin errores  
- dotnet run funciona — toda la funcionalidad POS opera igual
- grep -rn "new.*DatabaseService()" Views/ → CERO resultados
- grep -rn "new.*Service(" Views/ → CERO resultados (excepto NavigationService)
- grep -rn "App\.DatabaseService" Views/ → CERO resultados
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] `dotnet run` funciona — toda funcionalidad POS opera
- [ ] CERO `new DatabaseService()` en Views/
- [ ] CERO `new *Service()` en Views/
- [ ] INavigationService implementado y funcionando

---

## FASE 7 — Limpieza Final y Eliminación de Propiedades Estáticas

### Objetivo
Eliminar las propiedades estáticas de `App.axaml.cs` que sirvieron como puente temporal. Todo debe resolverse desde el contenedor DI.

### Prompt para Claude Code

```
CONTEXTO: Estoy refactorizando "casa_ceja_remake" (C#, .NET 8, Avalonia, SQLite).
Fases 0-6 completadas. Todo usa DI e interfaces. Las propiedades estáticas de
App.axaml.cs (App.DatabaseService, App.AuthService, etc.) aún existen como puente.

REPOSITORIO: https://github.com/VictorVega03/casa_ceja_remake  
Haz clone del repo y verifica que compila antes de empezar.

TAREA - FASE 7: Eliminar propiedades estáticas de App.axaml.cs

PASO 1: Buscar TODOS los usos de App.* en el proyecto
   grep -rn "App\.\(DatabaseService\|AuthService\|RoleService\|ConfigService\|PrintService\|ExportService\|FolioService\|UserService\)" .

PASO 2: Para cada uso encontrado, reemplazar por:
   - Si está en un Service → ya debería estar eliminado (Fase 4)
   - Si está en un ViewModel → ya debería estar eliminado (Fase 5)  
   - Si está en una View → usar AppServiceProvider.GetRequiredService<IXxxService>()
   - Si está en App.axaml.cs → resolver del contenedor directamente

PASO 3: Una vez que NINGÚN archivo use App.DatabaseService, App.AuthService, etc.:
   - Eliminar las propiedades estáticas de App.axaml.cs
   - Eliminar los campos privados _cartService, _salesService, etc.
   - Mantener solo el AppServiceProvider como punto de acceso al DI

PASO 4: Agregar métodos helper en App.axaml.cs si son necesarios para
   compatibilidad (por ejemplo, GetCashCloseService() que resuelva del DI):
   
   public ICashCloseService GetCashCloseService() => 
       AppServiceProvider.GetRequiredService<ICashCloseService>();

REGLAS:
- NO romper funcionalidad
- Verificar que cada reemplazo funciona antes de continuar
- El proyecto DEBE compilar Y funcionar al terminar
- Hacer commit: "refactor: remove static service properties from App (Phase 7)"

VERIFICACIÓN:
- dotnet build sin errores
- dotnet run funciona
- grep -rn "public static.*Service" App.axaml.cs → CERO resultados
  (excepto si quedan helpers necesarios)
- Toda resolución de servicios va por DI
```

### Criterio de aceptación
- [ ] `dotnet build` sin errores
- [ ] `dotnet run` funciona
- [ ] CERO propiedades estáticas de servicios en App.axaml.cs
- [ ] Todo se resuelve vía DI

---

## FASE 8 (Futura) — Capa de DTOs

> Esta fase se ejecutará cuando se implemente la API REST.
> Documentada aquí para referencia futura.

### Objetivo
Separar los modelos de DB de los objetos de transferencia (DTOs), preparando para la API REST.

```
Models/           → Entidades de DB (con atributos [Table], [Column])
Models/DTOs/      → Objetos de transferencia (sin atributos SQLite)
Models/Mappers/   → Conversión entre entidades y DTOs
```

---

## Resumen de Fases y Estimaciones

| Fase | Descripción | Archivos nuevos | Archivos modificados | Complejidad |
|------|-------------|-----------------|---------------------|-------------|
| 0 | DI Container | 2 | 1 (.csproj) | Baja |
| 1 | Repos específicos | ~28 (14 interfaces + 14 impl) | 0 | Media |
| 2 | Interfaces servicios + Results | ~21 (14 interfaces + 7 results) | ~14 services | Media |
| 3 | Registrar en DI | 0 | 2 (Extensions + App) | Media |
| 4 | Refactorizar Services | 0 | ~10 services + Extensions | Alta |
| 5 | Refactorizar ViewModels | 0 | ~22 ViewModels + App | Media |
| 6 | Limpiar Views | 2 (INavigation + Navigation) | ~15 Views | Alta |
| 7 | Eliminar estáticos | 0 | App + usos restantes | Baja |
| 8 | DTOs (futuro) | ~20+ | ~10+ | Media |

---

## Verificación Post-Refactorización

Después de completar todas las fases, verificar:

```bash
# 1. Compila
dotnet build

# 2. No hay "new BaseRepository" fuera de Data/Repositories
grep -rn "new BaseRepository" --include="*.cs" | grep -v "Data/Repositories" | grep -v bin | grep -v obj

# 3. No hay "new *Service(" fuera de Infrastructure y DI registration
grep -rn "new.*Service(" --include="*.cs" | grep -v Infrastructure | grep -v ServiceCollection | grep -v bin | grep -v obj

# 4. No hay "App.DatabaseService" fuera de App.axaml.cs
grep -rn "App\.DatabaseService" --include="*.cs" | grep -v "App.axaml.cs" | grep -v bin | grep -v obj

# 5. No hay DatabaseService en ViewModels ni Views
grep -rn "DatabaseService" ViewModels/ Views/ --include="*.cs"

# 6. Funciona
dotnet run
```

---

## Notas para el Agente (Claude Code)

1. **Siempre clonar el repo al inicio** — El código puede haber cambiado entre sesiones.
2. **Compilar después de cada cambio importante** — `dotnet build` frecuente.
3. **No asumir** — Leer el archivo actual antes de modificarlo.
4. **Cambios incrementales** — Si una fase es muy grande, hacer sub-commits.
5. **Si algo no compila** — Arreglar antes de continuar. No dejar errores.
6. **Respetar los namespaces existentes** — CasaCejaRemake.Data, CasaCejaRemake.Services, etc.
7. **No agregar dependencias innecesarias** — Solo Microsoft.Extensions.DependencyInjection.
8. **El patrón es siempre**: Interface en carpeta Interfaces/ → Implementación al lado.
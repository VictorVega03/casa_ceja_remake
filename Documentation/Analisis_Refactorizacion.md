# Análisis Arquitectónico Completo — Módulo POS Casa Ceja Remake

**Fecha:** 28 de Febrero, 2026  
**Repositorio:** `casa_ceja_remake` (commit `c0734ab`)  
**Alcance:** Solo módulo POS (Admin e Inventory no implementados)

---

## 1. Resumen Ejecutivo

El módulo POS tiene funcionalidad operativa: ventas, créditos, apartados, cortes de caja, tickets e impresión están implementados y trabajan. Sin embargo, la arquitectura presenta problemas significativos de acoplamiento, separación de responsabilidades y adherencia al patrón MVVM. Estos problemas no impiden que funcione hoy, pero harán muy difícil escalar a Admin/Inventario, implementar sincronización, migrar a API REST, o hacer testing automatizado en el futuro.

A continuación se detallan los hallazgos organizados por capa, seguidos de un diagnóstico SOLID y una propuesta de estado objetivo.

---

## 2. Análisis por Capa

### 2.1 Capa de Datos (Data Layer)

**Archivos involucrados:**
- `Data/DatabaseService.cs` (354 líneas)
- `Data/Repositories/IRepository.cs` (134 líneas)
- `Data/Repositories/BaseRepository.cs` (333 líneas)
- `Data/Repositories/` — 13 archivos específicos **(TODOS VACÍOS)**

#### Diagnóstico

**Lo bueno:**
- `IRepository<T>` está bien diseñado: CRUD genérico, expresiones lambda para queries, operaciones async.
- `BaseRepository<T>` implementa correctamente la interfaz, maneja timestamps de auditoría automáticamente y gestiona IDs de forma robusta.

**Los problemas:**

**PROBLEMA CRÍTICO #1 — Repositorios específicos vacíos y no usados.**  
Existen 13 archivos de repositorios específicos (`SaleRepository.cs`, `ProductRepository.cs`, etc.) pero todos tienen 0 líneas. Nadie los usa. En su lugar, cada Service crea directamente instancias de `BaseRepository<T>`:

```csharp
// Lo que pasa en TODOS los Services actualmente:
_saleRepository = new BaseRepository(databaseService);
_productRepository = new BaseRepository(databaseService);
```

Esto significa que si mañana necesitas una consulta especializada (ej: "ventas del mes por método de pago"), la lógica de esa query tendrá que vivir en el Service, mezclando acceso a datos con lógica de negocio.

**PROBLEMA #2 — DatabaseService actúa como wrapper AND como servicio.**  
`DatabaseService` tiene dos roles: gestiona la conexión/inicialización de SQLite Y expone métodos genéricos de acceso (`InsertAsync`, `Table<T>`, `QueryAsync`, `ExecuteScalarAsync`). Estos métodos de acceso directo permiten que cualquier parte del sistema bypasee los repositorios por completo. De hecho, `RoleService` ya lo hace:

```csharp
// RoleService.cs línea 37 — bypass directo del repositorio
var allRoles = await _databaseService.Table().ToListAsync();
```

Y `FolioService` usa SQL raw:

```csharp
// FolioService.cs línea 286
var count = await _databaseService.ExecuteScalarAsync(query);
```

**PROBLEMA #3 — No hay capa de DTOs/Entities.**  
Los modelos (`Sale.cs`, `Product.cs`, etc.) sirven simultáneamente como:
1. Entidades de base de datos (con atributos `[Table]`, `[Column]`)
2. Objetos de dominio en lógica de negocio
3. Objetos que se pasan a la UI (ViewModels los exponen directamente)

Cuando se implemente la API REST, se necesitará separar los modelos de DB de los DTOs de transferencia. Si los modelos cambian para la API, la UI se rompe, y viceversa.

#### Estado actual vs Estado objetivo

| Aspecto | Actual | Objetivo |
|---------|--------|----------|
| Repositorios específicos | Vacíos, no se usan | Implementados con queries especializadas |
| Acceso a datos | `BaseRepository<T>` instanciado dentro de Services | Inyectado vía interfaces (`ISaleRepository`) |
| Queries complejas | Mezcladas en Services o en SQL raw | Encapsuladas en repositorios |
| DTOs | No existen | Separar Models (DB) de DTOs (transferencia) |
| DatabaseService | Wrapper + acceso directo | Solo gestión de conexión |

---

### 2.2 Capa de Servicios (Business Logic Layer)

**Archivos involucrados:**
- `Services/SalesService.cs` (835 líneas)
- `Services/CashCloseService.cs` (678 líneas)
- `Services/TicketService.cs` (855 líneas)
- `Services/CreditService.cs` (371 líneas)
- `Services/LayawayService.cs` (380 líneas)
- `Services/FolioService.cs` (333 líneas)
- `Services/PricingService.cs` (345 líneas)
- `Services/CartService.cs` (274 líneas)
- Y otros: Auth, Config, Print, Export, Customer, Role, User, ThermalPrinterSetup

#### Diagnóstico

**Lo bueno:**
- Existe separación conceptual: cada servicio tiene un dominio (ventas, créditos, cortes, etc.)
- Se usan objetos Result (`SaleResult`, `CashCloseResult`, `CashMovementResult`) para comunicar éxito/error — buen patrón.
- `PricingService` está bien aislado: solo calcula precios, no toca la DB.
- `CartService` es in-memory, sin dependencias de DB — correcto.
- `TicketService` genera tickets sin estado — correcto.

**Los problemas:**

**PROBLEMA CRÍTICO #4 — Services mezclan lógica de negocio con acceso a datos.**  
Este es el problema más serio. Los Services deberían contener SOLO lógica de negocio y delegar el acceso a datos a los repositorios. En cambio, los Services:

1. **Crean sus propias instancias de repositorios** (no se inyectan):
```csharp
// SalesService constructor — crea 5 repos + 2 services
_saleRepository = new BaseRepository(databaseService);
_saleProductRepository = new BaseRepository(databaseService);
_productRepository = new BaseRepository(databaseService);
// ...
```

2. **Crean repositorios ad-hoc dentro de métodos** (repos que no son campo de la clase):
```csharp
// SalesService.cs líneas 500, 557, 688, 694, 704, 705, 823
var unitRepo = new BaseRepository(_databaseService);
var categoryRepo = new BaseRepository(_databaseService);
var userRepo = new BaseRepository(_databaseService);
```
Cada vez que se llama a `CreateCartItemAsync`, se crea un nuevo `BaseRepository<Unit>`. Son instancias efímeras creadas y descartadas.

3. **Hacen filtering en memoria en lugar de en la DB:**
```csharp
// SalesService.SearchProductsAsync — trae TODOS los productos y filtra en C#
var products = await _productRepository.GetAllAsync();
var results = new List();
foreach (var product in products)
{
    if (!product.Active) continue;
    // ... filtrado manual
}
```
Con 7,000+ productos (catálogo precargado), esto trae toda la tabla a memoria para filtrar. Debería ser un query en SQLite.

4. **Paginan en memoria:**
```csharp
// SalesService.GetSalesHistoryPagedAsync — trae todo y luego Skip/Take
var sales = await _saleRepository.FindAsync(s => s.BranchId == branchId);
// ... filtros en memoria
return sales.OrderByDescending(s => s.SaleDate)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToList();
```

5. **CashCloseService.CalculateTotalsAsync trae tablas enteras:**
```csharp
// CashCloseService.cs líneas 240, 285, 294, 309, 318
var allSales = await _saleRepository.GetAllAsync();
var allCredits = await _creditRepository.GetAllAsync();
var allCreditPayments = await _creditPaymentRepository.GetAllAsync();
var allLayaways = await _layawayRepository.GetAllAsync();
var allLayawayPayments = await _layawayPaymentRepository.GetAllAsync();
```
Para calcular los totales de UN turno, se cargan TODAS las ventas, TODOS los créditos, TODOS los pagos de crédito, TODOS los apartados y TODOS los pagos de apartado. Después filtra por fecha en memoria.

**PROBLEMA #5 — Services acceden a singletons estáticos de App.**  
Múltiples services usan `App.ConfigService`, `App.FolioService` directamente:

```csharp
// Dentro de SalesService, CreditService, LayawayService, CashCloseService
var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
string folio = await App.FolioService!.GenerarFolioVentaAsync(branchId, cajaId);
```

Esto crea un acoplamiento directo entre los Services y la clase `App`, haciendo imposible testear los services de forma aislada o reutilizarlos fuera de la aplicación Avalonia.

**PROBLEMA #6 — No hay interfaces para los Services.**  
No existen `ISalesService`, `ICreditService`, etc. Esto impide:
- Sustituir implementaciones (ej: mock para tests)
- Inyección de dependencias
- Desacoplamiento entre capas

**PROBLEMA #7 — Clases Result definidas dentro del archivo del Service.**  
`SaleResult`, `StockValidationResult`, `CashCloseResult`, `CashMovementResult`, `CashCloseTotals` están definidos DENTRO de los archivos de servicio. Deberían estar en archivos separados o en una carpeta `Models/Results/`.

#### Tabla de violaciones por Service

| Service | Mezcla datos+lógica | Crea repos internos | Usa App.* | GetAll + filter |
|---------|---------------------|--------------------|-----------|-----------------| 
| SalesService | ✅ Sí | ✅ 5+ repos ad-hoc | ✅ ConfigService, FolioService | ✅ SearchProducts, GetHistory |
| CashCloseService | ✅ Sí | ✅ 7 repos en constructor | ✅ ConfigService, FolioService | ✅ CalculateTotals (5 tablas completas) |
| CreditService | ✅ Sí | ✅ 5 repos | ✅ ConfigService, FolioService | — |
| LayawayService | ✅ Sí | ✅ 5 repos | ✅ ConfigService, FolioService | — |
| FolioService | ✅ SQL raw | ✅ Repos ad-hoc en métodos | — | — |
| CustomerService | ✅ Sí | ✅ 1 repo | — | — |
| AuthService | ✅ Sí | — | — | — |
| PricingService | ❌ No (puro) | ❌ No | ❌ No | ❌ No |
| CartService | ❌ No (puro) | ❌ No | ❌ No | ❌ No |
| TicketService | ❌ No (puro) | ❌ No | ❌ No | ❌ No |

Nota: `PricingService`, `CartService` y `TicketService` son los únicos correctamente aislados.

---

### 2.3 Capa de ViewModels (Presentation Logic)

**Archivos involucrados:** 22 ViewModels POS + 6 Shared (7,426 + líneas)

#### Diagnóstico

**Lo bueno:**
- Usan `CommunityToolkit.Mvvm` correctamente: `[ObservableProperty]`, `[RelayCommand]`, herencia de `ObservableObject`.
- `ViewModelBase` es mínimo y limpio.
- La mayoría delegan operaciones a Services.

**Los problemas:**

**PROBLEMA #8 — Algunos ViewModels acceden a la capa de datos directamente.**  

```csharp
// CashCloseHistoryViewModel.cs — recibe DatabaseService Y crea repositorios
public CashCloseHistoryViewModel(
    CashCloseService cashCloseService,
    AuthService authService,
    DatabaseService databaseService,  // ← NO debería recibir esto
    int branchId)
{
    _databaseService = databaseService;
}

// Luego en LoadDataAsync:
var userRepository = new BaseRepository(_databaseService);
var branchRepository = new BaseRepository(_databaseService);
```

Un ViewModel NUNCA debería conocer `DatabaseService` ni crear repositorios. Debería pedir esos datos a un Service.

```csharp
// AppConfigViewModel.cs línea 94 — acceso directo a DB desde ViewModel
var branchList = await _databaseService.Table().ToListAsync();
```

**PROBLEMA #9 — ViewModels crean Services ellos mismos.**  
```csharp
// SalesViewModel.cs línea 173
var cashCloseService = new CashCloseService(App.DatabaseService!);
```

Un ViewModel creando un Service internamente rompe la inversión de dependencias.

**PROBLEMA #10 — No hay navegación centralizada.**  
Los ViewModels comunican navegación mediante eventos (`CloseRequested`, `ItemSelected`, `ExportRequested`) que son manejados por los code-behind de las Views. Esto está parcialmente bien, pero la falta de un servicio de navegación centralizado obliga a que la lógica de "qué pantalla abrir después" viva en los code-behind.

---

### 2.4 Capa de Vistas (View Layer)

**Archivos involucrados:** 29 Views POS + 8 Shared (code-behind: 6,324 líneas)

#### Diagnóstico

**Este es donde el patrón MVVM se rompe más severamente.**

**PROBLEMA CRÍTICO #11 — Code-behind masivo con lógica de orquestación.**  
`SalesView.axaml.cs` tiene **1,860 líneas** de code-behind. En MVVM puro, el code-behind debería tener casi nada — solo inicialización de componentes y manejo de eventos que no pueden ir en binding. En cambio, `SalesView.axaml.cs` contiene:

- Creación e inicialización de ViewModels hijos (CustomerSearchViewModel, SearchProductViewModel, PaymentViewModel, CashMovementViewModel, CashCloseHistoryViewModel, etc.)
- Lógica de navegación entre diálogos
- Suscripción a eventos y coordinación entre vistas
- Gestión de estado de diálogos (`_hasOpenDialog`)

Las Views están actuando como **Controllers/Coordinators**, un rol que debería estar en los ViewModels o en un servicio de navegación.

**PROBLEMA CRÍTICO #12 — Views crean Services y acceden a datos.**  

```csharp
// CustomerCreditsLayawaysView.axaml.cs líneas 101, 118
// CREA NUEVAS INSTANCIAS DE DatabaseService — PELIGROSO
var creditService = new Services.CreditService(new Data.DatabaseService());
var layawayService = new Services.LayawayService(new Data.DatabaseService());
```

Esto es doblemente problemático:
1. Una View está creando Services (violación MVVM)
2. Crea **NUEVAS instancias de DatabaseService**, lo que significa conexiones SQLite separadas que podrían causar problemas de concurrencia

```csharp
// Múltiples Views crean TicketService directamente
var ticketService = new CasaCejaRemake.Services.TicketService(); // En 7+ lugares
```

**PROBLEMA #13 — Views acceden a App.* para obtener services.**  
```csharp
// Múltiples Views
App.ExportService
App.DatabaseService!
App.PrintService
```

**Resumen de violaciones MVVM en Views:**

| Vista (code-behind) | Líneas | Crea VMs | Crea Services | Accede App.* | Accede DB |
|---------------------|--------|----------|---------------|--------------|-----------|
| SalesView | 1,860 | ✅ 8+ VMs | ✅ CashCloseService | ✅ DatabaseService | — |
| CreditsLayawaysMenuView | 840 | ✅ 3+ VMs | ✅ TicketService | — | — |
| CustomerCreditsLayawaysView | 524 | — | ✅ CreditService, LayawayService (con new DatabaseService!) | — | ✅ Indirecto |
| CashCloseView | 212 | — | ✅ TicketService | — | — |
| AddPaymentView | 230 | — | ✅ TicketService | — | — |

---

### 2.5 Capa de Inyección de Dependencias

**Estado actual: NO EXISTE.**

**PROBLEMA CRÍTICO #14 — No hay contenedor de Dependencias (DI Container).**  

Todo se maneja mediante:

1. **Propiedades estáticas en `App.axaml.cs`:**
```csharp
public static DatabaseService? DatabaseService { get; private set; }
public static AuthService? AuthService { get; private set; }
public static ConfigService? ConfigService { get; private set; }
// ... etc.
```

2. **Instanciación manual con `new`:**
```csharp
DatabaseService = new DatabaseService();
RoleService = new RoleService(DatabaseService);
AuthService = new AuthService(userRepository, RoleService);
```

3. **Dependencias pasadas por constructor pero creadas manualmente:**
```csharp
var salesViewModel = new SalesViewModel(cartService, salesService, authService, branchId);
```

Esto tiene consecuencias graves:
- **No hay lifetime management**: No se controla si un servicio es singleton, transient o scoped.
- **No hay testabilidad**: No se pueden mockear dependencias.
- **Acoplamiento total**: Cada lugar que necesita un servicio tiene que saber cómo crearlo o acceder al singleton estático.

---

## 3. Diagnóstico SOLID

### S — Single Responsibility Principle ❌ VIOLADO

| Clase | Responsabilidades que tiene | Responsabilidades que debería tener |
|-------|----------------------------|-------------------------------------|
| SalesService | Lógica de ventas + búsqueda de productos + creación de CartItems + acceso a DB | Solo lógica de procesamiento de ventas |
| CashCloseService | Lógica de cortes + cálculo de totales + acceso a ventas/créditos/apartados | Solo lógica de cortes de caja |
| SalesView.axaml.cs | Renderizado + navegación + creación de VMs + orquestación de diálogos | Solo renderizado y binding |
| DatabaseService | Gestión de conexión + inicialización de tablas + operaciones CRUD genéricas | Solo gestión de conexión |
| App.axaml.cs | Contenedor DI improvisado + navegación + inicialización + gestión de ciclo de vida | Solo bootstrap de la aplicación |

### O — Open/Closed Principle ❌ VIOLADO

No se pueden agregar nuevas fuentes de datos (ej: API REST) sin modificar todos los Services, porque dependen directamente de `BaseRepository<T>` y `DatabaseService`. Si se quisiera que `SalesService` trabaje contra una API en lugar de SQLite, hay que reescribirlo.

### L — Liskov Substitution Principle ⚠️ PARCIAL

`BaseRepository<T>` implementa `IRepository<T>` correctamente, pero nadie lo usa a través de la interfaz. Todos los campos son `BaseRepository<T>` concreto, no `IRepository<T>`.

### I — Interface Segregation Principle ❌ VIOLADO

No existen interfaces para Services. No hay `ISalesService`, `ICashCloseService`, etc. Tampoco existen interfaces específicas para los repositorios que necesitan consultas especializadas.

### D — Dependency Inversion Principle ❌ VIOLADO SEVERAMENTE

Este es el principio más violado en todo el proyecto:
- Services dependen de implementaciones concretas (`BaseRepository<T>`, `DatabaseService`)
- ViewModels dependen de implementaciones concretas (Services sin interfaces)
- Views dependen de implementaciones concretas (crean Services y ViewModels con `new`)
- Services dependen de `App.*` singletons estáticos

---

## 4. Flujo de Dependencias Actual vs Objetivo

### Actual (problemático)

```
┌──────────────────────────────────┐
│          Views (.axaml.cs)       │
│   Crea ViewModels con new        │
│   Crea Services con new          │
│   Accede App.* estáticos         │
│   Accede DatabaseService         │
└──────────┬───────────────────────┘
           │ depende de
           ▼
┌──────────────────────────────────┐
│          ViewModels              │
│   Recibe Services concretos      │
│   A veces crea Services con new  │
│   A veces accede DatabaseService │
└──────────┬───────────────────────┘
           │ depende de
           ▼
┌──────────────────────────────────┐
│          Services                │
│   Crea BaseRepository<T> con new │
│   Accede App.* estáticos         │
│   Mezcla lógica + datos          │
└──────────┬───────────────────────┘
           │ depende de
           ▼
┌──────────────────────────────────┐
│   BaseRepository<T> (concreto)   │
│   DatabaseService (concreto)     │
└──────────────────────────────────┘
```

**Problemas:** Flechas van en la dirección correcta (arriba→abajo) pero son contra CONCRETOS, no abstracciones. Además hay saltos de capa (Views→Database, ViewModels→Database).

### Objetivo (correcto)

```
┌──────────────────────────────────┐
│     Views (.axaml + code-behind) │
│   SOLO: rendering + binding      │
│   Mínimo code-behind             │
└──────────┬───────────────────────┘
           │ DataBinding
           ▼
┌──────────────────────────────────┐
│     ViewModels                   │
│   Depende de IService interfaces │
│   Usa INavigationService         │
│   NUNCA toca datos directamente  │
└──────────┬───────────────────────┘
           │ interfaces
           ▼
┌──────────────────────────────────┐
│     Services (Business Logic)    │
│   Depende de IRepository intfcs  │
│   Recibe dependencias inyectadas │
│   NO conoce App.*, solo intfcs   │
└──────────┬───────────────────────┘
           │ interfaces
           ▼
┌──────────────────────────────────┐
│     Repositories                 │
│   Queries especializadas por     │
│   entidad, implementan IXxxRepo  │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│     DatabaseService / DbContext   │
│   Solo gestión de conexión       │
└──────────────────────────────────┘
```

---

## 5. Inventario de Problemas Priorizados

### 🔴 Críticos (impiden escalabilidad)

| # | Problema | Dónde | Impacto |
|---|---------|-------|---------|
| 1 | Repositorios específicos vacíos | Data/Repositories/ | Queries complejas viven en Services |
| 4 | Services mezclan lógica + datos | Todos los Services | No se puede cambiar fuente de datos sin reescribir lógica |
| 5 | Services acceden a App.* | SalesService, CreditService, LayawayService, CashCloseService | Imposible testear, acoplamiento total |
| 11 | Code-behind masivo | SalesView (1,860 líneas) y otros | MVVM roto, lógica duplicada |
| 12 | Views crean Services y DatabaseService | CustomerCreditsLayawaysView | Conexiones DB múltiples, violación de capas |
| 14 | Sin contenedor DI | App.axaml.cs | Todo acoplado, sin testabilidad |

### 🟡 Importantes (afectan mantenimiento)

| # | Problema | Dónde | Impacto |
|---|---------|-------|---------|
| 2 | DatabaseService expone acceso directo | DatabaseService.cs | Permite bypass de repos |
| 3 | Sin DTOs | Models/ | Modelos DB = Domain = UI |
| 6 | Sin interfaces para Services | Services/ | No hay abstracción |
| 7 | Clases Result dentro de archivos de Service | Services/ | Organización pobre |
| 8 | ViewModels acceden a datos | CashCloseHistoryViewModel, AppConfigViewModel | Salto de capa |
| 9 | ViewModels crean Services | SalesViewModel | Inversión de dependencias violada |

### 🟢 Menores (mejoras deseables)

| # | Problema | Dónde | Impacto |
|---|---------|-------|---------|
| 10 | Sin servicio de navegación | Views/ | Navegación dispersa en code-behind |
| — | GetAll + filter en memoria | SalesService, CashCloseService | Performance con datasets grandes |
| — | Console.WriteLine como logging | Servicios | Sin sistema de logging formal |
| — | Strings hardcodeados | Servicios | "CAJA-01", nombres de métodos de pago |

---

## 6. Métricas del Estado Actual

```
Archivos .cs totales:        150
Archivos .axaml totales:     30
Líneas de código totales:    ~33,088

Code-behind Views:           6,324 líneas (19% del total)
ViewModels:                  7,426 líneas (22%)
Services:                    6,408 líneas (19%)
Models:                      1,694 líneas (5%)
Repositorios (con código):   467 líneas (1.4%)
Helpers:                     2,742 líneas (8%)
Otros (App, etc):            ~8,000 líneas

Archivos vacíos:             28 (13 repos + 13 VMs admin/inv + SyncService + TicketSnapshot)

Instancias de "new BaseRepository" en Services:  23+
Instancias de "new BaseRepository" en ViewModels: 3
Instancias de "new BaseRepository" en Views:      0 (pero crean Services que los crean)
Instancias de "new *Service" en Views:            12+
Accesos a "App.*" en Services:                    15+
Accesos a "App.*" en Views:                       8+
```

---

## 7. Conclusión

El proyecto tiene una **base funcional sólida** — la lógica de negocio de ventas, créditos, apartados y cortes funciona. Los modelos están bien definidos, y algunos servicios puros (Pricing, Cart, Ticket) siguen buenos patrones.

Sin embargo, la arquitectura actual es esencialmente **procedural disfrazada de MVVM**: las capas existen en carpetas pero no hay separación real de responsabilidades entre ellas. La capa de datos (repositorios) está diseñada pero no se usa, los Services son "god classes" que mezclan todo, y las Views actúan como coordinadores en lugar de ser pantallas pasivas.

**Antes de avanzar con Admin, Inventario o Sincronización, es necesario refactorizar la arquitectura POS para que realmente siga MVVM + Repository + SOLID.** De lo contrario, cada módulo nuevo replicará los mismos problemas y el sistema será cada vez más difícil de mantener.
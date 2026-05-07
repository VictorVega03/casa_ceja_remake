# Plan de Implementación — Módulo Administrador
## Casa Ceja Remake

**Fecha de planificación:** Mayo 2026  
**Estado:** Listo para implementar — pendiente de inicio  
**Repositorio:** https://github.com/VictorVega03/casa_ceja_remake  
**Stack:** .NET 8 / Avalonia 11.3 / SQLite / CommunityToolkit.Mvvm / sqlite-net-pcl

---

## 1. Contexto del Proyecto

El sistema Casa Ceja Remake es una refactorización completa del sistema legacy (Windows Forms / .NET Framework). Los módulos **POS** e **Inventario** están terminados. El módulo **Administrador** existe solo como un placeholder — el método `ShowAdmin()` en `App.axaml.cs` abre una ventana gris con texto "Próximamente".

Los archivos `ViewModels/Admin/` (AdminMainViewModel, BranchesViewModel, CategoriesViewModel, ProductsViewModel, ReportsViewModel, SuppliersViewModel, UnitsViewModel, UsersViewModel) están **creados pero vacíos (0 bytes)**. La carpeta `Views/Admin/` no existe todavía.

---

## 2. Principios de Trabajo

- Reutilizar vistas y servicios existentes siempre que sea posible
- Seguir la arquitectura y patrones del proyecto (MVVM, eventos, navegación por `App.axaml.cs`)
- No modificar diseños visuales (`.axaml`) a menos que se indique explícitamente
- No modificar funcionalidad existente de POS ni Inventario
- Avanzar etapa por etapa — no implementar todo de golpe
- El administrador opera a nivel global — sin sucursal fija
- Mismos colores y estilo visual que los módulos existentes (dark theme, cards grises)

---

## 3. Lo que Existe y Se Puede Reutilizar

### Vistas reutilizables directamente

| Vista existente | Ubicación | Uso en Admin |
|---|---|---|
| `CatalogView` | `Views/Inventory/` | Catálogo de productos (alta, modificar, existencias) |
| `CatalogsManagementView` | `Views/Inventory/` | Gestión de medidas y categorías |
| `ProductFormView` | `Views/Inventory/` | Alta y edición de producto |
| `ProductDetailView` | `Views/Inventory/` | Detalle de producto |
| `StockByBranchDialog` | `Views/Shared/` | Ver existencias de un producto por sucursal |
| `UserManagementView` | `Views/Shared/` | Gestión de usuarios — ya tiene flag `isAdminMode` |
| `UserFormView` | `Views/Shared/` | Alta y edición de usuario |

### Vistas que necesitan adaptación (no rediseño, solo flag modo admin)

| Vista | Adaptación requerida |
|---|---|
| `HistoryView` + `HistoryViewModel` | Agregar modo admin: dropdown de sucursal + opción "Todas" (mismo patrón de `UserManagementViewModel` con `isAdminMode`) |
| `CashCloseHistoryView` + `CashCloseHistoryViewModel` | Igual: hoy filtra por `branchId` fijo, en modo admin debe poder ver todas las sucursales |

### Servicios existentes relevantes

- `InventoryService` — ya tiene `GetCategoriesAsync`, `GetUnitsAsync`, `GetSuppliersAsync`, `GetBranchesAsync`, `SaveCategoryAsync`, `SaveUnitAsync`
- `UserService` — CRUD completo de usuarios con roles
- `CashCloseService` — historial de cortes por sucursal
- `AuthService` — `HasBranchAccess()` retorna `true` para admin en cualquier sucursal
- `ExportService` — exportar a Excel (ClosedXML)
- `SyncService` — ya tiene `PushUserAsync`; necesitará métodos push para Branch y Supplier

### Lo que NO existe y debe crearse

- `BranchService` — CRUD de sucursales
- `SupplierService` — CRUD de proveedores
- `AdminMainView` + `AdminMainViewModel` — menú principal con 9 tarjetas
- Vistas de Sucursales (`BranchListView`, `BranchFormView`)
- Vistas de Proveedores (`SupplierListView`, `SupplierFormView`)
- `GlobalStockView` + `GlobalStockViewModel` — existencias por sucursal
- Métodos push en `SyncService` para Branch y Supplier

---

## 4. Decisiones de Diseño Tomadas

### 4.1 Menú principal del Admin

- 9 tarjetas en grid 3×3 (mismo estilo que `InventoryMainView`: fondo oscuro `#1C1C1C`, cards grises `#D8D8D8`, íconos circulares de colores)
- El factor diferenciador visual es el color del header: tono marrón/cobre oscuro (similar al sistema legacy) en lugar del azul de Inventario
- Sin indicador de conectividad en el header (el admin opera globalmente)
- Header muestra: nombre del usuario admin + "Acceso global" (sin nombre de sucursal)
- Botones: "Módulos" (volver al selector) y "Cerrar sesión"

### 4.2 Alta de productos

- **Tanto Admin como Inventario** pueden dar de alta productos nuevos
- **Solo Admin** puede modificar productos existentes
- Esto implica que en Inventario, el botón "Modificar" del catálogo debe estar deshabilitado o no visible. Verificar el estado actual antes de implementar — en el historial de commits existe una modificación reciente sobre esto

### 4.3 Usuarios sin sucursal fija

- El campo `branch_id` en `User` es la **sucursal de creación/asignación**, no la sucursal de trabajo activa
- La sucursal activa siempre viene de `ConfigService.AppConfig.CurrentBranchId`
- En el POS, `GetCashiersAsync(branchId)` filtra cajeros visibles por `branch_id` — muestra los cajeros que "pertenecen" a esa sucursal
- Cuando el Admin crea un usuario, **debe seleccionar a qué sucursal pertenece** (`branch_id`). Eso determina en qué terminal del POS aparece disponible para login

### 4.4 El Administrador no tiene sucursal fija

- En `ShowAdmin()` en `App.axaml.cs` no se pasa `branchId` al ViewModel
- Las vistas que normalmente reciben `branchId` (como `CatalogView`) reciben `branchId: 0` al abrirse desde Admin — eso es intencional y ya funciona porque `CatalogViewModel` almacena el branchId pero no lo usa para filtrar

---

## 5. Sistema de Roles y Seguridad de Acceso

### 5.1 Roles actuales

| Key | Nombre | Acceso |
|---|---|---|
| `admin` | Administrador | POS + Inventario + Admin module |
| `inventory` | Inventario | POS (solo visualizar) + Inventario |
| `cashier` | Cajero | Solo POS |

### 5.2 Problema identificado

El mismo rol `admin` permite tanto **desbloquear operaciones cotidianas** (descuentos, gestión de cajeros, cambio de sucursal) como **entrar al módulo Administrador** y modificar todo el sistema. Compartir las credenciales de admin con un encargado para desbloquear descuentos equivale a darle acceso total al sistema.

### 5.3 Solución adoptada — PIN de módulo (Opción A)

**Para operaciones del día a día** (sigue igual con credenciales de admin normales):
- Desbloquear descuento general en POS (`SalesView`)
- Acceso a gestión de cajeros desde POS (`SalesView`)
- Configuración de parámetros de ticket en `PosTerminalConfigView`

**Para cambio de sucursal** (sigue requiriendo login de admin, sin PIN):
- Desbloquear cambio de sucursal en `AppConfigView` — es configuración inicial/técnica, no operación diaria

**Para entrar al módulo Administrador** (nueva capa de seguridad):
- Requiere: (1) tener rol `admin` + (2) ingresar el **PIN del módulo Admin**
- El PIN se pide al hacer click en "Administrador" en el ModuleSelector, después de verificar el rol
- El PIN se configura y guarda en `AppConfig` (JSON local) — campo `AdminModulePin`
- Si el PIN no está configurado, el sistema lo pide la primera vez y lo guarda
- Si el PIN está vacío/nulo en config, no se solicita (útil para instalaciones nuevas hasta que el dueño lo configure)

### 5.4 Dónde configurar el PIN

La configuración del PIN vive en **AppConfigView** (la configuración general accesible desde el ModuleSelector), en la misma sección donde se configura la sucursal. Para ver/cambiar el PIN se requiere el login de usuario administrador (como ya funciona para cambiar la sucursal). Esto significa que el dueño/admin configura el PIN desde la misma pantalla que ya controla.

### 5.5 Cambios técnicos necesarios para el PIN

- Agregar `AdminModulePin` (string, nullable) a `AppConfig` y a `ConfigService`
- Agregar el campo y su UI a `AppConfigView` / `AppConfigViewModel` — solo visible para admins, en la misma sección de sucursal
- Modificar `ModuleSelectorView.axaml.cs` (o el handler en `App.axaml.cs`) para pedir el PIN antes de disparar `AdminSelected`
- Crear un diálogo simple de ingreso de PIN (puede ser el mismo patrón de `AdminVerificationHelper` pero solo para PIN numérico)

---

## 6. Mapa de Uso de Verificación Admin (Referencia)

Para no romper nada durante la implementación:

| Archivo | Qué protege | Tipo |
|---|---|---|
| `SalesView.axaml.cs` línea ~1866 | Acceso a gestión de cajeros desde POS | Operativo |
| `SalesView.axaml.cs` línea ~1893 | Descuento general en venta | Operativo |
| `AppConfigView.axaml.cs` línea ~34 | Cambio de sucursal del terminal | Técnico/config |
| `PosTerminalConfigView.axaml.cs` línea ~45 | Parámetros del ticket de impresión | Operativo |

El PIN del módulo Admin es independiente de todos estos — va en el `ModuleSelector`, no en ninguno de los anteriores.

---

## 7. Plan de Implementación por Etapas

### Etapa 1 — Menú principal del Admin ⭐ Primera en implementar

**Objetivo:** Reemplazar el placeholder con el menú real de 9 tarjetas. Conectar las que ya tienen vista destino; las demás muestran un diálogo "En desarrollo" con el nombre de la etapa.

**Archivos a crear:**
- `ViewModels/Admin/AdminMainViewModel.cs` — 9 eventos de navegación + Logout + Exit
- `Views/Admin/AdminMainView.axaml` — grid 3×3 con 9 cards, header marrón
- `Views/Admin/AdminMainView.axaml.cs` — code-behind mínimo

**Archivos a modificar:**
- `App.axaml.cs` — reemplazar `ShowAdmin()` placeholder; agregar `ShowAdminCatalog()`, `ShowAdminCatalogsManagement()`, `ShowAdminUsers()`; agregar `ShowComingSoonDialog()` para etapas futuras

**Tarjetas funcionales desde Etapa 1:**
- **CATÁLOGO** → abre `CatalogView` con `branchId: 0`, con alta y edición habilitadas
- **MEDIDAS** → abre `CatalogsManagementView` como diálogo (mismo que Inventario)
- **CATEGORÍAS** → abre `CatalogsManagementView` como diálogo (mismo que Inventario)
- **USUARIOS** → abre `UserManagementView` con `isAdminMode: true` (ya soportado)

**Tarjetas con placeholder (etapas posteriores):**
- SUCURSALES (Etapa 3), PROVEEDORES (Etapa 4), ENT. Y SALIDAS (Etapa 2), HIST. CORTES (Etapa 2), EXISTENCIAS (Etapa 5)

**Notas de implementación:**
- `CatalogViewModel` recibe `branchId` pero no lo usa para filtrar — pasar `0` es correcto para modo global
- `ProductFormViewModel` constructor: `(InventoryService, branchId: 0, product?)` — no olvidar el branchId
- Eventos correctos de `ProductFormViewModel`: `SaveCompleted` (no `SavedSuccessfully`), `CancelRequested` (no `CloseRequested`)
- Eventos correctos de `UserFormViewModel`: `SaveCompleted`, `CloseRequested` (el ViewModel los invoca ambos al guardar — suscribir solo `SaveCompleted` para el refresh, dejar que `CloseRequested` maneje el cierre)
- Al cerrar el catálogo (GoBack), volver a `ShowAdmin()` — no al ModuleSelector
- Usar `window.Tag` para determinar la acción al cerrar (mismo patrón de `InventoryMainView`)

---

### Etapa 2 — Historial global de movimientos y cortes

**Objetivo:** Adaptar `HistoryView` y `CashCloseHistoryView` para poder ver datos de todas las sucursales desde Admin.

**Patrón de implementación** (mismo que `UserManagementViewModel`):
- Agregar `bool isAdminMode` al constructor de `HistoryViewModel`
- En modo admin: mostrar dropdown con todas las sucursales + opción "Todas"
- En modo inventario: comportamiento actual (filtrado por `branchId` del config)
- Mismo patrón para `CashCloseHistoryViewModel`

**Archivos a modificar:**
- `ViewModels/Inventory/HistoryViewModel.cs` — agregar `isAdminMode`, dropdown de sucursal
- `Views/Inventory/HistoryView.axaml` — agregar ComboBox de sucursal (visible solo en modo admin)
- `ViewModels/POS/CashCloseHistoryViewModel.cs` — agregar `isAdminMode`, dropdown de sucursal
- `Views/POS/CashCloseHistoryView.axaml` — agregar ComboBox de sucursal (visible solo en modo admin)
- `App.axaml.cs` — en `ShowAdmin()`, conectar las tarjetas de historial usando las vistas adaptadas

**No romper:** Las instanciaciones existentes desde Inventario (`ShowHistory`) y POS (`CashCloseHistoryView`) siguen funcionando igual, pasando `isAdminMode: false`.

---

### Etapa 3 — CRUD Sucursales

**Objetivo:** Gestión completa de sucursales (lista, alta, edición, ver inventario).

**Archivos a crear:**
- `Services/BranchService.cs` — CRUD con validaciones, push al servidor
- `ViewModels/Admin/BranchListViewModel.cs` — lista con búsqueda, paginación
- `ViewModels/Admin/BranchFormViewModel.cs` — alta y edición
- `Views/Admin/BranchListView.axaml` + `.axaml.cs`
- `Views/Admin/BranchFormView.axaml` + `.axaml.cs`

**Campos de Branch** (ya en el modelo):
- `Name` (requerido), `Address` (requerido), `Email` (requerido), `RazonSocial`, `Active`

**Funcionalidades:**
- Listar sucursales activas con búsqueda por nombre
- Alta de sucursal nueva (push inmediato a `/api/v1/admin/branches`)
- Edición de sucursal
- Dar de baja (soft delete, `Active = false`)
- Ver inventario de sucursal — reutilizar `StockByBranchDialog` o crear vista específica

**Servicios a extender:**
- `SyncService` — agregar `PushBranchAsync(Branch branch)` (patrón idéntico a `PushUserAsync`)
- `ApiClient` — ya tiene el método `PostAsync<T>` genérico; solo necesita la URL correcta

**Endpoint API:** `CRUD /api/v1/admin/branches` (ya existe en el servidor Laravel)

---

### Etapa 4 — CRUD Proveedores

**Objetivo:** Gestión de proveedores (lista, alta, edición, exportar a Excel).

**Archivos a crear:**
- `Services/SupplierService.cs` — CRUD con validaciones, push al servidor
- `ViewModels/Admin/SupplierListViewModel.cs`
- `ViewModels/Admin/SupplierFormViewModel.cs`
- `Views/Admin/SupplierListView.axaml` + `.axaml.cs`
- `Views/Admin/SupplierFormView.axaml` + `.axaml.cs`

**Nota sobre el modelo Supplier:**
El modelo actual (`Models/Supplier.cs`) tiene: `Name`, `Phone`, `Email`, `Address`, `Active`. El sistema legacy también mostraba un campo `Description`. Decidir antes de implementar si se agrega `Description` al modelo — requeriría migración de la base de datos (agregar columna a la tabla `suppliers`).

**Funcionalidades:**
- Listar proveedores activos con búsqueda por nombre
- Alta de proveedor (push a `/api/v1/admin/suppliers`)
- Edición de proveedor
- Dar de baja (soft delete)
- Exportar lista a Excel (reutilizar `ExportService`)

**Endpoint API:** `CRUD /api/v1/admin/suppliers` (ya existe en el servidor Laravel)

---

### Etapa 5 — Existencias globales de productos

**Objetivo:** Vista para ver el inventario de cualquier sucursal con filtros y exportación.

**Archivos a crear:**
- `ViewModels/Admin/GlobalStockViewModel.cs`
- `Views/Admin/GlobalStockView.axaml` + `.axaml.cs`

**Funcionalidades:**
- Dropdown de sucursal (todas las sucursales activas)
- Checkbox "Solo productos con existencia"
- DataGrid: ID | Código | Nombre | Categoría | Precio | Existencia
- Existencias negativas en rojo
- Paginación
- Exportar a Excel (botón)
- Contador "Mostrando X de Y productos"

**Servicio a usar:**
- `SyncService.GetBranchStockAsync(branchId, page)` — ya existe en `SyncService` (línea ~813)
- O bien `InventoryService` con consulta local si no hay conexión

---

### Etapa 6 — PIN de módulo Admin (puede implementarse en cualquier etapa)

**Puede hacerse en paralelo o entre etapas — no depende de las anteriores.**

**Archivos a modificar:**
- `Models/AppConfig.cs` — agregar `public string? AdminModulePin { get; set; }`
- `ViewModels/Shared/AppConfigViewModel.cs` — agregar campo para ver/cambiar el PIN (solo visible si `IsAdmin`)
- `Views/Shared/AppConfigView.axaml` — agregar sección PIN en la vista de config, condicionada a rol admin
- `App.axaml.cs` — en el handler de `AdminSelected` en `ShowModuleSelector()`, verificar el PIN antes de llamar a `ShowAdmin()`

**Lógica del PIN:**
```
ModuleSelector → click "Administrador" → verificar rol admin → pedir PIN →
  PIN correcto → ShowAdmin()
  PIN incorrecto → mensaje de error, sin acceso
  PIN vacío en config → dejar pasar (no configurado todavía)
```

**Diálogo de PIN:** Similar a `AdminVerificationHelper` pero solo pide un número/código. No pide usuario ni contraseña — solo el PIN de 4-6 caracteres.

---

## 8. Orden de Implementación Recomendado

```
Etapa 1 (Menú) → Etapa 6 (PIN) → Etapa 2 (Historiales) → Etapa 3 (Sucursales) → Etapa 4 (Proveedores) → Etapa 5 (Existencias)
```

El PIN puede hacerse después del menú porque es corto y da seguridad temprana. Los historiales antes de sucursales porque son adaptaciones, no features nuevas. Sucursales antes de proveedores porque los proveedores no tienen dependencias cruzadas.

---

## 9. Notas de Arquitectura para Implementación

### Patrón de navegación (App.axaml.cs)

Toda la navegación pasa por `App.axaml.cs`. El patrón es:
1. Crear ViewModel y View
2. Suscribir eventos del ViewModel
3. Para subvistas modales: `ShowDialog(parentWindow)`
4. Para ventanas de pantalla completa: `desktop.MainWindow = view; view.Show(); windowToClose?.Close()`
5. Al cerrar: usar `window.Tag` para determinar a dónde navegar

### Patrón de servicios

- Servicios nuevos (BranchService, SupplierService) siguen el patrón de `UserService`
- Constructor recibe `IRepository<T>` + servicios necesarios
- Métodos retornan `(bool Success, string Message)` para operaciones de escritura
- Push al servidor es fire-and-forget (`_ = Task.Run(() => syncService.PushXAsync(entity))`)

### Nombre de eventos en ViewModels existentes (referencia)

- `ProductFormViewModel`: `SaveCompleted`, `CancelRequested`, `StartSaveConfirmation`
- `UserFormViewModel`: `SaveCompleted`, `CloseRequested` (ambos se invocan al guardar)
- `CatalogViewModel`: `GoBackRequested`, `ProductFormRequested`, `ProductDetailRequested`, `StockDataReady`
- `CatalogsManagementViewModel`: `GoBackRequested`, `CloseRequested`, `ShowErrorRequested`
- `UserManagementViewModel`: `CloseRequested`, `EditUserRequested`, `AddUserRequested`

### Constructor de ProductFormViewModel

```csharp
// CORRECTO — siempre pasar branchId
new ProductFormViewModel(_inventoryService, branchId: 0, product: null)  // alta nueva
new ProductFormViewModel(_inventoryService, branchId: 0, product: existingProduct)  // edición
```

### Después de guardar producto en catálogo admin

```csharp
formVm.SaveCompleted += (s, e) =>
{
    formView.Close();
    viewModel.RefreshData();  // no InitializeAsync()
};
```

---

## 10. Pendientes y Decisiones Abiertas

- [ ] Verificar el estado actual del botón "Modificar" en `CatalogView` de Inventario — el historial de commits menciona una modificación reciente sobre dar de baja en categorías/unidades. Confirmar que en Inventario no se puede modificar productos (solo verlos y dar alta)
- [ ] Decidir si se agrega el campo `Description` al modelo `Supplier` antes de la Etapa 4 (requiere migración de BD)
- [ ] En la Etapa 3, decidir si "Ver inventario de sucursal" abre el `StockByBranchDialog` existente o una vista nueva de pantalla completa con paginación
- [ ] Diseño de la sección PIN en `AppConfigView` — debe integrarse naturalmente sin romper el layout actual
- [ ] El diseño visual de `UserManagementView` para el módulo Admin: actualmente funciona y es suficiente, pero se puede revisar en su momento si se quiere un diseño diferente

---

*Documento generado en sesión de planificación — Mayo 2026*  
*Para implementación, iniciar siempre clonando el repo y verificando el estado actual del código*

# Plan de Implementación — Módulo Administrador
## Casa Ceja Remake

**Fecha de planificación:** Mayo 2026  
**Última actualización:** Mayo 2026  
**Estado:** Etapa 1 completada ✅ — Siguiente: PIN + Bloqueo sin conexión  
**Repositorio:** https://github.com/VictorVega03/casa_ceja_remake  
**Stack:** .NET 8 / Avalonia 11.3 / SQLite / CommunityToolkit.Mvvm / sqlite-net-pcl

---

## 1. Contexto del Proyecto

El sistema Casa Ceja Remake es una refactorización completa del sistema legacy (Windows Forms / .NET Framework). Los módulos **POS** e **Inventario** están terminados. El módulo **Administrador** tiene el menú principal implementado (Etapa 1 ✅).

`Views/Admin/AdminMainView.axaml` y `ViewModels/Admin/AdminMainViewModel.cs` están implementados y conectados en `App.axaml.cs`. Las tarjetas de Catálogo, Medidas, Categorías y Usuarios son funcionales desde esta etapa. Las tarjetas de Sucursales, Proveedores, Entradas/Salidas, Historial de Cortes y Existencias muestran un diálogo "En desarrollo" hasta su etapa correspondiente.

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
- `ApiClient` — ya tiene `GetAsync`, `PostAsync`, `PutAsync` genéricos; Admin los usa directamente contra `/api/v1/admin/*`

### Lo que NO existe y debe crearse

- `BranchService` — CRUD de sucursales usando `/api/v1/admin/branches`
- `SupplierService` — CRUD de proveedores usando `/api/v1/admin/suppliers`
- Vistas de Sucursales (`BranchListView`, `BranchFormView`)
- Vistas de Proveedores (`SupplierListView`, `SupplierFormView`)
- `GlobalStockView` + `GlobalStockViewModel` — existencias por sucursal

**Nota:** `AdminMainView` + `AdminMainViewModel` ya fueron creados en la Etapa 1 ✅. Los servicios nuevos de Admin **no usan el patrón SyncService** (sin folios, sin lotes, sin `SyncStatus=1` pendiente) — usan `ApiClient` directamente con los endpoints REST de `/api/v1/admin/*`.

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
- Si el PIN está vacío/nulo en config → **dejar pasar sin pedir nada** (instalación nueva sin PIN configurado todavía)

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

### ~~Etapa 1 — Menú principal del Admin~~ ✅ COMPLETADA

**Implementado en:** Mayo 2026  
**Commit:** `feat: add AdminMain module with navigation commands and connectivity monitoring`

**Lo que se implementó:**
- `ViewModels/Admin/AdminMainViewModel.cs` — 9 eventos de navegación + conectividad
- `Views/Admin/AdminMainView.axaml` + `AdminMainView.axaml.cs` — grid 3×3 con 9 cards, header azul, banners de conectividad
- `App.axaml.cs` — reemplazó el placeholder; `ShowAdminCatalog()`, `ShowAdminCatalogsManagement()`, `ShowAdminUsers()`, `ShowAdminComingSoon()`

**Correcciones aplicadas post-implementación:**
- `AdminMainView.axaml.cs` dispara `CheckConnectivityCommand` en `Loaded` (igual que `InventoryMainView`)
- `ShowAdminCatalogsManagement` acepta `initialTabIndex` para abrir en la pestaña correcta según tarjeta (0=Medidas, 1=Categorías)

**Tarjetas funcionales:** Catálogo, Medidas, Categorías, Usuarios  
**Tarjetas con placeholder:** Sucursales (Etapa 3), Proveedores (Etapa 4), Ent. y Salidas (Etapa 2), Hist. Cortes (Etapa 2), Existencias (Etapa 5)

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

**Endpoint API:** `POST/PUT /api/v1/admin/branches` — completamente implementado en el servidor ✅

**Flujo de guardado:**
El servicio llama a `ApiClient.PostAsync` o `PutAsync` contra `/api/v1/admin/branches`, recibe el objeto con el `id` real del servidor, guarda localmente con ese `id`, y marca `SyncStatus = 2` inmediatamente. No hay cola de pendientes ni `SyncStatus=1`.

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

**Endpoint API:** `POST/PUT /api/v1/admin/suppliers` — completamente implementado en el servidor ✅

**Flujo de guardado:** Igual que Sucursales — ApiClient directo, recibe `id` del servidor, guarda local, `SyncStatus = 2` inmediato.

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

**Endpoint API:** `GET /api/v1/admin/reports/product-stock?branch_id=X&page=Y` — implementado en `ReportController` ✅

**Requiere conexión activa** — no hay fallback local (consistente con la estrategia on-demand del módulo Admin).

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

## 8. Estrategia de Sincronización del Módulo Admin

### Principio general

Admin opera en modo **completamente on-demand** — todo requiere conexión activa al servidor. No hay operaciones que tengan sentido en modo offline porque:

- Un equipo dedicado a Admin puede no tener ningún dato local (sin ventas, sin entradas, sin historiales)
- Los datos maestros (productos, sucursales, usuarios) necesitan existir en el servidor para que otras terminales los reciban
- Las consultas de reportes (existencias, historial de cortes, entradas/salidas) solo viven en el servidor

### Estrategia por tipo de operación

| Operación | Estrategia | Sin conexión |
|---|---|---|
| Crear/modificar producto | Push on-demand inmediato | Bloquear — mostrar error |
| Crear/modificar categoría/unidad | Push on-demand inmediato | Bloquear — mostrar error |
| Crear/modificar sucursal | Push on-demand inmediato | Bloquear — mostrar error |
| Crear/modificar proveedor | Push on-demand inmediato | Bloquear — mostrar error |
| Crear/modificar usuario | Push on-demand (`SyncStatus=1` si falla) | Guarda local, sube después |
| Ver existencias de productos | Consulta directa al servidor + fallback caché | Caché local con aviso |
| Historial de cortes | Consulta directa al servidor | Mostrar error, sin fallback |
| Historial entradas/salidas | Consulta directa al servidor | Mostrar error, sin fallback |
| Existencias globales | Consulta directa al servidor | Mostrar error, sin fallback |

*Nota: Usuarios mantiene `SyncStatus=1` como excepción porque ya era así en `PushUserAsync` y es el comportamiento más seguro para no perder datos de usuarios recién creados.*

### Bloqueo de acceso al módulo sin conexión

**Decisión adoptada: bloquear en el punto de entrada (Opción A)**

El bloqueo ocurre en `App.axaml.cs` dentro del handler de `AdminSelected` en `ShowModuleSelector()`, **antes** de llamar a `ShowAdmin()`. Si no hay conexión al servidor, se muestra un diálogo de error con opción de reintentar. El módulo Admin nunca llega a abrirse sin conexión.

```
[Click "Administrador" en ModuleSelector]
    → Verificar conexión (IsServerAvailableAsync)
        → Sin conexión → Diálogo: "Sin conexión al servidor.
                                    El módulo Administrador requiere
                                    conexión activa."
                                    [Reintentar] [Cancelar]
            → Reintentar → Verifica de nuevo
            → Cancelar   → Vuelve al ModuleSelector sin hacer nada
        → Con conexión → ShowAdmin() normal
```

Esto es más limpio que deshabilitar las 9 tarjetas individualmente (como hace Inventario con algunas funciones), porque en Admin no hay ninguna función útil sin conexión.

**Nota importante para la Etapa 6 (PIN):** El check de conectividad debe ir **antes** de pedir el PIN. Flujo correcto: verificar conexión → pedir PIN → `ShowAdmin()`. No tiene sentido pedir el PIN si no hay conexión.

### Lectura de datos en Admin

Para listas y consultas (catálogo, historiales, existencias) Admin lee de la BD local SQLite que el `PullAllAsync` del login ya pobló. Esto es rápido y no requiere internet para navegar. Cuando Admin modifica algo, actualiza local inmediatamente después de la confirmación del servidor, para que la UI refleje el cambio sin esperar al próximo login.

Si el usuario quiere datos completamente frescos del servidor, usa el botón **Sincronizar** en `AppConfigView` (`PullCatalogFullAsync` con `since=0`). No se añaden botones de sync por vista individual.

---

## 9. Orden de Implementación

| # | Etapa | Estado |
|---|---|---|
| 1 | Menú principal del Admin | ✅ Completada |
| 2 | PIN de módulo (Etapa 6 en plan original) | ⬜ Siguiente |
| 3 | Bloqueo de acceso sin conexión en ModuleSelector | ⬜ Junto con PIN |
| 4 | Historial global de entradas/salidas y cortes | ⬜ Pendiente |
| 5 | CRUD Sucursales | ⬜ Pendiente |
| 6 | CRUD Proveedores | ⬜ Pendiente |
| 7 | Existencias globales por sucursal | ⬜ Pendiente |

El PIN y el bloqueo de acceso sin conexión van juntos porque ambos modifican el mismo punto: el handler de `AdminSelected` en `App.axaml.cs`. El bloqueo se implementa primero (verificar conexión), luego el PIN (verificar código), y finalmente `ShowAdmin()`.

---

## 10. Notas de Arquitectura para Implementación

### Patrón de navegación (App.axaml.cs)

Toda la navegación pasa por `App.axaml.cs`. El patrón es:
1. Crear ViewModel y View
2. Suscribir eventos del ViewModel
3. Para subvistas modales: `ShowDialog(parentWindow)`
4. Para ventanas de pantalla completa: `desktop.MainWindow = view; view.Show(); windowToClose?.Close()`
5. Al cerrar: usar `window.Tag` para determinar a dónde navegar

### Patrón de servicios

- Servicios nuevos de Admin (`BranchService`, `SupplierService`) usan `ApiClient` directamente — no `SyncService`
- Constructor recibe `ApiClient` + `BaseRepository<T>` para persistencia local
- Métodos retornan `(bool Success, string Message, T? Data)` para operaciones de escritura
- Flujo: llamar al servidor → recibir objeto con `id` real → guardar local con ese `id` → `SyncStatus = 2`
- Si el servidor falla → mostrar error → no guardar local (Admin no opera offline)
- **Excepción:** `UserService` con `isAdminMode` usa `/api/v1/admin/users` en lugar de `/api/v1/sync/push/users`

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

## 11. Pendientes y Decisiones Abiertas

- [ ] Verificar el estado actual del botón "Modificar" en `CatalogView` de Inventario — confirmar que en Inventario no se puede modificar productos (solo ver y dar alta)
- [ ] Decidir si se agrega el campo `Description` al modelo `Supplier` antes de la Etapa 6/Proveedores (requiere migración de BD)
- [ ] En la Etapa 5/Sucursales, decidir si "Ver inventario de sucursal" abre el `StockByBranchDialog` existente o una vista nueva de pantalla completa con paginación
- [ ] Diseño de la sección PIN en `AppConfigView` — debe integrarse naturalmente sin romper el layout actual
- [ ] El diseño visual de `UserManagementView` para el módulo Admin: actualmente funciona y es suficiente, pero se puede revisar en su momento si se quiere un diseño diferente

**Decisiones ya tomadas (referencia):**
- ✅ Estrategia de sync: todo on-demand, bloquear acceso completo si no hay conexión
- ✅ Bloqueo en punto de entrada (ModuleSelector), no dentro del módulo
- ✅ Usuarios mantienen SyncStatus=1 como excepción (ya implementado en PushUserAsync)
- ✅ Verificar conexión ANTES de pedir PIN (flujo: conexión → PIN → ShowAdmin)

---

*Documento de planificación — Casa Ceja Remake — Mayo 2026*  
*Actualizar estado de etapas conforme se completen. Para implementar, clonar el repo y verificar el estado actual del código antes de empezar.*

---

## 12. Estrategia de Endpoints: Admin vs Sync

### Contexto

La API tiene dos familias de endpoints para las mismas entidades:

```
/api/v1/sync/push/*   → Lotes, folios como clave, para POS e Inventario (offline → sync)
/api/v1/admin/*       → REST individual, CRUD inmediato, para el módulo Admin (on-demand)
```

El módulo Admin **siempre usa `/api/v1/admin/*`**, nunca los de sync.

### Estado de los controladores Admin en el servidor

Todos están **completamente implementados** — no hay código pendiente en el servidor para las operaciones CRUD básicas:

| Entidad | GET (list) | POST (create) | PUT (update) | DELETE | Extra |
|---|---|---|---|---|---|
| Productos | ✅ con filtros | ✅ | ✅ | ✅ | — |
| Categorías | ✅ | ✅ | ✅ | ✅ | — |
| Unidades | ✅ | ✅ | ✅ | ✅ | — |
| Sucursales | ✅ | ✅ | ✅ | ✅ | — |
| Proveedores | ✅ | ✅ | ✅ | ✅ | — |
| Usuarios | ✅ | ✅ | ✅ | ✅ | `PATCH /password` ✅ |
| Reportes | cashCloses ✅ | inventory ✅ | productStock ✅ | — | — |

Los campos de cada controlador coinciden exactamente con los modelos del cliente. No hay migraciones ni cambios de servidor necesarios para las etapas de implementación previstas.

### Implementación en el cliente: flag `isAdminMode`

Para reutilizar los servicios existentes sin duplicar lógica, cada servicio que tenga comportamiento diferente según el contexto recibirá un flag `isAdminMode` (mismo patrón que `UserManagementViewModel`). Según ese flag, ejecuta la ruta correcta:

```csharp
// Ejemplo en UserService
public async Task<(bool Success, string Message)> SaveUserAsync(User user, bool isAdminMode = false)
{
    if (isAdminMode)
    {
        // POST /api/v1/admin/users  o  PUT /api/v1/admin/users/{id}
        // Servidor primero → guarda local con el id del servidor
        // Contraseña en texto plano — el servidor hashea
    }
    else
    {
        // Guarda local con SyncStatus=1 → PushUserAsync → /api/v1/sync/push/users
        // Contraseña ya hasheada desde SQLite
    }
}
```

### Flujo de guardado en modo Admin (importante)

A diferencia de Inventario (local → servidor después), Admin hace:

```
Admin guarda → POST /api/v1/admin/{entidad} → servidor responde con {id: X, ...}
    → cliente guarda localmente con ese id del servidor
    → SyncStatus = 2 (synced) inmediatamente
```

Esto evita desincronización de IDs entre SQLite local y MySQL del servidor, lo que causaría duplicados en el siguiente `PullAllAsync`.

### Nota sobre contraseñas

- **Modo sync** (`/sync/push/users`): cliente envía password ya hasheada desde SQLite, servidor la guarda tal cual
- **Modo admin** (`/admin/users`): cliente envía password en texto plano, servidor hace `Hash::make()`. Al editar contraseña usar `PATCH /api/v1/admin/users/{id}/password`

### Autenticación

No requiere cambios. El `ApiClient` ya envía `X-User-Token` en cada request (guardado en `AppConfig.UserToken` al hacer login). El middleware `user.token` del servidor valida ese header para todas las rutas, incluyendo las de admin.

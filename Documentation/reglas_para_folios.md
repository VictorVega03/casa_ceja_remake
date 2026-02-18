# 🧾 Sistema de Folios — Casa Ceja Remake
## Documento Maestro de Definición e Implementación v2

> **Propósito:** Este documento define completamente el sistema de folios del proyecto Casa Ceja Remake.
> **Destinatario:** Claude en VSCode para generación de código.
> **Regla fundamental:** Nunca construir folios manualmente fuera del FolioService.
> **Versión:** 2 — Incluye análisis de unicidad, secuencial diario vs global, y validación de configuración.

---

## 1. FORMATO UNIVERSAL DE FOLIO

Todos los folios del sistema comparten **exactamente el mismo formato de 17 caracteres**, diferenciándose únicamente por la letra de tipo.

### Estructura

```
SS  CC  DD  MM  YYYY  T  ####
03  01  23  01  2026  V  0001
```

| Segmento | Posición | Longitud | Descripción | Ejemplo |
|---|---|---|---|---|
| SS | 0–1 | 2 dígitos | ID de sucursal, con padding de ceros | 03 |
| CC | 2–3 | 2 dígitos | Número de caja/POS, con padding de ceros | 01 |
| DD | 4–5 | 2 dígitos | Día del mes, con padding de ceros | 23 |
| MM | 6–7 | 2 dígitos | Mes del año, con padding de ceros | 01 |
| YYYY | 8–11 | 4 dígitos | Año completo sin abreviar | 2026 |
| T | 12 | 1 letra mayúscula | Tipo de transacción | V |
| #### | 13–16 | 4 dígitos | Número secuencial con padding de ceros | 0001 |

**Longitud total invariable: 17 caracteres.**

### Ejemplo desglosado

`030123012026V0001` se interpreta como:
- SS = 03 → Sucursal número 3
- CC = 01 → Caja número 1
- DD = 23 → Día 23
- MM = 01 → Mes enero
- YYYY = 2026 → Año 2026
- T = V → Tipo Venta
- #### = 0001 → Primera venta del día en esa caja

---

## 2. TABLA DE TIPOS DE TRANSACCIÓN

| Letra | Tipo | Descripción | Comportamiento del secuencial |
|---|---|---|---|
| V | Venta | Venta directa completada | Reinicia a 0001 cada día |
| A | Apartado | Registro de nuevo apartado | Reinicia a 0001 cada día |
| C | Crédito | Registro de nuevo crédito | Reinicia a 0001 cada día |
| P | Pago | Abono a un apartado o crédito | Reinicia a 0001 cada día |
| X | Corte | Corte de caja | Global, nunca reinicia |

### Notas sobre cada tipo

**Tipo V (Venta):** Se genera al confirmar y procesar la venta. Una venta cancelada conserva su folio original; ese número nunca se reutiliza.

**Tipo A (Apartado):** Se genera al registrar el apartado. Los abonos posteriores a ese apartado generan folios P, no nuevos folios A.

**Tipo C (Crédito):** Se genera al crear el crédito. Los pagos posteriores generan folios P.

**Tipo P (Pago):** Se genera cada vez que se registra un abono, ya sea a un apartado o a un crédito. El registro guarda su folio P más el folio del padre al que corresponde. Esto permite trazabilidad completa en ambas direcciones: desde el pago al apartado/crédito y desde el apartado/crédito a todos sus pagos.

**Tipo X (Corte):** Se genera al abrir el corte, no al cerrarlo. El folio queda registrado desde la apertura y refleja el día en que inició el período de caja. Es el único tipo cuyo secuencial NO reinicia por día; crece globalmente durante toda la vida del sistema.

---

## 3. REGLAS DEL SECUENCIAL (####)

### Para tipos V, A, C y P — Secuencial Diario

El secuencial reinicia a 0001 al comenzar cada día calendario. La unicidad no depende exclusivamente del secuencial porque la fecha completa ya está embebida en el folio. Dos folios del mismo tipo pero de días distintos son siempre diferentes aunque tengan el mismo secuencial, porque DD+MM+YYYY difiere.

**Lógica para calcular el siguiente secuencial:**

1. Tomar la fecha actual del sistema
2. Construir el prefijo: SS + CC + DD + MM + YYYY + T (13 caracteres)
3. Buscar en la tabla correspondiente todos los folios que comiencen exactamente con ese prefijo, filtrados por rango del día actual (00:00:00 a 23:59:59)
4. Tomar el mayor valor de los últimos 4 caracteres convertido a entero
5. Si no hay folios para ese prefijo hoy: el siguiente secuencial es 1 (se formatea como 0001)
6. Si hay folios: el siguiente es el mayor encontrado más uno

El secuencial es completamente independiente por cada combinación de sucursal + caja + día + tipo. Caja 01 y Caja 02 de la misma sucursal tienen secuenciales independientes entre sí.

### Para tipo X — Secuencial Global

El secuencial del corte crece de forma continua sin reiniciarse nunca. No importa cuántos días pasen ni cuántas sucursales existan.

**Lógica para calcular el siguiente secuencial:**

1. Buscar en la tabla de cortes el valor máximo de los últimos 4 caracteres del campo folio_corte en todos los registros, sin ningún filtro de fecha, sucursal o estado
2. Si la tabla está vacía: el siguiente secuencial es 1 (se formatea como 0001)
3. Si hay registros: el siguiente es el máximo encontrado más uno

**Capacidad:** 9,999 cortes globales en toda la vida del sistema. Si en el futuro se requiere más capacidad, se amplía el campo a 5 dígitos y el folio pasa a 18 caracteres. Este cambio es completamente localizado en FolioService y no afecta ninguna otra parte del sistema.

---

## 4. GARANTÍA DE UNICIDAD — ANÁLISIS COMPLETO

Este sistema garantiza que **nunca existirán dos folios idénticos** bajo ninguna operación normal. A continuación se explica por qué, capa por capa.

### Las cinco barreras de unicidad

Para que dos folios sean idénticos los 17 caracteres deben coincidir exactamente. Cada segmento actúa como barrera independiente:

**Barrera 1 — SS (Sucursal):** Dos operaciones en sucursales distintas siempre producen folios distintos. Sucursal 03 nunca colisiona con sucursal 07.

**Barrera 2 — CC (Caja):** Dos operaciones en la misma sucursal pero en cajas distintas siempre producen folios distintos. Caja 01 nunca colisiona con Caja 02 dentro de la misma sucursal.

**Barrera 3 — DD+MM+YYYY (Fecha):** La misma caja en días distintos siempre produce folios distintos aunque el secuencial haya reiniciado. El folio del día 23 y el del día 24 nunca colisionan aunque ambos tengan secuencial 0001 porque la fecha embebida difiere.

**Barrera 4 — T (Tipo):** Una venta y un apartado del mismo día, misma caja y mismo secuencial son folios distintos porque T difiere. `030123012026V0001` ≠ `030123012026A0001`.

**Barrera 5 — #### (Secuencial atómico):** Dentro del mismo prefijo SS+CC+DD+MM+YYYY+T, el secuencial se genera de forma atómica bajo SemaphoreSlim. Nunca dos operaciones pueden obtener el mismo número secuencial.

### Conclusión matemática

Dos folios son idénticos SOLO SI ocurren en la misma sucursal, la misma caja, el mismo día, el mismo tipo Y tienen el mismo número secuencial. El SemaphoreSlim hace esto imposible en código. El constraint UNIQUE lo hace imposible en base de datos. Ambas protecciones actúan en capas independientes.

### Las tres capas de protección en código

**Capa 1 — Diseño del folio:** La estructura de 17 caracteres con 5 segmentos diferenciadores hace matemáticamente imposible la colisión entre operaciones legítimas distintas.

**Capa 2 — SemaphoreSlim en FolioService:** Campo estático con capacidad 1. Solo un hilo puede generar un folio a la vez. Todo el bloque de consulta + cálculo + construcción ocurre dentro del semáforo. El bloque finally garantiza que el semáforo siempre se libere aunque ocurra una excepción.

**Capa 3 — UNIQUE constraint en SQLite:** Si por cualquier circunstancia extraordinaria llegara un folio duplicado a la base de datos, SQLite lo rechaza antes de insertar. Esta es la última línea de defensa que nunca debería activarse si las capas anteriores funcionan correctamente.

---

## 5. VALIDACIÓN DE CONFIGURACIÓN — REGLA CRÍTICA

### El único riesgo real del sistema

El único escenario que puede romper la unicidad no es un fallo de código sino un error de configuración: dos computadoras distintas configuradas con el mismo número de sucursal Y el mismo número de caja operando el mismo día.

| Máquina | Sucursal | Caja | Folio generado |
|---|---|---|---|
| PC Caja 1 | 03 | 01 | `030123012026V0001` |
| PC Caja 2 — MAL CONFIGURADA | 03 | 01 | `030123012026V0001` ← COLISIÓN |

Este escenario es imposible de detectar a nivel de FolioService porque cada máquina opera con su propia base de datos local. La protección debe ocurrir en ConfigService antes de guardar la configuración.

### Qué debe hacer ConfigService al guardar la configuración

Cuando el administrador configura o cambia el número de caja, ConfigService debe ejecutar esta validación antes de permitir el guardado:

1. Consultar la tabla de cortes buscando registros cuyo folio_corte comience con la combinación SS+CC que se intenta configurar (primeros 4 caracteres del folio)
2. Si se encuentran cortes con esa combinación: mostrar advertencia indicando que esa combinación de sucursal y caja ya ha sido utilizada en este dispositivo y que asignarla a otro POS podría generar folios duplicados
3. Si no hay conflictos: permitir el guardado normalmente

La documentación de instalación del sistema debe indicar claramente que cada instalación debe tener un número de caja único dentro de su sucursal.

### Validación al iniciar la aplicación

Al arrancar, antes de crear o cargar el corte, el sistema debe verificar que la configuración sea operativamente válida:

1. Leer sucursalId y cajaId desde la configuración local
2. Verificar que sucursalId sea un entero entre 1 y 99
3. Verificar que cajaId sea un entero entre 1 y 99
4. Si algún valor no es válido: mostrar pantalla de configuración inicial y bloquear la operación hasta que se configure correctamente

---

## 6. LÓGICA DE CORTES — REGLAS DE NEGOCIO CRÍTICAS

### 6.1 Al iniciar la aplicación (después del login)

Secuencia obligatoria al cargar el módulo principal:

**Paso 1:** Leer configuración local para obtener sucursalId y cajaId activos.

**Paso 2:** Ejecutar validación de configuración (sección 5). Si no es válida, detener el flujo.

**Paso 3:** Buscar en la tabla de cortes si existe un corte con estado = 0 (abierto) donde sucursal_id coincida Y caja_id coincida con la configuración actual.

**Paso 4:** Evaluar el resultado:
- Si existe corte abierto: cargarlo y continuar. No importa la fecha de apertura, puede ser de días anteriores.
- Si no existe corte abierto: crear uno nuevo, generar folio tipo X con FolioService, y continuar.

### 6.2 Escenario: Corte abierto de días anteriores (operación normal y común)

Es práctica frecuente en las sucursales no hacer corte durante varios días consecutivos. El sistema debe manejar esto sin advertencias ni bloqueos:

- El corte abierto se carga normalmente aunque sea de hace 3, 5 o más días.
- Las ventas del día actual se siguen acumulando bajo ese mismo corte.
- El folio del corte refleja el día en que se abrió, lo cual es correcto y representa el inicio del período contable de esa caja.
- No se genera advertencia, no se crea un corte nuevo ni se modifica el folio existente.

### 6.3 Escenario: Cambio de sucursal con corte abierto (BLOQUEANTE)

Si el usuario intenta cambiar la sucursal configurada y existe un corte abierto:

- El sistema bloquea el cambio completamente.
- Se muestra un mensaje claro indicando que hay un corte abierto que debe cerrarse antes de operar en otra sucursal.
- No hay forma de forzar el cambio sin hacer el corte primero.
- Esta es la única situación en que el sistema bloquea activamente al usuario.

La razón: el folio del corte ya tiene el ID de la sucursal anterior embebido. Mezclar ventas de dos sucursales bajo el mismo corte corrompería los datos contables.

### 6.4 Escenario: Cambio de sucursal sin corte abierto

Si no hay corte abierto, el cambio de sucursal es libre. Al reiniciar el flujo de inicio con la nueva configuración el sistema crea automáticamente un nuevo corte para la nueva sucursal y caja.

### 6.5 Al ejecutar el Corte Z

Cuando el usuario confirma y ejecuta el corte:

1. Se actualizan todos los campos del corte: totales por método de pago, fecha de cierre, usuario, sobrante.
2. El estado cambia de 0 (abierto) a 1 (cerrado).
3. La aplicación se cierra completamente (comportamiento del sistema original que se conserva).
4. Al reabrir, el sistema detecta que no hay corte abierto y crea uno nuevo reiniciando el ciclo.

### 6.6 Búsqueda de corte abierto por sucursal y caja

La búsqueda debe hacerse simultáneamente por dos condiciones:
- sucursal_id igual al ID de sucursal configurado (columna existente)
- caja_id igual al número de caja configurado (columna nueva que debe agregarse a la tabla de cortes)

No se filtra por fecha. Ver sección 9 para los cambios de base de datos requeridos.

---

## 7. SISTEMA DE PAGOS (TIPO P)

### Relación padre-hijo

Cada registro de pago debe almacenar:
- Su propio folio único tipo P
- El folio del apartado o crédito al que pertenece (folio_padre)
- El tipo del padre: valor "A" para apartado o "C" para crédito

Esto permite navegar la relación en ambas direcciones: desde un pago llegar al apartado o crédito padre, y desde un apartado o crédito listar todos sus pagos históricos.

### Tabla unificada de pagos (recomendada)

En lugar de mantener tablas separadas para abonos de apartados y abonos de créditos, se recomienda una sola tabla Payments con columna tipo_padre. Esta decisión reduce duplicación de código, simplifica las queries de reportes y hace más directa la generación de folios tipo P.

Si por razones de migración se decide mantener tablas separadas, FolioService genera igualmente el folio tipo P en ambos casos. La lógica del folio no cambia según la estructura de tablas elegida.

### Qué incluye el ticket de pago

- Folio P del pago (identificador único de este abono)
- Folio del padre (apartado o crédito al que se abona)
- Métodos de pago utilizados y montos por método
- Total abonado en esta operación
- Saldo restante por pagar
- Fecha, hora y cajero

---

## 8. DETALLE DE IMPLEMENTACIÓN — FolioService

**Ubicación:** `Services/FolioService.cs`

### Campos requeridos

- Campo privado readonly de tipo DatabaseService
- Campo privado estático de tipo SemaphoreSlim inicializado con SemaphoreSlim(1, 1). Debe ser estático para que sea compartido entre todas las instancias del servicio en el proceso.

### Constructor

Recibe DatabaseService por inyección de dependencia y lo asigna al campo privado.

### Métodos públicos

**GenerarFolioVentaAsync(int sucursalId, int cajaId):** Retorna Task de string. Delega a GenerarFolioAsync con tipo "V".

**GenerarFolioApartadoAsync(int sucursalId, int cajaId):** Retorna Task de string. Delega a GenerarFolioAsync con tipo "A".

**GenerarFolioCreditoAsync(int sucursalId, int cajaId):** Retorna Task de string. Delega a GenerarFolioAsync con tipo "C".

**GenerarFolioPagoAsync(int sucursalId, int cajaId):** Retorna Task de string. Delega a GenerarFolioAsync con tipo "P".

**GenerarFolioCorteAsync(int sucursalId, int cajaId):** Retorna Task de string. Delega a GenerarFolioCorteInternoAsync que usa secuencial global.

**ParsearFolio(string folio):** Método sincrónico. Valida que el folio tenga exactamente 17 caracteres; si no, lanza ArgumentException con mensaje descriptivo. Extrae y retorna en un objeto o tupla: sucursalId (pos 0–1), cajaId (pos 2–3), dia (pos 4–5), mes (pos 6–7), anio (pos 8–11), tipo (pos 12), secuencial (pos 13–16).

### Métodos privados

**GenerarFolioAsync(int sucursalId, int cajaId, string tipo):**

Secuencia obligatoria e invariable:
1. Llamar a `await _semaphore.WaitAsync()`
2. Abrir bloque `try`
3. Obtener `DateTime.Now`
4. Calcular `fechaInicio`: misma fecha a las 00:00:00.000
5. Calcular `fechaFin`: misma fecha a las 23:59:59.999
6. Llamar a `ObtenerUltimoSecuencialDiarioAsync(sucursalId, cajaId, tipo, fechaInicio, fechaFin)`
7. Calcular `nuevoSecuencial = resultado + 1`
8. Construir el folio concatenando: `sucursalId.ToString().PadLeft(2,'0')` + `cajaId.ToString().PadLeft(2,'0')` + `ahora.Day.ToString().PadLeft(2,'0')` + `ahora.Month.ToString().PadLeft(2,'0')` + `ahora.Year.ToString()` + `tipo` + `nuevoSecuencial.ToString().PadLeft(4,'0')`
9. Llamar a `ExisteFolioAsync(folio)`
10. Si existe: llamar recursivamente a `GenerarFolioAsync` y retornar ese resultado
11. Si no existe: retornar el folio construido
12. Bloque `finally`: llamar a `_semaphore.Release()` sin ninguna condición

**GenerarFolioCorteInternoAsync(int sucursalId, int cajaId):**

Igual a GenerarFolioAsync pero con secuencial global:
1. Adquirir semáforo
2. Obtener `DateTime.Now`
3. Llamar a `ObtenerUltimoSecuencialGlobalCorteAsync()` (sin parámetros de fecha ni sucursal)
4. Calcular `nuevoSecuencial = resultado + 1`
5. Construir folio con tipo "X"
6. Verificar unicidad con `ExisteFolioAsync`
7. Retornar o reintentar
8. Liberar semáforo en `finally`

**ObtenerUltimoSecuencialDiarioAsync(int sucursalId, int cajaId, string tipo, DateTime fechaInicio, DateTime fechaFin):**

1. Determinar la tabla a consultar según tipo: "V" → Sales, "A" → Layaways, "C" → Credits, "P" → Payments
2. Construir el prefijo del folio: los mismos primeros 13 caracteres que generaría el folio para esa combinación de parámetros
3. Ejecutar query: buscar folios que comiencen con ese prefijo Y cuya columna de fecha esté entre fechaInicio y fechaFin, ordenar descendente, LIMIT 1
4. Si no hay resultado: retornar 0
5. Si hay resultado: extraer `folio.Substring(13, 4)`, convertir a int y retornar

**ObtenerUltimoSecuencialGlobalCorteAsync():**

1. Ejecutar query en la tabla de cortes: obtener todos los valores del campo folio_corte donde la longitud sea 17 caracteres
2. Extraer los últimos 4 caracteres de cada folio y encontrar el máximo como entero
3. Retornar ese máximo, o 0 si la tabla está vacía o no hay registros válidos
4. Sin filtro de fecha, sucursal, caja ni estado

**ExisteFolioAsync(string folio):**

Verificar la existencia del folio en cada una de estas tablas de forma secuencial: Sales, Layaways, Credits, Cuts, Payments. Retornar `true` en cuanto se encuentre en cualquiera. Retornar `false` solo si no existe en ninguna.

---

## 9. CAMBIOS EN LA BASE DE DATOS

### Regla general para todas las tablas de transacciones

La columna Folio en Sales, Layaways, Credits y Payments debe cumplir:
- Tipo: TEXT
- Restricción: NOT NULL
- Restricción: UNIQUE
- Índice de búsqueda propio para cada tabla

Índices requeridos por tabla:
- `CREATE UNIQUE INDEX idx_sales_folio ON Sales(Folio)`
- `CREATE UNIQUE INDEX idx_layaways_folio ON Layaways(Folio)`
- `CREATE UNIQUE INDEX idx_credits_folio ON Credits(Folio)`
- `CREATE UNIQUE INDEX idx_cuts_folio ON Cuts(folio_corte)`
- `CREATE UNIQUE INDEX idx_payments_folio ON Payments(Folio)`
- `CREATE INDEX idx_payments_folio_parent ON Payments(FolioParent)`

Índices en columnas de fecha para optimizar las queries de secuencial diario:
- `CREATE INDEX idx_sales_date ON Sales(FechaVenta)`
- `CREATE INDEX idx_layaways_date ON Layaways(FechaRegistro)`
- `CREATE INDEX idx_credits_date ON Credits(FechaRegistro)`

### Columna adicional en tabla de cortes

Agregar columna `caja_id INTEGER NOT NULL DEFAULT 1` a la tabla Cuts. Esta columna es necesaria para filtrar el corte abierto por caja sin depender de parsear el folio.

Índice compuesto para la búsqueda de corte abierto:
- `CREATE INDEX idx_cuts_branch_register ON Cuts(sucursal_id, caja_id, estado)`

### Tabla Payments — si se implementa tabla unificada

Columnas mínimas requeridas:
- Id: INTEGER PRIMARY KEY AUTOINCREMENT
- Folio: TEXT NOT NULL UNIQUE
- FolioParent: TEXT NOT NULL
- TipoPadre: TEXT NOT NULL — valor "A" o "C"
- MetodoPago: TEXT — JSON con métodos y montos
- TotalAbonado: REAL NOT NULL
- FolioCorte: TEXT NOT NULL
- UsuarioId: INTEGER NOT NULL
- Fecha: TEXT NOT NULL

---

## 10. REGISTRO EN App.axaml.cs

El archivo principal de la aplicación debe exponer como propiedades estáticas públicas de solo lectura:

**DatabaseService:** Instancia única inicializada primero en el método Initialize() o OnFrameworkInitializationCompleted().

**FolioService:** Instancia única inicializada inmediatamente después de DatabaseService, construida pasándole el DatabaseService como parámetro.

Acceso desde cualquier parte: `App.DatabaseService` y `App.FolioService`. No crear instancias adicionales de estos servicios en ningún ViewModel o repositorio.

---

## 11. SERVICIOS ADICIONALES REQUERIDOS

### CortesService — `Services/CortesService.cs`

**BuscarCorteAbiertoAsync(int sucursalId, int cajaId):**
Query: `SELECT * FROM Cuts WHERE estado = 0 AND sucursal_id = @suc AND caja_id = @caja ORDER BY Id DESC LIMIT 1`. Sin filtro de fecha. Retorna el objeto corte si existe, null si no hay ninguno abierto.

**CrearNuevoCorteAsync(int sucursalId, int cajaId, double montoApertura):**
Llama internamente a `App.FolioService.GenerarFolioCorteAsync(sucursalId, cajaId)`. Inserta el nuevo corte en la base de datos con estado = 0. Retorna el objeto corte creado.

**CerrarCorteAsync(int idCorte, Dictionary datos):**
Actualiza el corte con totales finales, fecha de cierre y estado = 1.

**ValidarCambioSucursalAsync(int sucursalId, int cajaId):**
Llama a BuscarCorteAbiertoAsync. Retorna `true` si no hay corte abierto (cambio permitido). Retorna `false` si hay corte abierto (cambio bloqueado).

### ConfigService — `Services/ConfigService.cs`

Maneja el archivo config.json ubicado según el sistema operativo:
- Windows: `%AppData%\CasaCeja\config.json`
- macOS: `~/Library/Application Support/CasaCeja/config.json`
- Linux: `~/.config/CasaCeja/config.json`

Propiedades que gestiona: SucursalId, CajaId, NombreImpresora, ModuloDefault, UltimaSincronizacion.

**ValidarConfiguracion():** Verifica que SucursalId y CajaId sean enteros entre 1 y 99. Retorna bool.

**ValidarConfiguracionCajaAsync(int sucursalId, int cajaId):**
Busca en la tabla de cortes si existen registros cuyos primeros 4 caracteres del folio coincidan con la combinación SS+CC que se intenta configurar. Si existen y la base de datos local ya los registró: retorna false con mensaje de advertencia de posible colisión. Si no existen conflictos: retorna true.

---

## 12. FLUJO COMPLETO AL INICIAR SESIÓN

Secuencia obligatoria después del login exitoso:

1. Llamar a `ConfigService.ObtenerConfiguracionAsync()`
2. Llamar a `ConfigService.ValidarConfiguracion()`
3. Si configuración no válida: mostrar pantalla de configuración inicial y detener el flujo
4. Llamar a `CortesService.BuscarCorteAbiertoAsync(sucursalId, cajaId)`
5. Si retorna corte: almacenarlo en el estado global de la aplicación y navegar al módulo principal
6. Si retorna null: llamar a `CortesService.CrearNuevoCorteAsync(sucursalId, cajaId, montoApertura)`, almacenar el nuevo corte en el estado global y navegar al módulo principal

El folio del corte activo debe quedar accesible desde el estado global para que todas las ventas, apartados, créditos y pagos puedan referenciarlo al momento de registrarse.

---

## 13. FLUJO DE CAMBIO DE SUCURSAL O CAJA

Secuencia cuando el administrador intenta modificar SucursalId o CajaId en la configuración:

1. Obtener configuración actual
2. Llamar a `CortesService.ValidarCambioSucursalAsync(sucursalIdActual, cajaIdActual)`
3. Si retorna false: mostrar mensaje "Existe un corte de caja abierto. Debe cerrarse antes de cambiar la configuración de sucursal o caja." No continuar.
4. Si retorna true: llamar a `ConfigService.ValidarConfiguracionCajaAsync(nuevoSucursalId, nuevoCajaId)`
5. Si retorna false: mostrar advertencia sobre posible colisión de folios y solicitar confirmación explícita del administrador
6. Guardar la nueva configuración
7. Reiniciar el flujo de inicio con los nuevos valores

---

## 14. PATRONES DE USO EN VIEWMODELS

### Patrón correcto

```
var folio = await App.FolioService.GenerarFolioVentaAsync(sucursalId, cajaId);
venta.Folio = folio;
await _ventasRepository.InsertarAsync(venta);
```

### Lo que NUNCA debe hacerse

- Nunca construir un folio concatenando strings directamente en un ViewModel, View o Repository
- Nunca usar el último ID de AUTOINCREMENT de una tabla como secuencial
- Nunca usar DateTime.Now directamente para construir el folio sin pasar por FolioService
- Nunca crear una instancia de FolioService fuera de App.axaml.cs
- Nunca acceder a los métodos privados de FolioService desde fuera del servicio

---

## 15. TABLA MAESTRA DE SERVICIOS

| Servicio | Responsabilidad | Archivo |
|---|---|---|
| FolioService | Generación de todos los folios del sistema | Services/FolioService.cs |
| CortesService | Lógica de negocio de cortes de caja | Services/CortesService.cs |
| ConfigService | Configuración local y validación de caja | Services/ConfigService.cs |
| DatabaseService | Conexión y acceso a SQLite | Data/DatabaseService.cs |

---

## 16. CHECKLIST COMPLETO DE IMPLEMENTACIÓN

### Base de datos
- [ ] Constraint UNIQUE NOT NULL en Folio de Sales
- [ ] Constraint UNIQUE NOT NULL en Folio de Layaways
- [ ] Constraint UNIQUE NOT NULL en Folio de Credits
- [ ] Constraint UNIQUE NOT NULL en folio_corte de Cuts
- [ ] Constraint UNIQUE NOT NULL en Folio de Payments
- [ ] Columna caja_id agregada a tabla Cuts
- [ ] Índice idx_sales_folio
- [ ] Índice idx_layaways_folio
- [ ] Índice idx_credits_folio
- [ ] Índice idx_cuts_folio
- [ ] Índice idx_payments_folio
- [ ] Índice idx_payments_folio_parent
- [ ] Índice compuesto idx_cuts_branch_register (sucursal_id + caja_id + estado)
- [ ] Índices en columnas de fecha de Sales, Layaways, Credits

### FolioService
- [ ] Campo privado readonly DatabaseService
- [ ] Campo estático SemaphoreSlim(1,1)
- [ ] Constructor con inyección de DatabaseService
- [ ] GenerarFolioVentaAsync público
- [ ] GenerarFolioApartadoAsync público
- [ ] GenerarFolioCreditoAsync público
- [ ] GenerarFolioPagoAsync público
- [ ] GenerarFolioCorteAsync público (usa secuencial global)
- [ ] ParsearFolio público sincrónico con validación de 17 caracteres
- [ ] GenerarFolioAsync privado con semáforo, try y finally obligatorio
- [ ] GenerarFolioCorteInternoAsync privado con secuencial global
- [ ] ObtenerUltimoSecuencialDiarioAsync privado
- [ ] ObtenerUltimoSecuencialGlobalCorteAsync privado (sin filtros)
- [ ] ExisteFolioAsync privado que consulta todas las tablas

### CortesService
- [ ] BuscarCorteAbiertoAsync filtra por sucursal_id Y caja_id, sin filtro de fecha
- [ ] CrearNuevoCorteAsync usa App.FolioService.GenerarFolioCorteAsync
- [ ] CerrarCorteAsync actualiza estado a 1 y registra fecha de cierre
- [ ] ValidarCambioSucursalAsync retorna bool

### ConfigService
- [ ] ObtenerConfiguracionAsync lee y deserializa archivo JSON
- [ ] GuardarConfiguracionAsync serializa y escribe archivo JSON
- [ ] ValidarConfiguracion verifica rangos válidos (1–99)
- [ ] ValidarConfiguracionCajaAsync consulta historial de cortes para detectar colisión

### App.axaml.cs
- [ ] Propiedad estática DatabaseService
- [ ] Propiedad estática FolioService
- [ ] Inicialización de DatabaseService primero
- [ ] Inicialización de FolioService pasando DatabaseService

### ViewModels
- [ ] LoginViewModel o MainViewModel ejecuta flujo completo de inicio con corte
- [ ] VentasViewModel usa App.FolioService.GenerarFolioVentaAsync
- [ ] ApartadosViewModel usa App.FolioService.GenerarFolioApartadoAsync
- [ ] CreditosViewModel usa App.FolioService.GenerarFolioCreditoAsync
- [ ] PagosViewModel usa App.FolioService.GenerarFolioPagoAsync
- [ ] ConfiguracionViewModel llama a CortesService.ValidarCambioSucursalAsync antes de guardar
- [ ] ConfiguracionViewModel llama a ConfigService.ValidarConfiguracionCajaAsync antes de guardar

### Pruebas de unicidad
- [ ] Folio de venta tiene exactamente 17 caracteres
- [ ] Folio de corte tiene exactamente 17 caracteres con letra X en posición 12
- [ ] Folio de pago tiene exactamente 17 caracteres con letra P en posición 12
- [ ] Secuencial de ventas reinicia a 0001 al día siguiente
- [ ] Secuencial de corte NO reinicia al día siguiente, continúa incrementando globalmente
- [ ] Caja 01 y Caja 02 generan secuenciales independientes el mismo día
- [ ] Al iniciar sin corte abierto se crea uno nuevo automáticamente
- [ ] Al iniciar con corte abierto de días anteriores se carga sin advertencia ni error
- [ ] Cambio de sucursal con corte abierto muestra mensaje y bloquea el cambio
- [ ] Cambio de sucursal sin corte abierto procede sin bloqueo
- [ ] SQLite rechaza inserción de folio duplicado con error de constraint UNIQUE
- [ ] ParsearFolio extrae correctamente todos los componentes de un folio válido de 17 chars
- [ ] ParsearFolio lanza excepción descriptiva para folios con longitud incorrecta

---

## 17. TABLA DE EJEMPLOS DE FOLIOS

| Escenario | Folio | Nota |
|---|---|---|
| Primera venta del día — suc 03, caja 01, 23 ene 2026 | 030123012026V0001 | Primer secuencial del día |
| Segunda venta — mismo día y caja | 030123012026V0002 | Secuencial incrementado |
| Primera venta del día siguiente — misma caja | 030124012026V0001 | Fecha cambia, secuencial reinicia a 0001 |
| Primer apartado — mismo día y caja | 030123012026A0001 | Tipo A, secuencial propio independiente de V |
| Primer crédito — mismo día y caja | 030123012026C0001 | Tipo C, secuencial propio |
| Primer pago del día | 030123012026P0001 | Tipo P, secuencial diario propio |
| Segundo pago del día | 030123012026P0002 | Tipo P incrementado |
| Corte número 7 del sistema (global) | 030123012026X0007 | Secuencial 7 acumulado del sistema |
| Siguiente corte al día siguiente | 030124012026X0008 | Fecha cambia pero secuencial continúa en 8 |
| Misma sucursal — Caja 02, mismo día | 030223012026V0001 | CC=02, secuencial completamente independiente |
| Sucursal 07, caja 02, 1 feb 2026 | 070201022026V0001 | Diferente sucursal, totalmente independiente |

---

## 18. DIFERENCIA CON EL SISTEMA LEGACY

Esta sección es solo informativa para entender qué cambió respecto al sistema original.

El sistema legacy usaba el ID AUTOINCREMENT de la tabla como secuencial. Ese ID nunca reiniciaba y crecía globalmente. No había número de caja en el folio. El corte tampoco incluía número de caja.

El remake agrega el segmento CC (caja) en posiciones 2–3, lo que permite múltiples cajas en la misma sucursal sin riesgo de colisión. El secuencial para V, A, C y P reinicia por día porque la fecha ya está embebida y garantiza unicidad de todas formas. El corte mantiene secuencial global como en el legacy. Se agrega la letra de tipo en posición 12 para que el folio sea autoexplicativo.

La unicidad está igual de garantizada que en el legacy. El remake es adicionalmente más informativo, más trazable y elimina el bug de concurrencia del sistema original.

---

*Documento generado: Febrero 2026 — Versión 2*
*Proyecto: Casa Ceja Remake — .NET 8, Avalonia, SQLite*
*Stack: C# / Avalonia UI / SQLite / Dapper*
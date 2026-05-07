# Inventario — Funcionamiento actual del cliente (casa_ceja_remake)

> Documento generado el 2026-04-13 para contrastar con el diseño del servidor.
> Describe exactamente cómo funciona el módulo de inventario en el cliente .NET/Avalonia.

---

## Tablas SQLite locales

### `stock_entries`
Registra cada entrada de mercancía, ya sea por compra a proveedor o por traspaso recibido.

| Columna | Tipo | Notas |
|---|---|---|
| `id` | INTEGER PK AUTOINCREMENT | Local only |
| `folio` | TEXT UNIQUE | Identificador de negocio, formato `{branch}{date}E{seq}` |
| `folio_output` | TEXT NULL | Folio de la salida origen cuando es TRANSFER. NULL en PURCHASE |
| `branch_id` | INTEGER | Sucursal que recibe la mercancía |
| `supplier_id` | INTEGER | 0 cuando es TRANSFER (no hay proveedor) |
| `user_id` | INTEGER | Usuario que registró |
| `total_amount` | DECIMAL | Suma de `line_total` de todos los productos |
| `entry_date` | DATETIME | Fecha del documento (editable por el usuario) |
| `entry_type` | TEXT | `PURCHASE` o `TRANSFER` |
| `notes` | TEXT NULL | Observaciones |
| `confirmed_by_user_id` | INTEGER NULL | Quién confirmó la recepción (solo TRANSFER) |
| `confirmed_at` | DATETIME NULL | Cuándo se confirmó (solo TRANSFER) |
| `created_at` | DATETIME | Timestamp local de creación |
| `updated_at` | DATETIME | Timestamp local de última modificación |
| `sync_status` | INTEGER | `1` = pendiente de subir, `2` = sincronizado con servidor |
| `last_sync` | DATETIME NULL | Cuándo se sincronizó con el servidor |

### `entry_products`
Detalle de productos por entrada.

| Columna | Tipo | Notas |
|---|---|---|
| `id` | INTEGER PK AUTOINCREMENT | |
| `entry_id` | INTEGER FK → stock_entries.id | |
| `product_id` | INTEGER | Referencia al catálogo universal |
| `barcode` | TEXT | Snapshot del código de barras al momento de la entrada |
| `product_name` | TEXT | Snapshot del nombre al momento de la entrada |
| `quantity` | INTEGER | Cantidad recibida (o confirmada si es TRANSFER) |
| `unit_cost` | DECIMAL | Costo unitario al momento de la entrada |
| `line_total` | DECIMAL | `quantity × unit_cost` |
| `created_at` | DATETIME | |

### `stock_outputs`
Registra cada salida/traspaso enviado a otra sucursal.

| Columna | Tipo | Notas |
|---|---|---|
| `id` | INTEGER PK AUTOINCREMENT | |
| `folio` | TEXT UNIQUE | Formato `{branch}{date}S{seq}` |
| `origin_branch_id` | INTEGER | Sucursal que envía |
| `destination_branch_id` | INTEGER | Sucursal que debe recibir |
| `user_id` | INTEGER | Usuario que registró |
| `total_amount` | DECIMAL | |
| `output_date` | DATETIME | Fecha del documento |
| `notes` | TEXT NULL | |
| `status` | TEXT | `PENDING` al crear, `CONFIRMED` cuando la destino confirma |
| `confirmed_by_user_id` | INTEGER NULL | Usuario que confirmó en destino |
| `confirmed_at` | DATETIME NULL | Fecha de confirmación |
| `created_at` | DATETIME | |
| `updated_at` | DATETIME | |
| `sync_status` | INTEGER | `1` = pendiente, `2` = sincronizado |
| `last_sync` | DATETIME NULL | |

### `output_products`
Detalle de productos por salida.

| Columna | Tipo | Notas |
|---|---|---|
| `id` | INTEGER PK AUTOINCREMENT | |
| `output_id` | INTEGER FK → stock_outputs.id | |
| `product_id` | INTEGER | |
| `barcode` | TEXT | Snapshot |
| `product_name` | TEXT | Snapshot |
| `quantity` | INTEGER | Cantidad enviada |
| `unit_cost` | DECIMAL | |
| `line_total` | DECIMAL | |
| `created_at` | DATETIME | |

### `product_stock`
Existencias actuales por producto por sucursal.

| Columna | Tipo | Notas |
|---|---|---|
| `id` | INTEGER PK AUTOINCREMENT | |
| `product_id` | INTEGER | Clave compuesta con branch_id |
| `branch_id` | INTEGER | Clave compuesta con product_id |
| `quantity` | INTEGER | Puede ser negativo (por diseño, el cliente permite venta sin stock) |
| `updated_at` | DATETIME | |
| `sync_status` | INTEGER | `1` = pendiente, `2` = sincronizado |
| `last_sync` | DATETIME NULL | |

> **Nota importante**: El stock puede ser negativo. El sistema no bloquea ventas ni traspasos por stock insuficiente — es una decisión de negocio del cliente.

---

## Flujos de negocio

### 1. PURCHASE — Entrada por compra a proveedor

**Regla**: Local primero, servidor inmediatamente después. Funciona offline.

```
Usuario registra productos + proveedor
        ↓
[Local] Guardar stock_entry (SyncStatus=1)
        ↓
[Local] Guardar entry_products
        ↓
[Local] product_stock += quantity (upsert por product_id+branch_id)
        ↓
[API]  POST /api/v1/sync/push/stock-entries
        ├── Aceptado → SyncStatus=2, LastSync=now
        └── Fallido/offline → SyncStatus queda en 1
                              SyncService reintentará en background
```

**Campos clave en stock_entries**:
- `entry_type = "PURCHASE"`
- `supplier_id` = ID del proveedor seleccionado (nunca 0)
- `folio_output = NULL`
- `confirmed_by_user_id = NULL`

---

### 2. OUTPUT — Salida/traspaso a otra sucursal

**Regla**: Servidor primero obligatorio. Sin conexión no se puede crear una salida.

```
Usuario selecciona sucursal destino + productos
        ↓
[API]  POST /api/v1/inventory/outputs
        ├── Fallido → Error al usuario, NO se guarda nada local
        └── Aceptado ↓
[Local] Guardar stock_output (SyncStatus=2, ya sincronizado)
        ↓
[Local] Guardar output_products
        ↓
[Local] product_stock -= quantity (upsert, mínimo permitido: negativo)
```

**Request al servidor** (`POST /api/v1/inventory/outputs`):
```json
{
  "branch_id": 1,
  "folio": "010013042026S0001",
  "destination_branch_id": 4,
  "user_id": 1,
  "total_amount": 27600,
  "output_date": "2026-04-13T21:43:58",
  "notes": "...",
  "products": [
    { "product_id": 176, "barcode": "3315", "product_name": "...", "quantity": 5, "unit_cost": 1199, "line_total": 5995 }
  ]
}
```

**El servidor al recibir este request debe**:
1. Crear el `stock_output` en su DB con `status=PENDING`
2. Crear una entrada pendiente (`stock_entry` tipo TRANSFER) en la sucursal destino con folio `ENT-{folio_salida}`
3. Retornar respuesta de éxito

**Campos clave en stock_outputs local**:
- `status = "PENDING"` (hasta que la destino confirme)
- `sync_status = 2` (ya está en servidor)

---

### 3. CONFIRM ENTRY — Confirmación de traspaso en sucursal destino

**Regla**: Servidor primero. La sucursal destino ve traspasos pendientes que vienen del servidor.

```
[API]  GET /api/v1/inventory/pending-transfers?branch_id={id}
        └── Lista de PendingTransferDto con productos y cantidades

Usuario revisa cantidades (puede modificar cantidad recibida si hay diferencia)
        ↓
[API]  POST /api/v1/inventory/confirm-transfer/{entry_id}
        ├── Fallido → Error al usuario, nada local
        └── Aceptado ↓
[Local] Crear stock_entry tipo TRANSFER (SyncStatus=2)
        ↓
[Local] Crear entry_products con ReceivedQuantity (no la original)
        ↓
[Local] product_stock += ReceivedQuantity (upsert por product_id+branch_id)
```

**Request de confirmación** (`POST /api/v1/inventory/confirm-transfer/{id}`):
```json
{
  "confirmed_by_user_id": 1,
  "products": [
    { "product_id": 176, "quantity": 5 },
    { "product_id": 1,   "quantity": 5 }
  ]
}
```

**El servidor al confirmar debe**:
1. Marcar el `stock_output` como `status=CONFIRMED`
2. Actualizar `confirmed_by_user_id` y `confirmed_at` en el `stock_entry` de la sucursal destino

**Campos clave en stock_entries local (TRANSFER)**:
- `entry_type = "TRANSFER"`
- `folio = "ENT-{folio_salida}"` — folio asignado por el servidor
- `folio_output = "{folio_salida}"` — referencia cruzada a la salida
- `supplier_id = 0` — traspasos no tienen proveedor
- `confirmed_by_user_id` = ID del usuario que confirmó
- `confirmed_at` = timestamp de confirmación
- `sync_status = 2` — el servidor ya confirmó, no se vuelve a subir

---

## Sincronización (SyncService)

### Push — Cliente → Servidor

El `SyncService` revisa periódicamente registros con `sync_status = 1` y los sube en lotes de 100.

| Entidad | Endpoint | Condición |
|---|---|---|
| `stock_entries` | `POST /api/v1/sync/push/stock-entries` | Solo entradas PURCHASE con `sync_status=1` |
| `stock_outputs` | `POST /api/v1/sync/push/stock-outputs` | Salidas con `sync_status=1` (caso teórico, normalmente ya viene en 2) |

**Formato del request push**:
```json
{
  "branch_id": 1,
  "records": [ { ...StockEntryPushDto... } ]
}
```

**Respuesta esperada** (`PushResponse`):
```json
{
  "accepted": ["folio1", "folio2"],
  "rejected": [{ "folio": "folio3", "reason": "..." }]
}
```

Cuando un folio es aceptado: `sync_status` se actualiza a `2` y `last_sync` se registra.

### Pull — Servidor → Cliente

**Actualmente NO existe pull para inventario**. El cliente no descarga `stock_entries`, `stock_outputs` ni `product_stock` del servidor.

Esto implica:
- Si se borra la DB local, el historial de entradas/salidas se pierde localmente
- El stock local se reconstruye solo a partir de las operaciones que se hagan desde ese momento
- Las entradas TRANSFER se recuperan vía `GET /api/v1/inventory/pending-transfers` (solo las pendientes)

---

## Folio — Formato y generación

Los folios se generan localmente con `FolioService`:

```
Entradas: {branch_id_pad2}{date_ddmmyyyy}E{seq_pad4}
Salidas:  {branch_id_pad2}{date_ddmmyyyy}S{seq_pad4}
Ejemplo:  010013042026E0001
```

- `branch_id` = 1 → `01`
- fecha = 13/04/2026 → `13042026`
- tipo = Entrada → `E`
- secuencia = 1 → `0001`

El folio de la entrada TRANSFER que genera el servidor es `ENT-{folio_salida}`.

---

## APIs utilizadas por el módulo de inventario

| Operación | Método | Endpoint |
|---|---|---|
| Crear salida (output) | POST | `/api/v1/inventory/outputs` |
| Listar traspasos pendientes | GET | `/api/v1/inventory/pending-transfers?branch_id={id}` |
| Confirmar traspaso | POST | `/api/v1/inventory/confirm-transfer/{entry_id}` |
| Push entradas (sync) | POST | `/api/v1/sync/push/stock-entries` |
| Push salidas (sync) | POST | `/api/v1/sync/push/stock-outputs` |
| Stock por producto | GET | `/api/v1/stock/product/{barcode}` |
| Stock por sucursal | GET | `/api/v1/stock/branch/{branch_id}?page={n}` |

---

## Resumen de sync_status por flujo

| Flujo | sync_status al crear | Cuándo pasa a 2 |
|---|---|---|
| PURCHASE (con conexión) | 1 → 2 inmediato | Al hacer push exitoso en `CreateEntryAsync` |
| PURCHASE (sin conexión) | 1 | Cuando `SyncService` logre subirlo |
| OUTPUT | 2 desde el inicio | Solo se crea local si el servidor aceptó |
| TRANSFER (confirm) | 2 desde el inicio | Solo se crea local si el servidor confirmó |
| product_stock (entradas) | 1 | No se actualiza automáticamente — no hay push de stock |
| product_stock (salidas) | 1 | No se actualiza automáticamente — no hay push de stock |

> **Observación**: `product_stock` local siempre queda con `sync_status=1` porque no existe endpoint de push para esta tabla. El stock en el servidor se actualiza indirectamente cuando el servidor procesa las entradas y salidas.

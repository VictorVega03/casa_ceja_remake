# Plan de Pruebas — Módulo de Inventario

> Fecha: 2026-04-13
> Cubre: Entradas (PURCHASE y TRANSFER), Salidas, Confirmaciones, Stock y Sincronización.

---

## Preparación general

- Tener acceso a dos sucursales distintas (ej. Sucursal A = origen, Sucursal B = destino)
- Tener acceso a la DB SQLite local de cada sucursal (SQLite Browser o consultas directas)
- Tener acceso a la DB del servidor para contrastar
- Registrar en una bitácora: resultado esperado, resultado real, estado (✅/❌)

---

## Bloque 1: Entradas PURCHASE (compra a proveedor)

### TC-01: Entrada básica con conexión
**Precondición**: Conexión al servidor activa.
**Pasos**:
1. Abrir vista de Entradas en Sucursal A
2. Agregar 2 productos por código de barras con cantidades y costos
3. Seleccionar proveedor
4. Guardar (F5)

**Verificar**:
- [ ] `stock_entries` local: 1 registro, `entry_type=PURCHASE`, `sync_status=2`, `last_sync` set
- [ ] `entry_products` local: 2 registros con cantidades y costos correctos
- [ ] `product_stock` local: stock incrementado por las cantidades ingresadas
- [ ] `stock_entries` servidor: mismo registro con mismos datos
- [ ] `stock_entries` servidor: mismos `entry_products`
- [ ] `product_stock` servidor: cantidades actualizadas
- [ ] Mensaje en UI: "guardada y sincronizada con el servidor"

---

### TC-02: Entrada sin conexión (offline)
**Precondición**: Desconectar red antes de guardar.
**Pasos**:
1. Abrir vista de Entradas
2. Agregar productos, seleccionar proveedor
3. Guardar

**Verificar**:
- [ ] `stock_entries` local: `sync_status=1`, `last_sync=NULL`
- [ ] `entry_products` local: registros guardados
- [ ] `product_stock` local: stock actualizado
- [ ] Servidor: el registro NO existe todavía
- [ ] Mensaje en UI: "guardada localmente. Se sincronizará cuando haya conexión"

**Fase 2 — Reconexión**:
1. Reconectar red
2. Esperar ciclo del SyncService (o forzar sync)

**Verificar**:
- [ ] `sync_status` cambia a `2`
- [ ] Servidor recibe el registro correctamente

---

### TC-03: Entrada con proveedor sin seleccionar
**Pasos**:
1. Agregar productos
2. Intentar guardar sin seleccionar proveedor

**Verificar**:
- [ ] UI muestra mensaje de error "Selecciona un proveedor"
- [ ] Nada se guarda en DB local
- [ ] Nada llega al servidor

---

### TC-04: Entrada con lista vacía
**Pasos**:
1. Seleccionar proveedor
2. Intentar guardar sin productos

**Verificar**:
- [ ] UI muestra mensaje de error "Agrega al menos un producto"
- [ ] Nada se guarda

---

### TC-05: Agregar mismo producto dos veces
**Pasos**:
1. Buscar un producto y agregarlo
2. Buscar el mismo código de barras y presionar Enter

**Verificar**:
- [ ] En la lista solo aparece 1 línea con cantidad incrementada (no duplicado)
- [ ] `entry_products` tiene 1 registro para ese producto con cantidad sumada

---

### TC-06: Modificar cantidad con flechas del teclado
**Pasos**:
1. Agregar un producto
2. Seleccionar la línea
3. Mantener presionada flecha derecha hasta cantidad 5
4. Guardar

**Verificar**:
- [ ] `entry_products` guarda `quantity=5`
- [ ] `product_stock` se incrementa en 5

---

### TC-07: Costo unitario personalizado
**Pasos**:
1. Agregar un producto
2. Editar manualmente el campo de costo unitario a un valor diferente
3. Guardar

**Verificar**:
- [ ] `entry_products.unit_cost` refleja el valor editado
- [ ] `entry_products.line_total = quantity × unit_cost` es correcto
- [ ] `stock_entries.total_amount` suma correcta

---

## Bloque 2: Salidas / Traspasos

### TC-08: Salida básica con conexión
**Precondición**: Conexión activa. Sucursal A tiene stock.
**Pasos**:
1. Abrir vista de Salidas en Sucursal A
2. Agregar productos
3. Seleccionar Sucursal B como destino
4. Guardar (F5)

**Verificar**:
- [ ] `stock_outputs` local Sucursal A: `status=PENDING`, `sync_status=2`
- [ ] `output_products` local: registros correctos
- [ ] `product_stock` local Sucursal A: stock decrementado
- [ ] Servidor: `stock_outputs` con `status=PENDING`
- [ ] Servidor: `stock_entries` crea entrada TRANSFER para Sucursal B con folio `ENT-{folio_salida}`
- [ ] Mensaje en UI: "Salida registrada exitosamente"

---

### TC-09: Salida sin conexión
**Precondición**: Sin conexión al servidor.
**Pasos**:
1. Intentar crear una salida

**Verificar**:
- [ ] UI muestra error "No se pudo registrar la salida en el servidor"
- [ ] Nada se guarda en DB local (server-first, no hay fallback offline)
- [ ] `stock_outputs` local: sin cambios
- [ ] `product_stock` local: sin cambios

---

### TC-10: Salida sin sucursal destino seleccionada
**Verificar**:
- [ ] UI muestra "Selecciona una sucursal destino"
- [ ] Nada se guarda

---

### TC-11: Salida con stock negativo resultante
**Precondición**: Producto con `quantity=2` en Sucursal A.
**Pasos**:
1. Crear salida de 5 unidades del mismo producto

**Verificar**:
- [ ] La salida se registra sin bloqueo
- [ ] `product_stock` local Sucursal A: `quantity = 2 - 5 = -3`
- [ ] Servidor: acepta la salida normalmente

---

### TC-12: Salida → verificar que Sucursal B ve la entrada pendiente
**Pasos**:
1. Crear salida en Sucursal A (TC-08)
2. Cambiar al perfil de Sucursal B
3. Abrir vista "Confirmar Entrada"

**Verificar**:
- [ ] Lista muestra el traspaso pendiente con el folio correcto
- [ ] Muestra productos con cantidades correctas
- [ ] Muestra sucursal origen correcta

---

## Bloque 3: Confirmación de Entrada (TRANSFER)

### TC-13: Confirmación normal (sin diferencias)
**Precondición**: Traspaso pendiente disponible en Sucursal B (del TC-08 o TC-12).
**Pasos**:
1. En Sucursal B, abrir Confirmar Entrada
2. Seleccionar el traspaso pendiente
3. NO modificar cantidades
4. Confirmar

**Verificar**:
- [ ] `stock_entries` local Sucursal B: nuevo registro, `entry_type=TRANSFER`, `sync_status=2`, `confirmed_by_user_id` set
- [ ] `entry_products` local: cantidades iguales a las enviadas
- [ ] `product_stock` local Sucursal B: stock incrementado por las cantidades correctas
- [ ] Servidor `stock_outputs`: `status=CONFIRMED`, `confirmed_at` set
- [ ] Servidor `stock_entries` Sucursal B: `confirmed_by_user_id` y `confirmed_at` actualizados
- [ ] El traspaso desaparece de la lista de pendientes
- [ ] Mensaje en UI: confirmada correctamente

---

### TC-14: Confirmación con diferencia (menos recibido)
**Precondición**: Traspaso pendiente con 10 unidades de un producto.
**Pasos**:
1. En vista de confirmación, cambiar cantidad recibida de 10 a 7
2. Confirmar

**Verificar**:
- [ ] `entry_products` local: `quantity=7` (no 10)
- [ ] `product_stock` local Sucursal B: incremento de 7, no 10
- [ ] Servidor recibe la confirmación con `quantity=7`
- [ ] `stock_entries.total_amount` se calcula con la cantidad real (7)
- [ ] Mensaje en UI menciona "diferencias" o "merma"

---

### TC-15: Confirmación sin conexión
**Precondición**: Sin conexión al servidor.
**Pasos**:
1. En vista de confirmación, intentar confirmar un traspaso

**Verificar**:
- [ ] UI muestra error de conexión
- [ ] Nada se guarda localmente (server-first)
- [ ] El traspaso sigue apareciendo en la lista de pendientes

---

### TC-16: Recarga de traspasos pendientes
**Pasos**:
1. Abrir vista de confirmación
2. Verificar que carga la lista del servidor
3. Usar el botón de recargar (si existe)

**Verificar**:
- [ ] La lista se actualiza con los traspasos pendientes actuales del servidor
- [ ] Los traspasos ya confirmados no aparecen

---

## Bloque 4: Consulta de stock

### TC-17: Stock refleja las operaciones en orden
**Escenario completo secuencial**:
1. Stock inicial de producto X en Sucursal A: 0
2. Entrada PURCHASE de 10 unidades → stock debe ser 10
3. Salida de 3 unidades a Sucursal B → stock A debe ser 7
4. Confirmar en Sucursal B → stock B debe ser 3
5. Otra entrada PURCHASE de 5 en Sucursal A → stock A debe ser 12

**Verificar en cada paso**:
- [ ] `product_stock` local Sucursal A refleja el valor esperado
- [ ] `product_stock` servidor Sucursal A refleja el valor esperado
- [ ] `product_stock` local Sucursal B refleja el valor esperado
- [ ] `product_stock` servidor Sucursal B refleja el valor esperado

---

### TC-18: Stock en API por producto
**Pasos**:
1. Después del TC-17, consultar `GET /api/v1/stock/product/{barcode}`

**Verificar**:
- [ ] La respuesta incluye ambas sucursales con sus cantidades correctas

---

## Bloque 5: Sincronización y recuperación de DB

### TC-19: Reinicio con DB intacta
**Pasos**:
1. Cerrar y reabrir la aplicación
2. Abrir módulo de inventario

**Verificar**:
- [ ] El historial de entradas sigue visible
- [ ] El historial de salidas sigue visible
- [ ] El stock local es correcto
- [ ] El SyncService arranca y completa el push de cualquier `sync_status=1`

---

### TC-20: DB local borrada — reconstrucción desde servidor (CRÍTICO)
**Precondición**: Registrar al menos 1 entrada PURCHASE y 1 salida. Verificar que todo está en el servidor.

**Pasos**:
1. Cerrar la aplicación
2. **Borrar el archivo de DB SQLite local**
3. Abrir la aplicación en la misma sucursal
4. Hacer login y entrar al módulo de inventario

**Verificar**:
- [ ] La aplicación arranca sin errores (DB se recrea vacía)
- [ ] El catálogo de productos se descarga del servidor (pull de productos en sync inicial)
- [ ] Sucursales, usuarios, categorías se sincronizan
- [ ] **`stock_entries` local: VACÍO** — actualmente no hay pull de entradas
- [ ] **`stock_outputs` local: VACÍO** — actualmente no hay pull de salidas
- [ ] **`product_stock` local: VACÍO** — stock en 0 para todos los productos

> **Limitación conocida**: No existe pull de inventario. Si se borra la DB, el stock local queda en 0.
> El historial se perdió localmente pero existe en el servidor.
> Las entradas PENDING de confirmación siguen disponibles vía `GET /api/v1/inventory/pending-transfers`.

**¿Es esto el comportamiento esperado?** → Contrastar con el diseño. Puede ser necesario implementar un pull inicial de stock cuando la DB es nueva.

---

### TC-21: Sync retry — entradas offline que se suben al reconectar
**Pasos**:
1. Crear 3 entradas PURCHASE sin conexión
2. Verificar que las 3 tienen `sync_status=1`
3. Reconectar
4. Esperar ciclo de SyncService

**Verificar**:
- [ ] Las 3 entradas cambian a `sync_status=2`
- [ ] Las 3 aparecen en el servidor
- [ ] Los `entry_products` correspondientes están en el servidor
- [ ] El servidor actualiza su `product_stock` para esas entradas

---

### TC-22: Sync con folio duplicado (retry después de éxito parcial)
**Escenario**: Una entrada subió al servidor pero el cliente no recibió la respuesta (network timeout).
**Resultado**: La entrada queda con `sync_status=1` aunque ya está en el servidor.

**Pasos**:
1. Simular: insertar manualmente en DB local un `stock_entry` con `sync_status=1` y folio que ya existe en servidor

**Verificar**:
- [ ] El servidor retorna el folio en `rejected` con razón de folio duplicado
- [ ] El cliente registra el rechazo en consola pero no lanza error fatal
- [ ] El registro local queda con `sync_status=1` (no se marca como 2 ya que fue rechazado)

> **Pregunta de diseño**: ¿Debería el cliente actualizar a `sync_status=2` cuando el servidor rechaza por "ya existe"?

---

### TC-23: Dos equipos hacen push simultáneo de la misma entrada
**Escenario**: Mismo folio generado en dos equipos distintos (posible si los relojes o secuencias están desincronizados).

**Verificar**:
- [ ] El servidor acepta solo el primero
- [ ] El segundo es rechazado con razón apropiada
- [ ] El cliente que fue rechazado muestra el error en consola pero no corrompe datos

---

### TC-24: Confirmar el mismo traspaso dos veces
**Escenario**: Dos usuarios en la misma sucursal intentan confirmar el mismo traspaso pendiente.

**Pasos**:
1. Usuario 1 abre la lista de pendientes
2. Usuario 2 abre la lista de pendientes
3. Usuario 1 confirma
4. Usuario 2 intenta confirmar (el traspaso ya no está pendiente en servidor)

**Verificar**:
- [ ] El segundo intento falla en el servidor
- [ ] Usuario 2 recibe mensaje de error
- [ ] La DB local de Usuario 2 no crea un segundo registro de stock_entry

---

## Bloque 6: Casos edge de folios

### TC-25: Secuencia de folios por día
**Pasos**:
1. Crear 3 entradas en el mismo día en la misma sucursal

**Verificar**:
- [ ] Los folios son `...E0001`, `...E0002`, `...E0003` en orden
- [ ] No hay saltos ni repetidos

---

### TC-26: Folio del día siguiente reinicia secuencia
**Pasos**:
1. El último folio del día fue `010013042026E0003`
2. Al día siguiente crear una entrada

**Verificar**:
- [ ] El nuevo folio es `010014042026E0001` (fecha actualizada, secuencia reinicia)

---

## Resumen de cobertura

| Área | TCs | Estado |
|---|---|---|
| PURCHASE con conexión | TC-01, TC-05, TC-06, TC-07 | |
| PURCHASE sin conexión + retry | TC-02 | |
| PURCHASE validaciones | TC-03, TC-04 | |
| OUTPUT con conexión | TC-08, TC-11, TC-12 | |
| OUTPUT sin conexión | TC-09 | |
| OUTPUT validaciones | TC-10 | |
| CONFIRM normal | TC-13, TC-16 | |
| CONFIRM con diferencias | TC-14 | |
| CONFIRM sin conexión | TC-15 | |
| Stock acumulado multi-op | TC-17, TC-18 | |
| Reinicio app | TC-19 | |
| DB borrada | TC-20 | |
| Sync retry offline→online | TC-21 | |
| Casos edge sincronización | TC-22, TC-23, TC-24 | |
| Folios | TC-25, TC-26 | |

# ðŸ“‹ Reglas de Descuentos - Sistema Casa Ceja
## Documento de Reglas de Negocio

---

**VersiÃ³n:** 2.0 FINAL  
**Fecha:** Enero 2026  
**Audiencia:** Equipo completo (desarrollo, capacitaciÃ³n, administraciÃ³n)  
**PropÃ³sito:** Definir claramente cÃ³mo funcionan los precios y descuentos en el sistema

---

## ðŸŽ¯ Principio Fundamental

> **"Los precios se calculan UNA VEZ al agregar el producto al carrito y se guardan como valores INMUTABLES"**

Esto significa:
- âœ… El precio se calcula cuando agregas el producto
- âœ… Ese precio NUNCA cambia despuÃ©s de la venta
- âœ… Los tickets histÃ³ricos siempre muestran los precios originales exactos
- âœ… No hay recalculaciones ni sorpresas

---

## ðŸ“Š Tipos de Precio y Descuento

El sistema maneja **6 conceptos diferentes**:

| # | Tipo | CÃ³mo funciona | Se combina | ActivaciÃ³n |
|---|------|---------------|------------|------------|
| 1 | **Precio Menudeo** | Precio base normal | âœ… Con categorÃ­a | AutomÃ¡tico |
| 2 | **Precio Mayoreo** | Precio reducido por cantidad | âœ… Con categorÃ­a | AutomÃ¡tico |
| 3 | **Descuento CategorÃ­a** | Descuento por tipo de producto | âœ… Con mayoreo/menudeo | AutomÃ¡tico |
| 4 | **Precio Especial** | Precio promocional | âŒ AISLADO | Manual (F2) |
| 5 | **Precio Vendedor** | Precio para vendedores | âŒ AISLADO | Manual (F3) |
| 6 | **Descuento General** | Sobre el total de la venta | âœ… Con todo | Manual |

---

## ðŸ”„ Flujo BÃ¡sico del Sistema

### Cuando agregas un producto al carrito:

```
1. Sistema detecta quÃ© producto es
2. Sistema verifica la cantidad
3. Sistema calcula automÃ¡ticamente:
   â”œâ”€ Â¿La cantidad califica para mayoreo?
   â”‚  â”œâ”€ SÃ â†’ Usa precio mayoreo
   â”‚  â””â”€ NO â†’ Usa precio menudeo
   â”‚
   â””â”€ Â¿El producto tiene descuento de categorÃ­a?
      â”œâ”€ SÃ â†’ Aplica descuento sobre el precio
      â””â”€ NO â†’ Mantiene el precio

4. Resultado = Precio final del producto
```

**Ejemplo:**
```
Producto: Cuaderno
Cantidad: 10 piezas
Precio Menudeo: $25
Precio Mayoreo: $22 (se activa desde 6 piezas)
Descuento CategorÃ­a "PapelerÃ­a": 10%

CÃ¡lculo automÃ¡tico:
1. Â¿10 piezas >= 6? â†’ SÃ â†’ Precio base = $22
2. Â¿Tiene descuento categorÃ­a? â†’ SÃ, 10%
3. Descuento = $22 Ã— 10% = $2.20
4. Precio final = $22 - $2.20 = $19.80 por unidad

Total lÃ­nea: $19.80 Ã— 10 = $198.00
```

---

## ðŸ“– Reglas Detalladas

### REGLA 1: Precio Mayoreo (AutomÃ¡tico)

**Â¿CuÃ¡ndo se activa?**
- Cuando la cantidad de un producto es mayor o igual a su "cantidad de mayoreo"
- Se activa automÃ¡ticamente, sin intervenciÃ³n del cajero

**CaracterÃ­sticas:**
- Cada producto tiene su propia cantidad mÃ­nima para mayoreo
- Ejemplo: Cuadernos = 6 piezas, Plumas = 12 piezas, etc.
- Es un precio mÃ¡s bajo que el menudeo
- Se PUEDE combinar con descuento de categorÃ­a

**Ejemplo:**
```
Producto: Pluma
Precio Menudeo: $10
Precio Mayoreo: $8 (desde 12 piezas)

Compra de 5 plumas  â†’ Precio: $10 (menudeo)
Compra de 12 plumas â†’ Precio: $8 (mayoreo)
Compra de 50 plumas â†’ Precio: $8 (mayoreo)
```

---

### REGLA 2: Descuento de CategorÃ­a (AutomÃ¡tico)

**Â¿CuÃ¡ndo se aplica?**
- Cuando un producto pertenece a una categorÃ­a que tiene descuento configurado
- Se aplica automÃ¡ticamente, sin intervenciÃ³n del cajero

**CaracterÃ­sticas:**
- Se define a nivel de categorÃ­a (PapelerÃ­a 10%, Juguetes 5%, etc.)
- Se COMBINA con precio mayoreo o menudeo
- Se aplica DESPUÃ‰S de determinar el precio base

**Ejemplo:**
```
CategorÃ­a "PapelerÃ­a" tiene 10% de descuento

Producto A: Cuaderno (CategorÃ­a: PapelerÃ­a)
Precio base: $25 (menudeo, 3 piezas)
Descuento: $25 Ã— 10% = $2.50
Precio final: $22.50

Producto B: Cuaderno (CategorÃ­a: PapelerÃ­a)
Precio base: $22 (mayoreo, 10 piezas)
Descuento: $22 Ã— 10% = $2.20
Precio final: $19.80
```

**Importante:**
- âœ… Descuento categorÃ­a + Precio menudeo = Permitido
- âœ… Descuento categorÃ­a + Precio mayoreo = Permitido
- âŒ Descuento categorÃ­a + Precio especial = NO permitido
- âŒ Descuento categorÃ­a + Precio vendedor = NO permitido

---

### REGLA 3: Precio Especial (Manual, Aislado)

**Â¿CuÃ¡ndo se usa?**
- Para promociones especiales
- Para productos en oferta
- Debe activarse manualmente con el atajo F2

**CaracterÃ­sticas:**
- **AISLADO**: No se combina con NINGÃšN otro descuento
- Si el producto ya tiene descuentos â†’ Se BLOQUEA la activaciÃ³n
- Es un precio final cerrado
- Se marca visualmente con color amarillo ðŸŸ¡

**Flujo de activaciÃ³n:**

**Caso 1: Producto SIN descuentos previos** âœ…
```
1. Cajero agrega producto al carrito
2. Sistema calcula precio normal
3. Cajero presiona F2 inmediatamente
4. Sistema verifica: Â¿Ya tiene descuentos?
   â†’ NO â†’ âœ… Permite activar precio especial
5. Precio final = Precio especial (sin descuentos adicionales)
```

**Caso 2: Producto CON descuentos previos** âŒ
```
1. Producto en carrito con precio $19.80
   (Mayoreo $22 + Descuento categorÃ­a 10%)
2. Cajero presiona F2
3. Sistema verifica: Â¿Ya tiene descuentos?
   â†’ SÃ â†’ âŒ BLOQUEA activaciÃ³n
4. Muestra advertencia:
   "No se puede aplicar precio especial
    El producto ya tiene descuentos:
    - Precio Mayoreo: $22.00
    - Descuento CategorÃ­a 10%: -$2.20
    Precio actual: $19.80
    
    Para usar precio especial:
    1. Eliminar producto del carrito
    2. Agregar nuevamente
    3. Activar F2 inmediatamente"
```

**Ejemplo completo:**
```
Producto: Cuaderno
Precio Menudeo: $25
Precio Especial: $18
CategorÃ­a: PapelerÃ­a (10% descuento)

OpciÃ³n A - Sin activar precio especial:
  Cantidad: 3 piezas
  Base: $25 (menudeo)
  Descuento categorÃ­a: -$2.50
  Total: $22.50 por unidad

OpciÃ³n B - Con precio especial activado:
  Cantidad: 3 piezas
  Cajero presiona F2 antes de que se calculen descuentos
  Precio final: $18.00 por unidad ðŸŸ¡
  Sin descuento de categorÃ­a
  Sin descuento de mayoreo
```

---

### REGLA 4: Precio Vendedor (Manual, Aislado)

**Â¿CuÃ¡ndo se usa?**
- Para usuarios con rol de "Vendedor"
- Precio preferencial para el equipo de ventas
- Debe activarse manualmente con el atajo F3

**CaracterÃ­sticas:**
- Funciona EXACTAMENTE igual que Precio Especial
- **AISLADO**: No se combina con ningÃºn otro descuento
- Si el producto ya tiene descuentos â†’ Se BLOQUEA la activaciÃ³n
- Es un precio final cerrado
- Se marca visualmente con color verde ðŸŸ¢

**Reglas:**
- âœ… Precio Especial O Precio Vendedor (solo uno)
- âŒ NO se pueden activar ambos en el mismo producto
- âŒ NO se combina con descuento de categorÃ­a
- âŒ NO se combina con precio mayoreo

**Ejemplo:**
```
Producto: Cuaderno
Precio Menudeo: $25
Precio Mayoreo: $22
Precio Vendedor: $20

Usuario vendedor compra 10 piezas:

OpciÃ³n A - Sin activar precio vendedor:
  Precio: $22 (mayoreo, porque 10 >= 6)
  Descuento categorÃ­a 10%: -$2.20
  Final: $19.80 por unidad

OpciÃ³n B - Con precio vendedor activado (F3):
  Precio: $20.00 por unidad ðŸŸ¢
  Sin descuento de categorÃ­a
  Sin precio mayoreo
```

---

### REGLA 5: Descuento General sobre Venta (Manual, Final)

**Â¿CuÃ¡ndo se aplica?**
- Al finalizar la venta, sobre el total completo
- Se activa manualmente por el cajero
- Se usa para descuentos especiales del negocio

**CaracterÃ­sticas:**
- Se aplica SOBRE TODO el subtotal de la venta
- Incluye productos con precio normal
- Incluye productos con precio especial ðŸŸ¡
- Incluye productos con precio vendedor ðŸŸ¢
- Se aplica AL FINAL, despuÃ©s de sumar todo

**Dos modalidades:**

**Modalidad A: Porcentaje**
```
Opciones comunes: 5%, 10%, 15%, 20%
CÃ¡lculo: Subtotal Ã— (Porcentaje / 100)

Ejemplo:
  Subtotal: $1,000
  Descuento 10%: $100
  Total: $900
```

**Modalidad B: Cantidad Fija**
```
Cajero ingresa cantidad especÃ­fica: $50, $100, etc.
ValidaciÃ³n: No puede exceder el subtotal

Ejemplo:
  Subtotal: $1,000
  Descuento fijo: $75
  Total: $925
```

**Importante:**
- âš ï¸ Solo se puede usar UNA modalidad (porcentaje O cantidad fija)
- âš ï¸ No se pueden aplicar ambas a la vez
- âœ… El cajero decide cuÃ¡l usar segÃºn la situaciÃ³n

**Ejemplo completo:**
```
CARRITO:
Producto A: Pluma (x10)
  Precio: $9.50 (mayoreo + categorÃ­a)
  Subtotal: $95.00

Producto B: Cuaderno (x5)
  Precio: $22.50 (menudeo + categorÃ­a)
  Subtotal: $112.50

Producto C: LÃ¡piz (x2) ðŸŸ¡
  Precio especial: $3.00
  Subtotal: $6.00

Producto D: Borrador (x20) ðŸŸ¢
  Precio vendedor: $2.00
  Subtotal: $40.00

SUBTOTAL TOTAL: $253.50

Cajero aplica descuento general 10%:
Descuento: $253.50 Ã— 10% = $25.35

TOTAL FINAL: $228.15

âœ… El descuento general se aplicÃ³ sobre TODO,
   incluyendo productos con precio especial y vendedor
```

---

## ðŸŽ¨ Casos de Uso Completos

### Caso 1: Venta Normal PequeÃ±a

```
Cliente compra:
- 2 Cuadernos (no califica mayoreo)

Precio Menudeo: $25
Descuento CategorÃ­a: 10%
Cantidad mayoreo: 6 piezas

CÃ¡lculo:
1. Cantidad 2 < 6 â†’ No califica mayoreo
2. Base: $25 (menudeo)
3. Descuento categorÃ­a: $25 Ã— 10% = $2.50
4. Precio final: $22.50 por unidad

TICKET:
  Cuaderno (x2)
  P. Menudeo: $25.00
  Desc Cat 10%: -$2.50
  C/U: $22.50
  Total: $45.00
```

### Caso 2: Venta Mayoreo

```
Cliente compra:
- 12 Cuadernos (califica mayoreo)

Precio Menudeo: $25
Precio Mayoreo: $22 (desde 6 pzas)
Descuento CategorÃ­a: 10%

CÃ¡lculo:
1. Cantidad 12 >= 6 â†’ Califica mayoreo
2. Base: $22 (mayoreo)
3. Descuento categorÃ­a: $22 Ã— 10% = $2.20
4. Precio final: $19.80 por unidad

TICKET:
  Cuaderno (x12)
  P. Mayoreo: $22.00
  Desc Cat 10%: -$2.20
  C/U: $19.80
  Total: $237.60
```

### Caso 3: Venta con Precio Especial

```
PromociÃ³n: Cuadernos en oferta

Cliente compra:
- 5 Cuadernos

Precio Menudeo: $25
Precio Especial: $18
Descuento CategorÃ­a: 10%

Proceso:
1. Cajero agrega producto
2. Sistema calcula: $22.50 (menudeo + categorÃ­a)
3. Cajero ELIMINA producto
4. Cajero agrega producto nuevamente
5. Cajero presiona F2 INMEDIATAMENTE
6. Precio final: $18.00 ðŸŸ¡

TICKET:
  Cuaderno (x5) ðŸŸ¡
  P. Especial: $18.00
  C/U: $18.00
  Total: $90.00
```

### Caso 4: Venta Mixta con Descuento General

```
Cliente compra varios productos

PRODUCTOS:
1. Pluma (x24)
   - Califica mayoreo
   - Precio: $8.00
   - Descuento categorÃ­a 5%: -$0.40
   - Final: $7.60 Ã— 24 = $182.40

2. Cuaderno (x3)
   - NO califica mayoreo
   - Precio: $25.00
   - Descuento categorÃ­a 10%: -$2.50
   - Final: $22.50 Ã— 3 = $67.50

3. LÃ¡piz (x10) ðŸŸ¡
   - Precio especial activado
   - Final: $3.00 Ã— 10 = $30.00

SUBTOTAL: $279.90

Cliente es frecuente, cajero aplica descuento 10%:
Descuento general: $279.90 Ã— 10% = $27.99

TOTAL FINAL: $251.91

TICKET COMPLETO:
  Pluma (x24)             $182.40
  Cuaderno (x3)            $67.50
  LÃ¡piz (x10) ðŸŸ¡           $30.00
  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  SUBTOTAL               $279.90
  DESC. GRAL 10%         -$27.99
  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  TOTAL                  $251.91
```

### Caso 5: Intento Fallido de Precio Especial

```
Producto ya en carrito con descuentos

SituaciÃ³n:
- Cuaderno (x10) ya agregado
- Precio actual: $19.80 (mayoreo + categorÃ­a)
- Cajero intenta aplicar precio especial

Proceso:
1. Cajero presiona F2
2. Sistema detecta descuentos existentes
3. Sistema BLOQUEA la operaciÃ³n
4. Muestra advertencia

ADVERTENCIA EN PANTALLA:
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ âš ï¸ NO SE PUEDE APLICAR              â”‚
â”‚    PRECIO ESPECIAL                  â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ El producto ya tiene descuentos:    â”‚
â”‚                                     â”‚
â”‚ â€¢ Precio Mayoreo: $22.00            â”‚
â”‚ â€¢ Descuento CategorÃ­a 10%: -$2.20   â”‚
â”‚                                     â”‚
â”‚ Precio actual: $19.80               â”‚
â”‚ Precio especial: $18.00             â”‚
â”‚ Diferencia: Solo $1.80 menos        â”‚
â”‚                                     â”‚
â”‚ Para usar precio especial debe:     â”‚
â”‚ 1. Eliminar este producto           â”‚
â”‚ 2. Agregarlo nuevamente             â”‚
â”‚ 3. Presionar F2 inmediatamente      â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

Opciones del cajero:
[Aceptar] - Mantener precio actual
```

---

## ðŸ” Inmutabilidad de Precios

### Â¿QuÃ© significa "inmutabilidad"?

Una vez que se completa una venta:
- âœ… Los precios quedan CONGELADOS en la base de datos
- âœ… NUNCA se recalculan
- âœ… Los tickets siempre muestran los precios originales exactos
- âœ… Aunque cambien los precios en el catÃ¡logo

### Durante el Carrito vs DespuÃ©s de la Venta

**EN EL CARRITO (antes de confirmar venta):**
```
âœ… Permitido:
- Cambiar cantidad â†’ Se RECALCULA precio
- Agregar descuentos â†’ Se RECALCULA precio
- Eliminar productos
- Modificar el carrito

El precio se ajusta dinÃ¡micamente
```

**DESPUÃ‰S DE LA VENTA (confirmada):**
```
âŒ Prohibido:
- Recalcular precios
- Cambiar cantidades
- Modificar descuentos

âœ… Solo permitido:
- Consultar venta (con precios originales)
- Reimprimir ticket (con precios originales)
- Ver en reportes (con precios originales)
```

### Ejemplo de Inmutabilidad

```
ENERO 2026:
Venta #001
- Cuaderno: $19.80 (mayoreo + categorÃ­a)
- Cliente pagÃ³: $198.00 (10 unidades)

JUNIO 2026 (6 meses despuÃ©s):
Precios cambiaron en el catÃ¡logo:
- Cuaderno ahora cuesta $30 menudeo
- CategorÃ­a ahora tiene 15% descuento

Cliente pide reimprimir ticket de Venta #001:

âœ… Sistema muestra:
  Cuaderno: $19.80 (PRECIO ORIGINAL)
  Total: $198.00

âŒ Sistema NO muestra:
  Cuaderno: $25.50 (precio recalculado)
  âŒ ESTO ESTARÃA MAL

Resultado: Ticket idÃ©ntico al original
```

---

## âš–ï¸ Prioridades y Combinaciones

### Tabla de Compatibilidad

| Desde \ Con | Mayoreo | CategorÃ­a | Especial | Vendedor | Gral |
|-------------|---------|-----------|----------|----------|------|
| **Mayoreo** | - | âœ… | âŒ | âŒ | âœ… |
| **Menudeo** | - | âœ… | âŒ | âŒ | âœ… |
| **CategorÃ­a** | âœ… | - | âŒ | âŒ | âœ… |
| **Especial** | âŒ | âŒ | - | âŒ | âœ… |
| **Vendedor** | âŒ | âŒ | âŒ | - | âœ… |
| **General** | âœ… | âœ… | âœ… | âœ… | - |

âœ… = Se pueden combinar  
âŒ = NO se pueden combinar  
\- = No aplica

### Reglas de CombinaciÃ³n

**âœ… COMBINACIONES PERMITIDAS:**
1. Precio Mayoreo + Descuento CategorÃ­a
2. Precio Menudeo + Descuento CategorÃ­a
3. Cualquier precio + Descuento General

**âŒ COMBINACIONES PROHIBIDAS:**
1. Precio Especial + Descuento CategorÃ­a
2. Precio Especial + Precio Mayoreo
3. Precio Vendedor + Descuento CategorÃ­a
4. Precio Vendedor + Precio Mayoreo
5. Precio Especial + Precio Vendedor

**Resumen Simple:**
```
Precios AutomÃ¡ticos (Mayoreo/Menudeo)
  âœ… Se combinan con Descuento CategorÃ­a
  âœ… Todo se combina con Descuento General

Precios Manuales (Especial/Vendedor)
  âŒ NO se combinan con nada mÃ¡s
  âœ… Excepto Descuento General (se aplica al final)
```

---

## ðŸ“± Interfaz de Usuario

### Indicadores Visuales

```
Precio Normal:
  Cuaderno (x10)
  $19.80 c/u

Precio Especial:
  Cuaderno (x10) ðŸŸ¡
  $18.00 c/u

Precio Vendedor:
  Cuaderno (x10) ðŸŸ¢
  $20.00 c/u
```

### Atajos de Teclado

| Tecla | FunciÃ³n |
|-------|---------|
| **F2** | Activar Precio Especial |
| **F3** | Activar Precio Vendedor |
| **F5** | Aplicar Descuento General (sugerido) |

---

## âœ… Checklist de ValidaciÃ³n

### Al Agregar Producto

- [ ] Â¿La cantidad califica para mayoreo?
- [ ] Â¿El producto tiene categorÃ­a con descuento?
- [ ] Â¿Se activÃ³ precio especial/vendedor?
- [ ] Â¿Ya hay descuentos si se intenta activar especial/vendedor?

### Al Completar Venta

- [ ] Â¿El subtotal estÃ¡ correcto?
- [ ] Â¿Se aplicÃ³ descuento general?
- [ ] Â¿El descuento general no excede el subtotal?
- [ ] Â¿Los precios quedaron guardados correctamente?

### Al Reimprimir Ticket

- [ ] Â¿Los precios son exactamente los originales?
- [ ] Â¿NO se recalculÃ³ nada?
- [ ] Â¿Los descuentos son los originales?

---

## ðŸ“š Glosario

**Precio Base:** Precio inicial del producto antes de descuentos (puede ser mayoreo o menudeo)

**Precio Final:** Precio que se cobra al cliente despuÃ©s de aplicar todos los descuentos

**Precio Aislado:** Precio que NO se combina con otros descuentos (especial y vendedor)

**Descuento Acumulable:** Descuento que SÃ se puede combinar con otros (categorÃ­a y general)

**Inmutabilidad:** Propiedad de los precios de NO cambiar despuÃ©s de la venta

**Subtotal:** Suma de todos los productos antes del descuento general

**Total:** Cantidad final a pagar despuÃ©s de todos los descuentos

---

## ðŸŽ“ Preguntas Frecuentes

**P: Â¿Puedo aplicar precio especial a un producto que ya tiene descuento de categorÃ­a?**  
R: No. Si ya tiene descuentos, debes eliminar el producto y agregarlo nuevamente, activando precio especial antes que se calculen los descuentos automÃ¡ticos.

**P: Â¿QuÃ© pasa si el precio especial es mÃ¡s caro que el precio con descuentos?**  
R: El sistema actualmente bloquea la activaciÃ³n si ya hay descuentos. El cajero debe decidir cuÃ¡l opciÃ³n usar.

**P: Â¿El descuento general aplica sobre productos con precio especial?**  
R: SÃ­, el descuento general se aplica sobre TODO el subtotal, sin importar el tipo de precio.

**P: Â¿Puedo cambiar el precio de una venta despuÃ©s de completarla?**  
R: No. Los precios son inmutables una vez confirmada la venta. Solo se pueden consultar y reimprimir con los precios originales.

**P: Â¿Puedo usar precio especial Y precio vendedor al mismo tiempo?**  
R: No. Son mutuamente excluyentes. Solo puedes usar uno u otro, no ambos.

**P: Â¿QuÃ© pasa si cambio la cantidad de un producto en el carrito?**  
R: El sistema recalcula automÃ¡ticamente. Si ahora califica para mayoreo, se aplica el precio mayoreo.

**P: Â¿El descuento de categorÃ­a siempre se aplica?**  
R: SÃ­, de manera automÃ¡tica, EXCEPTO cuando se usa precio especial o vendedor.

---

## ðŸ“ Resumen Ejecutivo

### En 5 Puntos:

1. **AutomÃ¡tico es mejor:** Mayoreo + CategorÃ­a se calculan solos
2. **Especial/Vendedor son aislados:** No se mezclan con nada
3. **General es al final:** Sobre todo el total de la venta
4. **Una vez vendido, congelado:** Los precios nunca cambian
5. **Claro y visual:** Marcas de color para precios especiales

### Flujo Simple:

```
AGREGAR PRODUCTO
    â†“
Â¿Activar Especial/Vendedor? (F2/F3)
    â”œâ”€ SÃ â†’ Precio aislado
    â””â”€ NO â†’ AutomÃ¡tico (Mayoreo/Menudeo + CategorÃ­a)
    â†“
COMPLETAR VENTA
    â†“
Â¿Aplicar Descuento General?
    â”œâ”€ SÃ â†’ Sobre total
    â””â”€ NO â†’ Total = Subtotal
    â†“
GUARDAR (Inmutable)
```

---

**Documento creado:** Enero 2026  
**VersiÃ³n:** 2.0 FINAL  
**PrÃ³xima revisiÃ³n:** Cuando el cliente solicite cambios

---

*Este documento define las reglas de negocio completas para el sistema de descuentos de Casa Ceja. Debe ser consultado por todo el equipo antes de realizar ventas, capacitaciones o cambios en el sistema.*
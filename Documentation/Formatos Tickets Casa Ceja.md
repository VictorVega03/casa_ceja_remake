# FORMATOS DE TICKETS — CASA CEJA REMAKE
**Referencia extraída del código original WinForms**  
**Destino:** `Services/TicketService.cs` y `Services/PrintService.cs`

---

## ÍNDICE
1. [Constantes y parámetros globales](#1-constantes-y-parámetros-globales)
2. [Ticket de Venta (Térmica)](#2-ticket-de-venta-térmica)
3. [Ticket de Venta (Carta/Letra)](#3-ticket-de-venta-cartaletter)
4. [Ticket de Corte CZ (Térmica)](#4-ticket-de-corte-cz-térmica)
5. [Ticket de Corte CZ (Carta/Gráfico)](#5-ticket-de-corte-cz-cartagráfico)
6. [Ticket de Apartado (Térmica)](#6-ticket-de-apartado-térmica)
7. [Ticket de Crédito (Térmica)](#7-ticket-de-crédito-térmica)
8. [Ticket de Abono (Térmica)](#8-ticket-de-abono-térmica)
9. [Reimprimir Apartado (Térmica)](#9-reimprimir-apartado-con-historial-de-pagos)
10. [Reimprimir Crédito (Térmica)](#10-reimprimir-crédito-con-historial-de-pagos)
11. [Historial de Cortes (Térmica)](#11-historial-de-cortes-térmica)
12. [Reimprimir Venta (Carta)](#12-reimprimir-venta-carta)
13. [Indicadores de descuento en productos](#13-indicadores-de-descuento-en-productos)

---

## 1. Constantes y parámetros globales

```
Ancho línea térmica : 40 caracteres
Separador guiones   : "----------------------------------------"
Separador asteriscos: "****************************************"
Separador iguales   : "========================================"
Separador totales   : "                             -----------"
Encabezado artículos: "Articulo        Can    P.Unit    Importe"
```

**Datos que siempre aparecen en encabezados:**
- Nombre empresa: `CASA CEJA`
- Sucursal: `Sucursal: {nombre_sucursal}` (en mayúsculas)
- Dirección de sucursal
- Fecha/hora de la operación
- Folio del ticket
- RFC (si está configurado en settings)
- Pie de ticket (configurable por el usuario en settings)
- URL facturación: `https://cm-papeleria.com/public/facturacion`

---

## 2. Ticket de Venta (Térmica)

**Método original:** `LocaldataManager.imprimirTicket()`  
**Tipo impresora:** Térmica (impresión directa ESC/POS vía `RawPrinterHelper`)

### Estructura del ticket

```
[ABRE CAJÓN si no es reimpresión]
=========================================
           CASA CEJA
      Sucursal: {SUCURSAL_NOMBRE}
         {SUCURSAL_DIRECCIÓN}
         {FECHA_VENTA}
         FOLIO: {FOLIO}

Articulo        Can    P.Unit    Importe
----------------------------------------
{NOMBRE_PRODUCTO}{INDICADORES} {CANT} {P.UNIT_ORIGINAL} {P.TOTAL}
... (uno por línea)
----------------------------------------
SUBTOTAL $                  {SUBTOTAL_SIN_DESCUENTOS}
DESC. POR CATEGORIA         -{DESC_CATEGORIA}        [si aplica]
DESC. PRECIO ESPECIAL       -{DESC_PRECIO_ESP}       [si aplica]
DESCUENTO DE VENTA          -{DESC_VENTA}            [si aplica]
TOTAL FINAL $               {TOTAL_FINAL}
----------------------------------------
PAGO T. DEBITO              {MONTO}                  [si aplica]
PAGO T.CREDITO              {MONTO}                  [si aplica]
PAGO CHEQUES                {MONTO}                  [si aplica]
PAGO TRANSFERENCIA          {MONTO}                  [si aplica]
EFECTIVO ENTREGADO          {MONTO}                  [si aplica]
SU CAMBIO $                 {CAMBIO}
----------------------------------------

      LE ATENDIO: {CAJERO_NOMBRE}
    NO DE ARTICULOS: {NUM_ART 5 dígitos}
       GRACIAS POR SU COMPRA

         RFC: {RFC}                          [si está configurado]

----------------------------------------
         {PIE_DE_TICKET}                     [si está configurado]
----------------------------------------

   SI DESEA FACTURAR ESTA COMPRA INGRESE A
   https://cm-papeleria.com/public/facturacion
[CORTA TICKET]
```

### Reglas de cálculo

```
subtotalSinDescuentos = Σ (precio_menudeo_original × cantidad)  // precio sin ningún descuento
descuentoCategoria    = Σ (descuento_categoria_unitario × cantidad)  // para productos con descuento de categoría
descuentoPrecioEsp    = Σ (descuento_unitario × cantidad)            // para productos con precio especial
totalFinal            = subtotalSinDescuentos - descuentoCategoria - descuentoPrecioEsp - descuentoVenta

// El P.UNIT en el ticket siempre muestra el precio original (menudeo), NO el precio con descuento
// El P.TOTAL muestra el precio final con descuento × cantidad
```

### Columnas de artículos
```
{NombreProducto}{Indicadores}  {Cantidad}  {PrecioUnitOriginal}  {TotalLinea}
```
- Nombre truncado si excede caracteres disponibles
- Indicadores pegados al nombre (ver sección 13)

---

## 3. Ticket de Venta (Carta/Letter)

**Método original:** `Ventas.imprimirTicketCarta()` + `docToPrint_PrintPage()`  
**Tipo impresora:** Inyección/Láser, papel carta  
**Fuente:** Configurable (`fontName`, `fontSize` desde settings)  
**Tabs configurados por tamaño de fuente:**

| FontSize | Col1 | Col2 | Col3 | Col4 |
|----------|------|------|------|------|
| 5        | 110  | 30   | 50   | 50   |
| 6        | 130  | 40   | 60   | 60   |
| 7        | 145  | 45   | 65   | 65   |
| 8        | 160  | 50   | 65   | 65   |
| 9        | 185  | 55   | 70   | 70   |
| 10       | 210  | 60   | 75   | 75   |
| 11       | 225  | 75   | 85   | 85   |
| 12       | 250  | 75   | 90   | 90   |
| 13       | 270  | 80   | 100  | 100  |
| 14       | 290  | 85   | 110  | 110  |
| 15       | 310  | 90   | 120  | 120  |

### Estructura del ticket (texto con tabs `\t`)

```
CASA CEJA
SUCURSAL: {SUCURSAL_NOMBRE}
{SUCURSAL_DIRECCIÓN}
{FECHA_VENTA}
FOLIO: {FOLIO}

DESCRIPCION\tCANT\tP. UNIT\tP. TOTAL
{nombre_prod}{indicadores}\t{cant}\t{p_unit_original}\t{p_total}
... (uno por línea)

[si fontName != "Consolas": "--------------------"]
--------------------------------------------------------------
SUBTOTAL $\t------>\t\t{SUBTOTAL}
DESC. POR CATEGORIA\t------>\t-{DESC_CAT}            [si aplica]
DESC. PRECIO ESPECIAL\t------>\t-{DESC_PRECIO_ESP}   [si aplica]
DESCUENTO DE VENTA\t------>\t-{DESC_VENTA}           [si aplica]
TOTAL A PAGAR $\t------>\t\t{TOTAL_FINAL}

[si fontName != "Consolas": "--------------------"]
--------------------------------------------------------------

PAGO T. DEBITO\t------>\t\t{MONTO}      [si aplica]
PAGO T. CREDITO\t------>\t\t{MONTO}     [si aplica]
PAGO CHEQUES\t------>\t\t{MONTO}        [si aplica]
PAGO TRANSFERENCIA\t------>\t\t{MONTO}  [si aplica]
EFECTIVO ENTREGADO\t------>\t\t{MONTO}  [si aplica]
SU CAMBIO $\t------>\t\t{CAMBIO}

[si fontName != "Consolas": "--------------------"]
--------------------------------------------------------------

LE ATENDIO: {CAJERO_NOMBRE}
NO DE ARTICULOS: {NUM_ART 5 dígitos}
GRACIAS POR SU COMPRA

RFC: {RFC}                                          [si configurado]

----------------------------------------------------------------------------------
{PIE_DE_TICKET}                                     [si configurado]
----------------------------------------------------------------------------------

SI DESEA FACTURAR ESTA COMPRA INGRESE A :
https://cm-papeleria.com/public/facturacion
```

### Cálculo del total carta
```
// totalcarrito ya tiene todos los descuentos de precio especial y categoría aplicados
// data.descuento es el descuento de venta manual aplicado sobre el total
totalFinalCorrecto = totalcarrito - data.descuento

// Para el cambio:
cambio = totalPagado - totalFinalCorrecto  (valor positivo = cambio a devolver)
```

---

## 4. Ticket de Corte CZ (Térmica)

**Métodos originales:** `Ventas.imprimirCorte()` y `HistorialCortes` (mismo formato)  
**Tipo impresora:** Térmica  

### Estructura del ticket

```
         CASA CEJA

   SUCURSAL: {SUCURSAL_NOMBRE}

   CZ FOLIO:  {FOLIO_CORTE}

----------------------------------------
FECHA DE APERTURA:         {FECHA_APERTURA}
FECHA DE CORTE:            {FECHA_CORTE}
----------------------------------------

FONDO DE APERTURA:         {FONDO_APERTURA}

TOTAL CZ:                  {TOTAL_CZ}
----------------------------------------
EFECTIVO DE CREDITOS:      {EFECTIVO_CREDITOS}
EFECTIVO DE APARTADOS:     {EFECTIVO_APARTADOS}
EFECTIVO DIRECTO:          {EFECTO_DIRECTO}
----------------------------------------

----------------------------------------
TOTAL T. DEBITO            {TOTAL_DEBITO}
TOTAL T. CREDITO           {TOTAL_CREDITO}
TOTAL CHEQUES              {TOTAL_CHEQUES}
TOTAL TRANSFERENCIAS       {TOTAL_TRANSFERENCIAS}
----------------------------------------

----------------------------------------
SOBRANTE:                  {SOBRANTE}
GASTOS:                    {TOTAL_GASTOS}
INGRESOS:                  {TOTAL_INGRESOS}
EFECTIVO TOTAL:            {TOTAL_EFECTIVO}
----------------------------------------




----------------------------------------
CAJERO:{NOMBRE_CAJERO}
[CORTA TICKET]
```

### Fórmulas del corte

```
TOTAL_CZ        = total_efectivo + total_tarjetas_debito + total_tarjetas_credito 
                  + total_cheques + total_transferencias + sobrante

EFECTIVO_DIRECTO = total_efectivo - total_gastos

TOTAL_GASTOS    = Σ valores de diccionario JSON "gastos"
TOTAL_INGRESOS  = Σ valores de diccionario JSON "ingresos"
```

---

## 5. Ticket de Corte CZ (Carta/Gráfico)

**Método original:** `Ventas.docZToPrint_PrintPagez()`  
**Tipo impresora:** Láser/inyección, papel carta  
**Fuente título:** Calibri 18pt Bold  
**Fuente cuerpo:** Calibri 12pt Bold  
**Layout:** Rectángulos dibujados para cada fila (formato tabla visual)

### Contenido (por renglones dibujados en coordenadas Y)

```
[TÍTULO CENTRADO]          CASA CEJA
[SUBTÍTULO CENTRADO]       SUCURSAL: {SUCURSAL_NOMBRE}
[CENTRADO]                 CZ FOLIO: {FOLIO_CORTE}

[FILA CON BORDE: Y+4]     FECHA DE APERTURA:     {FECHA_APERTURA}
[FILA CON BORDE: Y+5]     FECHA DE CORTE:        {FECHA_CORTE}

[FILA CON BORDE: Y+7]     FONDO DE APERTURA:     $ {FONDO_APERTURA}

[FILA CON BORDE: Y+9]     TOTAL CZ:              $ {TOTAL_CZ}
[FILA CON BORDE: Y+10]    EFECTIVO DE CREDITOS:  $ {EFECTIVO_CREDITOS}
[FILA CON BORDE: Y+11]    EFECTIVO DE APARTADOS: $ {EFECTIVO_APARTADOS}

[FILA CON BORDE: Y+13]    EFECTIVO DIRECTO:      $ {EFECTIVO_DIRECTO}

[FILA CON BORDE: Y+16]    TOTAL T. DEBITO:       $ {TOTAL_DEBITO}
[FILA CON BORDE: Y+17]    TOTAL T. CREDITO:      $ {TOTAL_CREDITO}
[FILA CON BORDE: Y+18]    TOTAL CHEQUES:         $ {TOTAL_CHEQUES}
[FILA CON BORDE: Y+19]    TOTAL TRANSFERENCIAS:  $ {TOTAL_TRANSFERENCIAS}

[FILA CON BORDE: Y+21]    SOBRANTE:              $ {SOBRANTE}
[FILA CON BORDE: Y+22]    GASTOS:                $ {TOTAL_GASTOS}
[FILA CON BORDE: Y+23]    INGRESOS:              $ {TOTAL_INGRESOS}

[FILA CON BORDE: Y+25]    EFECTIVO TOTAL:        $ {TOTAL_EFECTIVO}

[LÍNEA: Y+29]             ____________________________________________
[CENTRADO: Y+28]          FIRMA
[CENTRADO: Y+29]          CAJERO: {NOMBRE_CAJERO}
```

**Nota de implementación:** En Avalonia, este formato gráfico se puede generar como HTML/PDF o usando SkiaSharp para el renderizado. El formato térmica es suficiente para la fase actual.

---

## 6. Ticket de Apartado (Térmica)

**Método original:** `LocaldataManager.imprimirApartado()`  
**Tipo impresora:** Térmica  

### Estructura del ticket

```
[ABRE CAJÓN]
         CASA CEJA
    Sucursal: {SUCURSAL_NOMBRE}
       {SUCURSAL_DIRECCIÓN}
       {FECHA_APARTADO}
       FOLIO: {FOLIO_CORTE}

       TICKET DE APARTADO

Articulo        Can    P.Unit    Importe
----------------------------------------
{NOMBRE_PRODUCTO}{INDICADORES} {CANT} {P.UNIT_ORIGINAL} {P.TOTAL}
...
----------------------------------------
SUBTOTAL $              {SUBTOTAL_SIN_DESCUENTOS}
DESC. POR CATEGORIA     -{DESC_CAT}              [si aplica]
DESC. PRECIO ESPECIAL   -{DESC_PRECIO_ESP}       [si aplica]
TOTAL $                 {TOTAL_APARTADO}

PAGO T. DEBITO          {MONTO}                  [si aplica]
PAGO T.CREDITO          {MONTO}                  [si aplica]
PAGO CHEQUES            {MONTO}                  [si aplica]
PAGO TRANSFERENCIA      {MONTO}                  [si aplica]
EFECTIVO ENTREGADO      {MONTO}                  [si aplica]
----------------------------------------
POR PAGAR $             {TOTAL - TOTAL_PAGADO}

      LE ATENDIO: {CAJERO_NOMBRE}
    NO DE ARTICULOS: {NUM_ART 5 dígitos}
      FECHA DE VENCIMIENTO:
         {FECHA_VENCIMIENTO}
            CLIENTE:
         {CLIENTE_NOMBRE}
       NUMERO TELEFONICO:
         {CLIENTE_TELEFONO}

         RFC: {RFC}                              [si configurado]

----------------------------------------
         {PIE_DE_TICKET}                         [si configurado]
----------------------------------------

   SI DESEA FACTURAR ESTA COMPRA INGRESE A
   https://cm-papeleria.com/public/facturacion
[CORTA TICKET]
```

### Diferencias vs Ticket de Venta
- Encabezado dice `TICKET DE APARTADO`
- Muestra `FECHA DE VENCIMIENTO` y datos del cliente (nombre, teléfono)
- No hay `CAMBIO` — en su lugar muestra `POR PAGAR $`
- NO hay `DESCUENTO DE VENTA` (solo categoría y precio especial)
- El cajón SÍ se abre siempre (no condicional)

---

## 7. Ticket de Crédito (Térmica)

**Método original:** `LocaldataManager.imprimirCredito()`  
**Tipo impresora:** Térmica  

### Estructura del ticket

```
[ABRE CAJÓN]
         CASA CEJA
    Sucursal: {SUCURSAL_NOMBRE}
       {SUCURSAL_DIRECCIÓN}
       {FECHA_CREDITO}
       FOLIO: {FOLIO_CREDITO}

       TICKET DE CREDITO

Articulo        Can    P.Unit    Importe
----------------------------------------
{NOMBRE_PRODUCTO}{INDICADORES} {CANT} {P.UNIT_ORIGINAL} {P.TOTAL}
...
----------------------------------------
SUBTOTAL $              {SUBTOTAL_SIN_DESCUENTOS}
DESC. POR CATEGORIA     -{DESC_CAT}              [si aplica]
DESC. PRECIO ESPECIAL   -{DESC_PRECIO_ESP}       [si aplica]
TOTAL $                 {TOTAL_CREDITO}

PAGO T. DEBITO          {MONTO}                  [si aplica]
PAGO T.CREDITO          {MONTO}                  [si aplica]
PAGO CHEQUES            {MONTO}                  [si aplica]
PAGO TRANSFERENCIA      {MONTO}                  [si aplica]
EFECTIVO ENTREGADO      {MONTO}                  [si aplica]
----------------------------------------
POR PAGAR $             {TOTAL - TOTAL_PAGADO}

      LE ATENDIO: {CAJERO_NOMBRE}
    NO DE ARTICULOS: {NUM_ART 5 dígitos}
      FECHA DE VENCIMIENTO:
         {FECHA_VENCIMIENTO}
            CLIENTE:
         {CLIENTE_NOMBRE}
       NUMERO TELEFONICO:
         {CLIENTE_TELEFONO}

         RFC: {RFC}                              [si configurado]

----------------------------------------
         {PIE_DE_TICKET}                         [si configurado]
----------------------------------------

   SI DESEA FACTURAR ESTA COMPRA INGRESE A
   https://cm-papeleria.com/public/facturacion
[CORTA TICKET]
```

**Nota:** El formato es idéntico al Ticket de Apartado, excepto que el encabezado dice `TICKET DE CREDITO` y el folio es `FOLIO_CREDITO` (no `FOLIO_CORTE`).

---

## 8. Ticket de Abono (Térmica)

**Método original:** `LocaldataManager.imprimirAbono()`  
**Tipo impresora:** Térmica  
**Aplica para:** Abonos a créditos Y abonos a apartados

### Estructura del ticket

```
[ABRE CAJÓN]
         CASA CEJA
    Sucursal: {SUCURSAL_NOMBRE}
       {SUCURSAL_DIRECCIÓN}
       {FECHA_ABONO}
       FOLIO: {FOLIO_ABONO}
       TICKET DE ABONO

CONCEPTO:
  [si tipo == crédito]  CREDITO CON FOLIO: {FOLIO_OPERACION}
  [si tipo == apartado] APARTADO CON FOLIO: {FOLIO_OPERACION}

----------------------------------------
PAGO T. DEBITO          {MONTO}       [si aplica]
PAGO T.CREDITO          {MONTO}       [si aplica]
PAGO CHEQUES            {MONTO}       [si aplica]
PAGO TRANSFERENCIA      {MONTO}       [si aplica]
EFECTIVO ENTREGADO      {MONTO}       [si aplica]
----------------------------------------
TOTAL ABONADO           {TOTAL_ABONADO}
----------------------------------------
POR PAGAR $             {SALDO_PENDIENTE}

      LE ATENDIO: {CAJERO_NOMBRE}
      GRACIAS POR SU PREFERENCIA

         RFC: {RFC}                              [si configurado]
[CORTA TICKET]
```

### Diferencias vs otros tickets
- No muestra lista de productos
- No tiene pie de ticket personalizado ni URL de facturación
- Tipo 0 = crédito, Tipo 1 = apartado (lógica para el texto de concepto)
- Muestra `TOTAL ABONADO` (no total de venta)

---

## 9. Reimprimir Apartado con Historial de Pagos

**Método original:** `LocaldataManager.reimprimirAbonosApartado()`  
**Tipo impresora:** Térmica  

### Estructura del ticket

```
[ABRE CAJÓN]
         CASA CEJA
    Sucursal: {SUCURSAL_NOMBRE}
       {SUCURSAL_DIRECCIÓN}
       {FECHA_APARTADO}
       FOLIO: {FOLIO_CORTE}

       TICKET DE APARTADO

Articulo        Can    P.Unit    Importe
----------------------------------------
{NOMBRE_PRODUCTO} {CANT} {PRECIO_VENTA} {PRECIO_VENTA × CANT}
... (muestra precio_venta tal cual, sin reconstruir descuentos)
----------------------------------------
Total               {TOTAL_APARTADO}
----------------------------------------
HISTORIAL DE PAGOS:                     [solo si hay abonos]

  [Por cada abono:]
  PAGO T. DEBITO          {MONTO}       [si aplica]
  PAGO T.CREDITO          {MONTO}       [si aplica]
  PAGO CHEQUES            {MONTO}       [si aplica]
  PAGO TRANSFERENCIA      {MONTO}       [si aplica]
  EFECTIVO ENTREGADO      {MONTO}       [si aplica]
  ----------------------------------------

POR PAGAR $             {TOTAL - TOTAL_PAGADO}

      LE ATENDIO: {CAJERO_NOMBRE}
    NO DE ARTICULOS: {NUM_ART 5 dígitos}

         RFC: {RFC}                              [si configurado]
[CORTA TICKET]
```

**Nota:** Al reimprimir, los productos se deserializan desde JSON guardado. No se reconstruyen descuentos originales. Se muestra `precio_venta` tal como fue guardado.

---

## 10. Reimprimir Crédito con Historial de Pagos

**Método original:** `LocaldataManager.reimprimirAbonosCredito()`  
**Tipo impresora:** Térmica  
**Formato:** Idéntico a Reimprimir Apartado, pero con encabezado `TICKET DE CREDITO` y folio del crédito.

---

## 11. Historial de Cortes (Térmica)

**Clase original:** `HistorialCortes.cs`  
**Formato:** Idéntico al Ticket de Corte CZ Térmico (sección 4).  
**Diferencia:** Se llama desde el historial, el folio ya está cerrado.

---

## 12. Reimprimir Venta (Carta)

**Clase original:** `VerOperaciones.cs`  
**Tipo impresora:** Carta (igual que Ticket de Venta Carta)  
**Nota:** Usa el mismo formato que la venta carta original pero los datos vienen de la BD.

### Estructura (igual que sección 3 pero con datos recuperados)

```
CASA CEJA
SUCURSAL: {SUCURSAL_NOMBRE}
{SUCURSAL_DIRECCIÓN}
{FECHA_VENTA}
FOLIO: {FOLIO}

DESCRIPCION\tCANT\tP. UNIT\tP. TOTAL
{productos desde BD...}

...subtotales, descuentos, pagos, cambio...

LE ATENDIO: {CAJERO}
NO DE ARTICULOS: {CANT}
GRACIAS POR SU COMPRA

RFC: {RFC}

{PIE_DE_TICKET}

SI DESEA FACTURAR ESTA COMPRA INGRESE A :
https://cm-papeleria.com/public/facturacion
```

---

## 13. Indicadores de descuento en productos

Los indicadores se agregan concatenados al nombre del producto en la línea del ticket:

| Indicador | Condición | Ejemplo |
|-----------|-----------|---------|
| `*ESP`    | `es_precio_especial == true` | `CUADERNO A5*ESP` |
| `*CAT{X}%` | `tuvo_descuento_categoria_original == true` | `CUADERNO A5*CAT10%` |

**Reglas:**
- Si tiene ambos: `*ESP *CAT10%` (separados por espacio)
- El `*CAT` usa `porcentaje_categoria_original` (valor guardado, no recalculado)
- En reimprimir, solo se muestran si los campos están disponibles en los datos guardados

---

## 14. Notas de implementación para el Remake

### TicketService — métodos a implementar/completar

```csharp
// Ya implementados (mejorar):
string GenerateTicketText(TicketData ticket)                    // Venta térmica
string GenerateTicketText(TicketData ticket, TicketType type)   // Crédito/Apartado

// Pendientes:
string GenerateCashCloseTicket(CashCloseData data)             // Corte CZ
string GeneratePaymentTicket(PaymentData data, OperationType type)  // Abono
string GenerateReprintTicket(TicketData ticket, TicketType type)    // Reimpresión con historial
```

### PrintService — configuración de papel

```csharp
// Térmica
PaperType.Thermal:
  width  = 78mm (≈ 500 units en WinForms)
  height = variable (automático según contenido)
  margins = mínimos (10,10,10,10)

// Carta
PaperType.Letter:
  width  = 8.5 pulgadas (850 units / 21.6cm)
  height = 11 pulgadas  (1100 units / 27.9cm)
  margins = 50,50,50,50
  font   = configurable (fontName, fontSize desde PosTerminalConfig)
  tabs   = ver tabla en sección 3
```

### Apertura de cajón
- Venta térmica: Solo abre si NO es reimpresión
- Apartado, Crédito, Abono: Siempre abre cajón
- Corte, reimprimir: NO abre cajón

### Campos del modelo que alimentan los tickets

```
ProductoVenta (CartItem en el remake):
  - precio_venta              → precio final con todos los descuentos
  - precio_original           → precio menudeo original (para P.UNIT en ticket)
  - es_precio_especial        → bool
  - descuento_unitario        → descuento por precio especial por unidad
  - tuvo_descuento_categoria_original → bool (para indicador *CAT en ticket)
  - descuento_categoria_original      → monto desc categoría por unidad (valor guardado)
  - porcentaje_categoria_original     → % desc categoría (para mostrar en *CAT{X}%)
```

---

*Documento generado: Febrero 2026*  
*Fuente: PuntoVentaCasaCeja — Ventas.cs, LocaldataManager.cs, HistorialCortes.cs, VerOperaciones.cs*

# ðŸ“Š REGLAS DE NEGOCIO: Sistema de Cortes de Caja
## Casa Ceja - EspecificaciÃ³n Completa

**VersiÃ³n:** 1.0  
**Fecha:** 2 de Febrero, 2026  
**Autor:** AnÃ¡lisis de Sistema  
**PropÃ³sito:** Documento de referencia para implementaciÃ³n del mÃ³dulo de cortes de caja

---

## ðŸ“‘ Ãndice

1. [IntroducciÃ³n](#introducciÃ³n)
2. [Conceptos Fundamentales](#conceptos-fundamentales)
3. [Flujo del Efectivo en Caja](#flujo-del-efectivo-en-caja)
4. [Campos del Corte: Definiciones](#campos-del-corte-definiciones)
5. [FÃ³rmulas y CÃ¡lculos](#fÃ³rmulas-y-cÃ¡lculos)
6. [Reglas de Negocio](#reglas-de-negocio)
7. [Validaciones Requeridas](#validaciones-requeridas)
8. [Ejemplos PrÃ¡cticos](#ejemplos-prÃ¡cticos)
9. [Casos Especiales](#casos-especiales)
10. [Integridad de Datos](#integridad-de-datos)

---

## 1. IntroducciÃ³n

### Objetivo del Sistema de Cortes

El sistema de cortes de caja tiene como objetivo principal:
- Registrar todas las transacciones monetarias del dÃ­a
- Controlar el efectivo fÃ­sico en caja
- Identificar diferencias entre efectivo esperado y real
- Generar auditorÃ­a completa de movimientos
- Separar claramente efectivo de otros mÃ©todos de pago

### Principio Rector

> **"El corte de caja refleja VENTAS realizadas, no necesariamente DINERO cobrado"**

Esto significa que:
- Una venta a crÃ©dito SUMA al total del corte (es una venta realizada)
- Pero NO suma al efectivo de caja (no se cobrÃ³ todavÃ­a)
- Un abono posterior SÃ suma al efectivo de caja

---

## 2. Conceptos Fundamentales

### 2.1 Diferencia: Total del Corte vs Efectivo Total

**TOTAL DEL CORTE**
- Representa el valor TOTAL de todas las ventas del turno
- Incluye ventas pagadas con tarjetas, cheques, transferencias
- Incluye ventas a crÃ©dito (aunque no estÃ©n pagadas aÃºn)
- Incluye apartados (aunque no estÃ©n pagados aÃºn)
- Es un indicador de PRODUCTIVIDAD

**EFECTIVO TOTAL**
- Representa el dinero FÃSICO que pasÃ³ por la caja
- Solo incluye transacciones en efectivo
- No incluye tarjetas, cheques, transferencias
- Es lo que debe estar FÃSICAMENTE en la caja

**Ejemplo ilustrativo:**
```
Turno del dÃ­a:
- Venta 1: $1,000 en efectivo
- Venta 2: $500 con tarjeta de dÃ©bito
- Venta 3: $2,000 a crÃ©dito (sin pagar aÃºn)

TOTAL DEL CORTE = $3,500  (todas las ventas)
EFECTIVO TOTAL  = $1,000  (solo el efectivo fÃ­sico)
```

### 2.2 El Cambio NO es un Gasto

**REGLA CRÃTICA:** El cambio dado a los clientes NO se contabiliza como gasto ni reduce el efectivo.

**Â¿Por quÃ©?**
- El cambio ya estÃ¡ implÃ­cito en la diferencia entre "monto recibido" y "total de venta"
- Si un cliente paga $1,250 por una venta de $1,200, los $50 de cambio ya estÃ¡n contemplados

**Flujo correcto:**
```
Entra a caja:   +$1,250
Sale de caja:   -$50 (cambio)
Neto en caja:   = $1,200 âœ“
```

**En el corte se registra:**
- Total de venta: $1,200 (NO $1,250)
- Efectivo en caja: +$1,200 (el neto correcto)

**En la base de datos se guarda (para auditorÃ­a):**
- Total: $1,200
- AmountPaid: $1,250
- ChangeGiven: $50

Pero en el corte solo importa el **Total** de la venta.

### 2.3 Tipos de Transacciones

El sistema maneja 4 tipos principales de transacciones:

**A. VENTAS DIRECTAS**
- Se pagan completamente en el momento
- Pueden ser en efectivo, tarjeta, cheque, transferencia
- Se registran en el corte segÃºn su mÃ©todo de pago

**B. VENTAS A CRÃ‰DITO**
- Se vende la mercancÃ­a pero el pago es diferido
- Se divide en "enganche inicial" + "pagos mensuales"
- El total de la venta cuenta en el corte
- Solo el enganche (si es en efectivo) cuenta como efectivo

**C. APARTADOS**
- Similar a crÃ©ditos pero con mercancÃ­a retenida
- Requiere abonos hasta completar el total
- Se entrega la mercancÃ­a cuando se pague completo
- El total del apartado cuenta en el corte
- Solo los abonos en efectivo cuentan como efectivo

**D. MOVIMIENTOS DE CAJA**
- Gastos: Dinero que sale de caja (gasolina, papelerÃ­a, etc.)
- Ingresos: Dinero que entra a caja (reembolsos, otros ingresos)

---

## 3. Flujo del Efectivo en Caja

### 3.1 Apertura de Caja

Al inicio del turno:
1. El cajero cuenta el dinero inicial (fondo de apertura)
2. Este monto se registra en el sistema
3. Este es el "punto cero" para calcular todo lo demÃ¡s

**Fondo de Apertura = Dinero inicial en caja**

### 3.2 Durante el Turno

El efectivo en caja cambia por:

**AUMENTA (+) con:**
- Ventas directas en efectivo
- Abonos a crÃ©ditos (en efectivo)
- Abonos a apartados (en efectivo)
- Enganches de crÃ©ditos nuevos (en efectivo)
- Ingresos extraordinarios

**DISMINUYE (-) con:**
- Gastos operativos
- Retiros de efectivo

**NO AFECTA el efectivo:**
- Ventas con tarjeta (el dinero no estÃ¡ en caja)
- Ventas con cheque (hasta que se deposite)
- Ventas con transferencia (el dinero no estÃ¡ en caja)
- CrÃ©ditos sin enganche
- Apartados sin abono inicial

### 3.3 Cierre de Caja

Al final del turno:
1. El cajero cuenta el efectivo fÃ­sico
2. El sistema calcula cuÃ¡nto efectivo DEBERÃA haber
3. Se comparan ambas cifras
4. La diferencia es el sobrante o faltante

**FÃ³rmula del efectivo esperado:**
```
Efectivo Esperado = 
    Fondo de Apertura
  + Efectivo de ventas directas
  + Efectivo de abonos a crÃ©ditos
  + Efectivo de abonos a apartados
  + Ingresos
  - Gastos
```

**Diferencia:**
```
Sobrante/Faltante = Efectivo Contado - Efectivo Esperado
```

---

## 4. Campos del Corte: Definiciones

### 4.1 CAMPOS DE IDENTIFICACIÃ“N

**FOLIO**
- Tipo: Texto
- GeneraciÃ³n: AutomÃ¡tica
- Formato: `SUCURSAL-AAAAMMDD-CONSECUTIVO` (ej: "001-20260202-0001")
- PropÃ³sito: Identificador Ãºnico del corte
- Regla: Nunca se repite, incrementa por dÃ­a y sucursal

**FECHA Y HORA DE APERTURA**
- Tipo: Fecha/Hora
- GeneraciÃ³n: AutomÃ¡tica
- Valor: Momento exacto en que se abriÃ³ la caja
- PropÃ³sito: Delimitar inicio del turno

**FECHA Y HORA DE CIERRE**
- Tipo: Fecha/Hora
- GeneraciÃ³n: AutomÃ¡tica
- Valor: Momento exacto en que se cierra la caja
- PropÃ³sito: Delimitar fin del turno

**USUARIO (CAJERO)**
- Tipo: Referencia
- Valor: Usuario que abriÃ³ la caja
- PropÃ³sito: Identificar responsable del turno
- Nota: Un corte pertenece a UN solo cajero

**SUCURSAL**
- Tipo: Referencia
- Valor: Sucursal donde se realizÃ³ el corte
- PropÃ³sito: Separar cortes por ubicaciÃ³n fÃ­sica

**ESTADO**
- Tipo: EnumeraciÃ³n
- Valores posibles: "abierto", "cerrado"
- Regla: Solo puede haber UNA caja abierta por sucursal/usuario

---

### 4.2 CAMPOS DE APERTURA

**FONDO DE APERTURA**
- Tipo: Decimal (dinero)
- Origen: INPUT manual del cajero
- DefiniciÃ³n: Efectivo inicial en caja al comenzar el turno
- ValidaciÃ³n: Debe ser >= 0
- PropÃ³sito: Establecer el "punto cero" del efectivo
- Ejemplo: Si el cajero inicia con $500, este es el fondo de apertura

---

### 4.3 CAMPOS DE VENTAS DIRECTAS (POR MÃ‰TODO DE PAGO)

Estos campos representan ventas que se pagaron COMPLETAMENTE en el momento.

**EFECTIVO DIRECTO**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma de todas las ventas pagadas completamente en efectivo
- Excluye: CrÃ©ditos y apartados (esos van en sus propios campos)
- Incluye: El TOTAL de cada venta, no el monto recibido
- FÃ³rmula: `SUM(ventas donde mÃ©todo_pago = 'Efectivo' AND tipo = 'directa')`

**AclaraciÃ³n sobre el cambio:**
```
Si una venta es de $1,200 y el cliente paga $1,250:
- Se suma al corte: $1,200 (el total)
- NO se suma: $1,250 (el monto recibido)
- El cambio de $50 ya estÃ¡ implÃ­cito
```

**TOTAL TARJETA DE DÃ‰BITO**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma de ventas pagadas con tarjeta de dÃ©bito
- FÃ³rmula: `SUM(ventas donde mÃ©todo_pago = 'TarjetaDebito')`
- Nota: Este dinero NO estÃ¡ fÃ­sicamente en caja

**TOTAL TARJETA DE CRÃ‰DITO**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma de ventas pagadas con tarjeta de crÃ©dito
- FÃ³rmula: `SUM(ventas donde mÃ©todo_pago = 'TarjetaCredito')`
- Nota: Este dinero NO estÃ¡ fÃ­sicamente en caja

**TOTAL EN CHEQUES**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma de ventas pagadas con cheque
- FÃ³rmula: `SUM(ventas donde mÃ©todo_pago = 'Cheque')`
- Nota: Los cheques estÃ¡n fÃ­sicamente en caja pero no son efectivo

**TOTAL EN TRANSFERENCIAS**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma de ventas pagadas con transferencia bancaria
- FÃ³rmula: `SUM(ventas donde mÃ©todo_pago = 'Transferencia')`
- Nota: Este dinero NO estÃ¡ fÃ­sicamente en caja

---

### 4.4 CAMPOS DE CRÃ‰DITOS

**TOTAL DE CRÃ‰DITOS CREADOS**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma TOTAL de todos los crÃ©ditos generados en el turno
- Incluye: El valor completo del crÃ©dito (aunque no estÃ© pagado aÃºn)
- FÃ³rmula: `SUM(creditos.total donde fecha_creaciÃ³n = turno)`
- PropÃ³sito: Medir productividad en ventas a crÃ©dito
- Nota: Este monto SÃ suma al "Total del Corte"

**EFECTIVO DE CRÃ‰DITOS**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Efectivo recibido por abonos a crÃ©ditos
- Incluye:
  - Enganches iniciales (si se pagaron en efectivo)
  - Abonos a crÃ©ditos anteriores (si se pagaron en efectivo)
- Excluye:
  - Enganches/abonos pagados con tarjeta
- FÃ³rmula: `SUM(pagos_credito donde mÃ©todo = 'Efectivo' AND fecha = turno)`
- PropÃ³sito: Contabilizar el efectivo fÃ­sico de crÃ©ditos
- Nota: Este monto SÃ suma al "Efectivo Total"

**Ejemplo:**
```
Se crea un crÃ©dito de $10,000 con enganche de $2,000 en efectivo:
- Total de CrÃ©ditos Creados: +$10,000
- Efectivo de CrÃ©ditos: +$2,000
- Efectivo Total: +$2,000
- Total del Corte: +$10,000
```

---

### 4.5 CAMPOS DE APARTADOS

**TOTAL DE APARTADOS CREADOS**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma TOTAL de todos los apartados generados en el turno
- Incluye: El valor completo del apartado (aunque no estÃ© pagado aÃºn)
- FÃ³rmula: `SUM(apartados.total donde fecha_creaciÃ³n = turno)`
- PropÃ³sito: Medir productividad en apartados
- Nota: Este monto SÃ suma al "Total del Corte"

**EFECTIVO DE APARTADOS**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Efectivo recibido por abonos a apartados
- Incluye:
  - Abonos iniciales (si se pagaron en efectivo)
  - Abonos a apartados anteriores (si se pagaron en efectivo)
- Excluye:
  - Abonos pagados con tarjeta
- FÃ³rmula: `SUM(pagos_apartado donde mÃ©todo = 'Efectivo' AND fecha = turno)`
- PropÃ³sito: Contabilizar el efectivo fÃ­sico de apartados
- Nota: Este monto SÃ suma al "Efectivo Total"

---

### 4.6 CAMPOS DE MOVIMIENTOS DE CAJA

**GASTOS**
- Tipo: Decimal
- Origen: CALCULADO (suma de movimientos tipo "gasto")
- DefiniciÃ³n: Dinero SACADO de la caja para gastos operativos
- Ejemplos: Gasolina, papelerÃ­a, propinas, reparaciones menores
- Efecto: REDUCE el efectivo en caja
- FÃ³rmula: `SUM(movimientos donde tipo = 'gasto' AND turno_actual)`
- ValidaciÃ³n: Cada gasto debe tener concepto obligatorio
- Nota: Se registran en tabla separada con concepto y monto

**INGRESOS**
- Tipo: Decimal
- Origen: CALCULADO (suma de movimientos tipo "ingreso")
- DefiniciÃ³n: Dinero AGREGADO a la caja (no por ventas)
- Ejemplos: Reembolsos, devoluciones de gastos, otros ingresos
- Efecto: AUMENTA el efectivo en caja
- FÃ³rmula: `SUM(movimientos donde tipo = 'ingreso' AND turno_actual)`
- ValidaciÃ³n: Cada ingreso debe tener concepto obligatorio
- Nota: Se registran en tabla separada con concepto y monto

---

### 4.7 CAMPOS DE CIERRE

**EFECTIVO REPORTADO**
- Tipo: Decimal
- Origen: INPUT manual del cajero
- DefiniciÃ³n: Efectivo CONTADO fÃ­sicamente al cerrar
- Proceso: El cajero cuenta billetes y monedas manualmente
- ValidaciÃ³n: Debe ser >= 0
- PropÃ³sito: Comparar con el efectivo esperado

**EFECTIVO ESPERADO**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Efectivo que DEBERÃA estar en caja segÃºn el sistema
- FÃ³rmula: Ver secciÃ³n 5.3
- PropÃ³sito: Base de comparaciÃ³n para detectar diferencias

**SOBRANTE / FALTANTE**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Diferencia entre efectivo reportado y esperado
- FÃ³rmula: `Sobrante = Efectivo Reportado - Efectivo Esperado`
- InterpretaciÃ³n:
  - `> 0` = Sobrante (hay MÃS dinero del esperado)
  - `= 0` = Cuadrado perfecto
  - `< 0` = Faltante (hay MENOS dinero del esperado)
- PropÃ³sito: Detectar errores, robos o problemas

---

### 4.8 CAMPOS TOTALIZADORES

**TOTAL DEL CORTE**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Suma de TODAS las ventas del turno
- Incluye:
  - Efectivo directo
  - Tarjetas (dÃ©bito y crÃ©dito)
  - Cheques
  - Transferencias
  - Total de crÃ©ditos creados
  - Total de apartados creados
- FÃ³rmula: Ver secciÃ³n 5.1
- PropÃ³sito: Medir productividad total del turno
- Nota: NO representa efectivo fÃ­sico

**EFECTIVO TOTAL**
- Tipo: Decimal
- Origen: CALCULADO
- DefiniciÃ³n: Todo el efectivo que pasÃ³ por la caja
- Incluye:
  - Fondo de apertura
  - Efectivo directo
  - Efectivo de crÃ©ditos
  - Efectivo de apartados
  - Ingresos
  - (Menos) Gastos
- FÃ³rmula: Ver secciÃ³n 5.2
- PropÃ³sito: Saber cuÃ¡nto efectivo manejÃ³ el cajero
- Nota: DeberÃ­a coincidir con el efectivo esperado

---

## 5. FÃ³rmulas y CÃ¡lculos

### 5.1 TOTAL DEL CORTE

```
Total del Corte = 
    Efectivo Directo
  + Total Tarjeta DÃ©bito
  + Total Tarjeta CrÃ©dito
  + Total Cheques
  + Total Transferencias
  + Total de CrÃ©ditos Creados
  + Total de Apartados Creados
```

**ExplicaciÃ³n:**
- Suma TODAS las ventas sin importar cÃ³mo se pagaron
- Los crÃ©ditos y apartados suman por su valor TOTAL (no solo el abono)
- Representa el valor total de mercancÃ­a vendida

**Ejemplo numÃ©rico:**
```
Efectivo Directo:        $5,000
Tarjeta DÃ©bito:          $3,000
Tarjeta CrÃ©dito:         $2,000
Cheques:                 $1,000
Transferencias:            $500
CrÃ©ditos Creados:       $10,000 (total, aunque solo se dio $2,000 de enganche)
Apartados Creados:       $4,000 (total, aunque solo se dio $500 de abono)
                        -------
TOTAL DEL CORTE:        $25,500
```

---

### 5.2 EFECTIVO TOTAL

```
Efectivo Total = 
    Fondo de Apertura
  + Efectivo Directo
  + Efectivo de CrÃ©ditos
  + Efectivo de Apartados
  + Ingresos
  - Gastos
```

**ExplicaciÃ³n:**
- Suma solo transacciones EN EFECTIVO
- No incluye tarjetas, cheques, transferencias
- Representa cuÃ¡nto efectivo pasÃ³ por las manos del cajero

**Ejemplo numÃ©rico:**
```
Fondo de Apertura:       $1,000
Efectivo Directo:        $5,000
Efectivo de CrÃ©ditos:    $2,000 (enganches/abonos en efectivo)
Efectivo de Apartados:     $500 (abonos en efectivo)
Ingresos:                  $200 (reembolso de gasolina)
Gastos:                   -$300 (gasolina para repartidor)
                         -------
EFECTIVO TOTAL:          $8,400
```

---

### 5.3 EFECTIVO ESPERADO

```
Efectivo Esperado = 
    Fondo de Apertura
  + Efectivo Directo
  + Efectivo de CrÃ©ditos
  + Efectivo de Apartados
  + Ingresos
  - Gastos
```

**Nota:** Esta fÃ³rmula es idÃ©ntica a "Efectivo Total"

**Â¿Por quÃ©?** 
- Porque representa lo mismo: cuÃ¡nto efectivo deberÃ­a haber
- Se usa como base para comparar con lo contado

---

### 5.4 SOBRANTE / FALTANTE

```
Sobrante = Efectivo Reportado - Efectivo Esperado
```

**InterpretaciÃ³n:**

| Resultado | Significado | Ejemplo |
|-----------|-------------|---------|
| `Sobrante > 0` | Hay MÃS efectivo del esperado | Reportado: $8,500, Esperado: $8,400 â†’ Sobrante: $100 |
| `Sobrante = 0` | Cuadra perfectamente | Reportado: $8,400, Esperado: $8,400 â†’ Sobrante: $0 |
| `Sobrante < 0` | Hay MENOS efectivo del esperado | Reportado: $8,300, Esperado: $8,400 â†’ Faltante: -$100 |

**Causas comunes de diferencias:**
- Error al dar cambio
- Olvido de registrar un gasto
- Olvido de registrar un ingreso
- Error al contar el efectivo
- Error en el sistema
- (En casos graves) Robo o pÃ©rdida

---

### 5.5 CÃLCULO DE EFECTIVO DIRECTO

```
Efectivo Directo = SUM(ventas.total) 
WHERE 
    mÃ©todo_pago = 'Efectivo' 
    AND tipo_venta = 'directa' 
    AND fecha_venta BETWEEN apertura AND cierre
    AND sucursal = sucursal_actual
```

**Importante:**
- Se suma el campo `total` de cada venta (NO `amount_paid`)
- El cambio ya estÃ¡ implÃ­cito en esta operaciÃ³n
- Solo cuenta ventas directas (excluye crÃ©ditos/apartados)

---

### 5.6 CÃLCULO DE EFECTIVO DE CRÃ‰DITOS

```
Efectivo de CrÃ©ditos = SUM(pagos_credito.monto)
WHERE
    mÃ©todo_pago = 'Efectivo'
    AND fecha_pago BETWEEN apertura AND cierre
    AND sucursal = sucursal_actual
```

**Incluye:**
- Enganches de crÃ©ditos nuevos (si fueron en efectivo)
- Abonos a crÃ©ditos anteriores (si fueron en efectivo)

---

### 5.7 CÃLCULO DE EFECTIVO DE APARTADOS

```
Efectivo de Apartados = SUM(pagos_apartado.monto)
WHERE
    mÃ©todo_pago = 'Efectivo'
    AND fecha_pago BETWEEN apertura AND cierre
    AND sucursal = sucursal_actual
```

**Incluye:**
- Abonos iniciales de apartados nuevos (si fueron en efectivo)
- Abonos a apartados anteriores (si fueron en efectivo)

---

### 5.8 CÃLCULO DE TOTAL POR MÃ‰TODO DE PAGO

Para cada mÃ©todo de pago (tarjetas, cheques, transferencias):

```
Total [MÃ©todo] = SUM(ventas.total)
WHERE
    mÃ©todo_pago = [MÃ©todo especÃ­fico]
    AND fecha_venta BETWEEN apertura AND cierre
    AND sucursal = sucursal_actual
```

Repetir para:
- TarjetaDebito
- TarjetaCredito
- Cheque
- Transferencia

---

## 6. Reglas de Negocio

### 6.1 REGLAS DE APERTURA

**R1.1: Solo una caja abierta por sucursal/cajero**
- No puede haber dos cajas abiertas simultÃ¡neamente para el mismo usuario
- Validar antes de permitir apertura

**R1.2: Fondo de apertura obligatorio**
- El cajero DEBE ingresar un monto de apertura (puede ser 0)
- El sistema registra la fecha/hora exacta de apertura

**R1.3: No se puede vender sin caja abierta**
- Todas las funciones de venta deben estar bloqueadas
- Mostrar mensaje: "Debe abrir caja antes de realizar ventas"

**R1.4: El fondo de apertura no se puede modificar**
- Una vez registrado, es inmutable
- Cualquier ajuste debe hacerse con movimientos de caja

---

### 6.2 REGLAS DURANTE EL TURNO

**R2.1: Todas las ventas deben asociarse al corte abierto**
- Cada venta registra el ID del corte de caja actual
- Esto permite calcular totales correctamente

**R2.2: Los movimientos de caja requieren concepto**
- Gastos e ingresos SIEMPRE deben tener descripciÃ³n
- Ejemplo: "Gasolina para repartidor", "Reembolso de cambio"

**R2.3: El cambio NO se registra como movimiento**
- El cambio estÃ¡ implÃ­cito en la venta
- No crear registros de "gasto" por dar cambio

**R2.4: Solo el efectivo afecta el efectivo esperado**
- Tarjetas, cheques, transferencias NO modifican el efectivo de caja
- Solo suman al total del corte

**R2.5: Los crÃ©ditos/apartados suman completo al corte**
- El valor TOTAL del crÃ©dito/apartado suma al "Total del Corte"
- Solo el enganche/abono en efectivo suma al "Efectivo Total"

---

### 6.3 REGLAS DE CIERRE

**R3.1: El cajero debe contar el efectivo manualmente**
- Sistema solicita el "Efectivo Reportado"
- El cajero ingresa lo que contÃ³ fÃ­sicamente

**R3.2: El sistema calcula el efectivo esperado automÃ¡ticamente**
- No se puede modificar manualmente
- Se calcula con la fÃ³rmula definida

**R3.3: El sistema calcula el sobrante/faltante automÃ¡ticamente**
- Diferencia simple: Reportado - Esperado
- Se muestra claramente si hay diferencia

**R3.4: El corte debe mostrar todos los detalles**
- Desglose completo de ventas por mÃ©todo de pago
- Lista de gastos e ingresos
- Todos los cÃ¡lculos visibles

**R3.5: DespuÃ©s del corte, la caja queda cerrada**
- Estado cambia de "abierto" a "cerrado"
- No se pueden hacer mÃ¡s ventas con ese corte
- Para vender nuevamente, se debe abrir una nueva caja

**R3.6: Los cortes son inmutables**
- Una vez cerrado, no se puede editar
- Cualquier correcciÃ³n requiere un ajuste en el siguiente turno

---

### 6.4 REGLAS DE FECHAS Y TURNOS

**R4.1: Un turno pertenece a un dÃ­a calendario**
- Si se abre a las 11:00 PM y se cierra al dÃ­a siguiente a las 2:00 AM
- El corte pertenece al dÃ­a en que se ABRIÃ“

**R4.2: Los cÃ¡lculos usan rangos de fecha/hora exactos**
- Desde: fecha_apertura
- Hasta: fecha_cierre
- Todas las transacciones en ese rango se incluyen

**R4.3: No puede haber traslape de turnos**
- Validar que no haya transacciones "huÃ©rfanas"
- Todas las ventas deben pertenecer a un corte

---

### 6.5 REGLAS DE AUDITORÃA

**R5.1: Todos los cortes se guardan permanentemente**
- Nunca se borran
- Se marcan como cerrados

**R5.2: Cada corte tiene folio Ãºnico**
- Formato: SUCURSAL-FECHA-CONSECUTIVO
- Permite trazabilidad completa

**R5.3: Los tickets de venta referencian el corte**
- Cada venta guarda el ID del corte
- Permite reconstruir cualquier corte histÃ³rico

**R5.4: Las diferencias deben justificarse**
- Si hay sobrante/faltante, el cajero debe explicarlo
- Se recomienda campo de "observaciones"

---

## 7. Validaciones Requeridas

### 7.1 VALIDACIONES DE APERTURA

| Campo | ValidaciÃ³n | Mensaje de Error |
|-------|-----------|------------------|
| Fondo de Apertura | >= 0 | "El fondo de apertura no puede ser negativo" |
| Usuario | Existe en BD | "Usuario no vÃ¡lido" |
| Sucursal | Existe en BD | "Sucursal no vÃ¡lida" |
| Estado | No hay caja abierta | "Ya existe una caja abierta para este usuario/sucursal" |

---

### 7.2 VALIDACIONES DE MOVIMIENTOS

| Campo | ValidaciÃ³n | Mensaje de Error |
|-------|-----------|------------------|
| Monto de gasto | > 0 | "El monto debe ser mayor a cero" |
| Monto de ingreso | > 0 | "El monto debe ser mayor a cero" |
| Concepto | No vacÃ­o, mÃ­nimo 3 caracteres | "Debe especificar el concepto" |
| Monto vs Efectivo | Gasto <= Efectivo disponible | "No hay suficiente efectivo en caja" |

---

### 7.3 VALIDACIONES DE CIERRE

| Campo | ValidaciÃ³n | Mensaje de Error |
|-------|-----------|------------------|
| Efectivo Reportado | >= 0 | "El efectivo reportado no puede ser negativo" |
| Caja abierta | Estado = "abierto" | "No hay caja abierta para cerrar" |
| Ventas pendientes | No hay ventas sin completar | "Hay ventas pendientes de completar" |

---

### 7.4 VALIDACIONES DE NEGOCIO

**V4.1: Diferencia mÃ¡xima permitida**
- Si `|Sobrante| > LIMITE_DIFERENCIA` (ej: $100)
- Solicitar autorizaciÃ³n de supervisor
- O al menos mostrar advertencia fuerte

**V4.2: ValidaciÃ³n de horarios**
- Turno no puede durar mÃ¡s de 24 horas
- Advertir si el turno es muy largo (>12 horas)

**V4.3: ValidaciÃ³n de montos extremos**
- Advertir si hay gastos muy grandes (>$1,000)
- Advertir si hay ingresos muy grandes
- Prevenir errores de captura

---

## 8. Ejemplos PrÃ¡cticos

### Ejemplo 1: Turno Simple

**Escenario:**
- Apertura: $500
- 10 ventas en efectivo por $5,000
- 5 ventas con tarjeta de dÃ©bito por $2,000
- 1 gasto de $200 (gasolina)
- Cierre: Se cuentan $5,280

**CÃ¡lculos:**

```
EFECTIVO ESPERADO:
  Fondo de apertura:     $500
  + Efectivo directo:  $5,000
  - Gastos:             -$200
  = Esperado:          $5,300

EFECTIVO REPORTADO:    $5,280

SOBRANTE/FALTANTE:
  $5,280 - $5,300 = -$20 (FALTANTE)

TOTAL DEL CORTE:
  Efectivo directo:    $5,000
  + Tarjeta dÃ©bito:    $2,000
  = Total:             $7,000
```

**Resultado:**
- Productividad: $7,000
- Efectivo manejado: $5,300 esperado
- Diferencia: Falta $20 (investigar)

---

### Ejemplo 2: Turno con CrÃ©ditos

**Escenario:**
- Apertura: $1,000
- 5 ventas en efectivo por $3,000
- 2 crÃ©ditos creados:
  - CrÃ©dito 1: Total $10,000, enganche $2,000 en efectivo
  - CrÃ©dito 2: Total $5,000, enganche $1,000 en efectivo
- Cierre: Se cuentan $7,010

**CÃ¡lculos:**

```
TOTAL DE CRÃ‰DITOS CREADOS:
  $10,000 + $5,000 = $15,000

EFECTIVO DE CRÃ‰DITOS:
  $2,000 + $1,000 = $3,000

EFECTIVO ESPERADO:
  Fondo de apertura:     $1,000
  + Efectivo directo:    $3,000
  + Efectivo crÃ©ditos:   $3,000
  = Esperado:            $7,000

EFECTIVO REPORTADO:      $7,010

SOBRANTE/FALTANTE:
  $7,010 - $7,000 = +$10 (SOBRANTE)

TOTAL DEL CORTE:
  Efectivo directo:      $3,000
  + CrÃ©ditos creados:   $15,000
  = Total:              $18,000
```

**Resultado:**
- Productividad: $18,000 (excelente por los crÃ©ditos)
- Efectivo manejado: $7,000
- Diferencia: Sobran $10 (aceptable)

---

### Ejemplo 3: Turno con Todo

**Escenario:**
- Apertura: $800
- Ventas:
  - Efectivo: $4,000
  - Tarjeta dÃ©bito: $2,000
  - Tarjeta crÃ©dito: $1,500
  - Transferencia: $500
- CrÃ©ditos:
  - Nuevo: Total $8,000, enganche $1,500 efectivo
  - Abono anterior: $500 efectivo
- Apartados:
  - Nuevo: Total $3,000, abono $300 efectivo
  - Abono anterior: $200 efectivo
- Movimientos:
  - Gasto: $150 (papelerÃ­a)
  - Ingreso: $100 (reembolso)
- Cierre: Se cuentan $6,750

**CÃ¡lculos:**

```
TOTAL DE CRÃ‰DITOS: $8,000
EFECTIVO DE CRÃ‰DITOS: $1,500 + $500 = $2,000

TOTAL DE APARTADOS: $3,000
EFECTIVO DE APARTADOS: $300 + $200 = $500

EFECTIVO ESPERADO:
  Fondo de apertura:      $800
  + Efectivo directo:   $4,000
  + Efectivo crÃ©ditos:  $2,000
  + Efectivo apartados:   $500
  + Ingresos:             $100
  - Gastos:              -$150
  = Esperado:           $7,250

EFECTIVO REPORTADO:     $6,750

SOBRANTE/FALTANTE:
  $6,750 - $7,250 = -$500 (FALTANTE IMPORTANTE)

TOTAL DEL CORTE:
  Efectivo directo:     $4,000
  + Tarjeta dÃ©bito:     $2,000
  + Tarjeta crÃ©dito:    $1,500
  + Transferencia:        $500
  + CrÃ©ditos creados:   $8,000
  + Apartados creados:  $3,000
  = Total:             $19,000
```

**Resultado:**
- Productividad: $19,000 (excelente)
- Efectivo esperado: $7,250
- Diferencia: FALTA $500 âš ï¸ (REQUIERE INVESTIGACIÃ“N)

**Posibles causas del faltante:**
- Error al dar cambio
- Venta en efectivo no registrada
- Gasto no registrado
- Error al contar
- Problema grave (robo)

---

## 9. Casos Especiales

### 9.1 VENTAS MIXTAS (MÃšLTIPLES MÃ‰TODOS DE PAGO)

**SituaciÃ³n:**
Un cliente compra $1,000 pero paga:
- $400 en efectivo
- $600 con tarjeta

**SoluciÃ³n:**
En el modelo actual NO estÃ¡ implementado pagos mixtos. RecomendaciÃ³n para futuro:
- Guardar en campo JSON los detalles: `{"efectivo": 400, "tarjeta": 600}`
- En el corte:
  - Efectivo directo: +$400
  - Tarjeta dÃ©bito: +$600
  - Total del corte: +$1,000

**Nota:** Por ahora, forzar a elegir UN solo mÃ©todo de pago principal.

---

### 9.2 DEVOLUCIONES

**SituaciÃ³n:**
Un cliente devuelve un producto comprado hace 2 dÃ­as por $500 (pagado en efectivo).

**SoluciÃ³n:**
- Crear un movimiento de caja tipo "gasto" con concepto "DevoluciÃ³n"
- Monto: -$500
- Esto reduce el efectivo correctamente
- En el "Total del Corte" NO afecta (fue una venta de otro dÃ­a)

**Nota:** Las devoluciones del mismo dÃ­a deben cancelar la venta original.

---

### 9.3 ERRORES DE CAPTURA

**SituaciÃ³n:**
Se registrÃ³ una venta de $1,000 pero debiÃ³ ser $100.

**SoluciÃ³n durante el turno:**
- Cancelar la venta incorrecta
- Crear la venta correcta
- El sistema recalcula automÃ¡ticamente

**SoluciÃ³n despuÃ©s del cierre:**
- El corte cerrado NO se modifica
- Crear ajuste en el siguiente turno
- Documentar en observaciones

---

### 9.4 CAMBIO DE TURNO SIN CERRAR

**SituaciÃ³n:**
Un cajero termina su turno pero olvida cerrar la caja.

**SoluciÃ³n:**
- El siguiente cajero NO puede abrir su caja
- Requiere que supervisor cierre el corte anterior
- O implementar "cierre forzado" con contraseÃ±a admin

---

### 9.5 CORTE CON DIFERENCIA GRANDE

**SituaciÃ³n:**
Falta $1,000 en el corte.

**Proceso:**
1. Reconteo de efectivo
2. RevisiÃ³n de todas las transacciones
3. Buscar ventas no registradas
4. Buscar gastos no registrados
5. Si persiste: escalamiento a supervisor
6. DocumentaciÃ³n obligatoria del faltante
7. Posible responsabilidad del cajero

---

### 9.6 MÃšLTIPLES SUCURSALES

**Regla:**
- Cada sucursal tiene sus propios cortes independientes
- Un usuario puede tener cortes abiertos en diferentes sucursales
- Los cÃ¡lculos SIEMPRE filtran por sucursal

**Ejemplo:**
```
Usuario: Juan
Sucursal A: Corte abierto desde las 8 AM
Sucursal B: Corte abierto desde las 2 PM
```
Es vÃ¡lido porque son sucursales diferentes.

---

## 10. Integridad de Datos

### 10.1 RELACIONES DE BASE DE DATOS

**Cortes de Caja deben relacionarse con:**
- Usuario (cajero)
- Sucursal
- Ventas (FK: cash_close_id en tabla sales)
- Movimientos de caja (FK: cash_close_id en tabla cash_movements)

**Restricciones:**
- No se puede borrar un corte si tiene ventas asociadas
- No se puede borrar un usuario si tiene cortes
- No se puede borrar una sucursal si tiene cortes

---

### 10.2 CAMPOS AUDITABLES

Todos los registros de cortes deben tener:
- `id` (autoincremental)
- `created_at` (fecha de creaciÃ³n)
- `updated_at` (Ãºltima modificaciÃ³n)
- `folio` (Ãºnico)
- Usuario que creÃ³
- Usuario que modificÃ³ (si aplica)

---

### 10.3 RESPALDO DE DATOS

**CrÃ­tico:** Los cortes de caja son documentos financieros legales.

**Recomendaciones:**
- Backup diario de la base de datos
- Logs de todas las operaciones de cortes
- Exportar cortes a PDF al cerrar (respaldo fÃ­sico)
- SincronizaciÃ³n con servidor central

---

### 10.4 SINCRONIZACIÃ“N

Si el sistema tiene mÃºltiples sucursales:
- Los cortes locales se suben al servidor central
- El campo `sync_status` marca si estÃ¡ sincronizado
- El campo `last_sync` guarda la Ãºltima fecha de sincronizaciÃ³n
- Los cortes NO sincronizados se marcan visualmente

---

## 11. Resumen de Campos para ImplementaciÃ³n

### Tabla: `cash_closes`

| Campo | Tipo | Origen | DescripciÃ³n |
|-------|------|--------|-------------|
| `id` | INT | Auto | ID Ãºnico |
| `folio` | VARCHAR(50) | Auto | Folio Ãºnico |
| `branch_id` | INT | Auto | Sucursal |
| `user_id` | INT | Auto | Cajero |
| `opening_amount` | DECIMAL | Input | Fondo de apertura |
| `opening_date` | DATETIME | Auto | Fecha apertura |
| `closing_date` | DATETIME | Auto | Fecha cierre |
| `status` | VARCHAR(20) | Auto | "open" o "closed" |
| `total_cash` | DECIMAL | Calc | Efectivo directo |
| `total_debit` | DECIMAL | Calc | Tarjeta dÃ©bito |
| `total_credit` | DECIMAL | Calc | Tarjeta crÃ©dito |
| `total_check` | DECIMAL | Calc | Cheques |
| `total_transfer` | DECIMAL | Calc | Transferencias |
| `credit_total` | DECIMAL | Calc | Total crÃ©ditos creados |
| `credit_cash` | DECIMAL | Calc | Efectivo de crÃ©ditos |
| `layaway_total` | DECIMAL | Calc | Total apartados creados |
| `layaway_cash` | DECIMAL | Calc | Efectivo de apartados |
| `total_expenses` | DECIMAL | Calc | Suma de gastos |
| `total_income` | DECIMAL | Calc | Suma de ingresos |
| `reported_cash` | DECIMAL | Input | Efectivo contado |
| `expected_cash` | DECIMAL | Calc | Efectivo esperado |
| `surplus` | DECIMAL | Calc | Diferencia |
| `notes` | TEXT | Input | Observaciones |
| `created_at` | DATETIME | Auto | AuditorÃ­a |
| `sync_status` | INT | Auto | Estado sync |
| `last_sync` | DATETIME | Auto | Ãšltima sync |

### Tabla: `cash_movements`

| Campo | Tipo | DescripciÃ³n |
|-------|------|-------------|
| `id` | INT | ID Ãºnico |
| `cash_close_id` | INT | FK a corte |
| `type` | VARCHAR(20) | "expense" o "income" |
| `concept` | VARCHAR(200) | DescripciÃ³n |
| `amount` | DECIMAL | Monto |
| `created_at` | DATETIME | Fecha/hora |
| `user_id` | INT | Quien lo registrÃ³ |

---

## 12. Checklist de ImplementaciÃ³n

### âœ… Fase 1: Modelos y Base de Datos
- [ ] Crear/actualizar modelo `CashClose` con todos los campos
- [ ] Crear modelo `CashMovement`
- [ ] Agregar FK `cash_close_id` a tabla `sales`
- [ ] Crear Ã­ndices necesarios
- [ ] Probar creaciÃ³n de tablas

### âœ… Fase 2: Servicios de Negocio
- [ ] Servicio de apertura de caja
- [ ] ValidaciÃ³n de caja Ãºnica abierta
- [ ] Servicio para calcular totales
- [ ] Servicio para registrar movimientos
- [ ] Servicio para calcular efectivo esperado
- [ ] Servicio para cierre de caja
- [ ] Probar todos los cÃ¡lculos con datos de prueba

### âœ… Fase 3: Interfaz de Usuario
- [ ] Vista de apertura de caja (modal)
- [ ] Vista de registro de gastos/ingresos (modal)
- [ ] Vista de corte de caja (pantalla completa)
- [ ] VisualizaciÃ³n de todos los campos
- [ ] Validaciones de campos
- [ ] Shortcuts de teclado (F5, Esc)
- [ ] Mensajes de error claros

### âœ… Fase 4: ImpresiÃ³n
- [ ] Formato de ticket de corte
- [ ] Incluir todos los detalles
- [ ] Desglose por mÃ©todo de pago
- [ ] Lista de gastos e ingresos
- [ ] Resaltar diferencias
- [ ] Probar impresiÃ³n

### âœ… Fase 5: IntegraciÃ³n
- [ ] Bloquear ventas sin caja abierta
- [ ] Vincular ventas al corte actual
- [ ] Recalcular totales en tiempo real
- [ ] Validar al cerrar caja
- [ ] Probar flujo completo

### âœ… Fase 6: Pruebas
- [ ] Caso: Turno simple
- [ ] Caso: Turno con crÃ©ditos
- [ ] Caso: Turno con apartados
- [ ] Caso: Turno con gastos/ingresos
- [ ] Caso: Diferencia en efectivo
- [ ] Caso: Sin ventas
- [ ] Validar todas las fÃ³rmulas

---

## ðŸ“š Glosario de TÃ©rminos

| TÃ©rmino | DefiniciÃ³n |
|---------|-----------|
| **Corte de Caja** | Proceso de cierre de turno y conteo de efectivo |
| **Fondo de Apertura** | Dinero inicial en caja al comenzar turno |
| **Efectivo Esperado** | Cantidad de efectivo que deberÃ­a haber segÃºn el sistema |
| **Efectivo Reportado** | Cantidad de efectivo contada manualmente |
| **Sobrante** | Diferencia positiva entre reportado y esperado |
| **Faltante** | Diferencia negativa entre reportado y esperado |
| **Total del Corte** | Suma de todas las ventas del turno |
| **Efectivo Total** | Todo el efectivo que pasÃ³ por la caja |
| **Venta Directa** | Venta pag
# ðŸŽ¯ PROMPT: ImplementaciÃ³n Fase 4 - Cortes de Caja
## Casa Ceja Remake

---

## CONTEXTO DEL PROYECTO

Estoy refactorizando un sistema POS completo desde .NET Framework/Windows Forms hacia una arquitectura moderna con:

- **Framework**: .NET 8.0 LTS
- **UI**: Avalonia 11.3.0
- **Base de Datos**: SQLite (sqlite-net-pcl 1.9.172)
- **MVVM**: CommunityToolkit.Mvvm 8.3.2
- **Target**: Windows 10+ x64
- **Desarrollo**: macOS

### Estado Actual
Las **Fases 0-3 estÃ¡n completadas al 100%**:
- âœ… Fase 0: Setup inicial
- âœ… Fase 1: Capa de datos (23 modelos, repositorios)
- âœ… Fase 2: Login y autenticaciÃ³n
- âœ… Fase 3: POS Ventas completo (incluye crÃ©ditos/apartados bÃ¡sicos)

### Repositorio
**GitHub**: https://github.com/VictorVega03/casa_ceja_remake

**IMPORTANTE**: Antes de comenzar, revisa el repositorio para obtener el cÃ³digo mÃ¡s reciente. El conocimiento del proyecto contiene el documento `ANALISIS_SISTEMA_CASA_CEJA_REMAKE_v3.md` con toda la arquitectura y convenciones.

---

## OBJETIVO: FASE 4 - CORTES DE CAJA

Implementar el sistema completo de apertura y cierre de caja con las siguientes funcionalidades:

1. **Apertura de Caja** - Registrar fondo inicial antes de vender
2. **Corte de Caja** - Cerrar turno con resumen de ventas
3. **Gastos e Ingresos** - Registrar movimientos de efectivo
4. **ImpresiÃ³n de Corte** - Generar ticket del corte

---

## COMPONENTES A CREAR

### 4.1 Apertura de Caja (DÃ­a 1)

```
Archivos a crear:
â”œâ”€â”€ Views/POS/OpenCashView.axaml
â”œâ”€â”€ Views/POS/OpenCashView.axaml.cs
â”œâ”€â”€ ViewModels/POS/OpenCashViewModel.cs
â””â”€â”€ Actualizar Services/CashCloseService.cs
```

**Funcionalidad OpenCashView:**
- Modal que aparece al entrar al POS si no hay caja abierta
- Campo numÃ©rico para "Fondo de Apertura" (F1)
- BotÃ³n ACEPTAR (F5) y CANCELAR (Esc)
- Validar que el monto sea >= 0
- Bloquear ventas si no hay caja abierta
- Guardar registro en tabla `cash_closes` con estado "abierto"

**Campos del registro de apertura:**
| Campo | Tipo | DescripciÃ³n |
|-------|------|-------------|
| opening_amount | decimal | Fondo inicial |
| opening_date | DateTime | Fecha/hora apertura |
| user_id | int | Usuario que abre |
| branch_id | int | Sucursal |
| status | string | "open" |

### 4.2 Corte de Caja (DÃ­as 2-3)

```
Archivos a crear:
â”œâ”€â”€ Views/POS/CashCloseView.axaml
â”œâ”€â”€ Views/POS/CashCloseView.axaml.cs
â””â”€â”€ Actualizar ViewModels/POS/CashCloseViewModel.cs (ya existe)
```

**Layout de CashCloseView (2 columnas):**

```
â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
â•‘                        CORTE DE CAJA                              â•‘
â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£
â•‘                                                                   â•‘
â•‘  COLUMNA IZQUIERDA              â”‚  COLUMNA DERECHA                â•‘
â•‘  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€              â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€               â•‘
â•‘  FOLIO: [AUTO]                  â”‚  FONDO DE APERTURA: $[CALC]     â•‘
â•‘                                 â”‚                                 â•‘
â•‘  TOTAL EN EFECTIVO: $[CALC]     â”‚  TOTAL TARJETA DÃ‰BITO: $[CALC]  â•‘
â•‘                                 â”‚                                 â•‘
â•‘  EFECTIVO APARTADOS: $[CALC]    â”‚  TOTAL TARJETA CRÃ‰DITO: $[CALC] â•‘
â•‘                                 â”‚                                 â•‘
â•‘  TOTAL APARTADOS: $[CALC]       â”‚  TOTAL CHEQUES: $[CALC]         â•‘
â•‘                                 â”‚                                 â•‘
â•‘  EFECTIVO CRÃ‰DITOS: $[CALC]     â”‚  TOTAL TRANSFERENCIAS: $[CALC]  â•‘
â•‘                                 â”‚                                 â•‘
â•‘  TOTAL CRÃ‰DITOS: $[CALC]        â”‚  SOBRANTE/FALTANTE: [INPUT]     â•‘
â•‘                                 â”‚                                 â•‘
â•‘  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â•‘
â•‘  â”‚ GASTOS                  â”‚    â”‚  â”‚ INGRESOS                â”‚    â•‘
â•‘  â”‚ [Lista de gastos]       â”‚    â”‚  â”‚ [Lista de ingresos]     â”‚    â•‘
â•‘  â”‚ + Agregar Gasto (F6)    â”‚    â”‚  â”‚ + Agregar Ingreso (F7)  â”‚    â•‘
â•‘  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â•‘
â•‘                                 â”‚                                 â•‘
â•‘  FECHA APERTURA: [DATE]         â”‚  FECHA CORTE: [DATE]            â•‘
â•‘                                 â”‚                                 â•‘
â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£
â•‘              [ACEPTAR (F5)]              [CANCELAR (Esc)]         â•‘
â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
```

**CÃ¡lculos automÃ¡ticos:**
```csharp
// Obtener ventas del dÃ­a desde apertura
var salesSinceOpen = await GetSalesSinceOpeningAsync(openingDate);

// Totales por mÃ©todo de pago
TotalEfectivo = salesSinceOpen.Where(s => s.PaymentMethod == "cash").Sum(s => s.Total);
TotalDebito = salesSinceOpen.Where(s => s.PaymentMethod == "debit").Sum(s => s.Total);
TotalCredito = salesSinceOpen.Where(s => s.PaymentMethod == "credit").Sum(s => s.Total);
TotalTransferencia = salesSinceOpen.Where(s => s.PaymentMethod == "transfer").Sum(s => s.Total);

// Abonos de apartados/crÃ©ditos (solo efectivo)
EfectivoApartados = await GetLayawayPaymentsCashAsync(openingDate);
EfectivoCreditos = await GetCreditPaymentsCashAsync(openingDate);

// Total esperado en caja
TotalEsperado = FondoApertura + TotalEfectivo + EfectivoApartados + EfectivoCreditos 
                + TotalIngresos - TotalGastos;

// Diferencia
Diferencia = MontoDeclarado - TotalEsperado;
```

**Shortcuts:**
| Tecla | AcciÃ³n |
|-------|--------|
| F5 | Aceptar corte |
| F6 | Agregar gasto |
| F7 | Agregar ingreso |
| Esc | Cancelar |

### 4.3 Gastos e Ingresos (DÃ­a 3)

```
Archivos a crear:
â”œâ”€â”€ Views/POS/CashMovementView.axaml
â”œâ”€â”€ Views/POS/CashMovementView.axaml.cs
â”œâ”€â”€ ViewModels/POS/CashMovementViewModel.cs
â””â”€â”€ Models/CashMovement.cs (si no existe)
```

**CashMovementView (Modal):**
```
â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—
â•‘     GASTO / INGRESO DE EFECTIVO       â•‘
â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£
â•‘                                       â•‘
â•‘  TIPO: â—‹ Gasto  â—‹ Ingreso             â•‘
â•‘                                       â•‘
â•‘  CONCEPTO (F1):                       â•‘
â•‘  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â•‘
â•‘  â”‚                                 â”‚  â•‘
â•‘  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â•‘
â•‘                                       â•‘
â•‘  MONTO (F2):                          â•‘
â•‘  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â•‘
â•‘  â”‚ $                               â”‚  â•‘
â•‘  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â•‘
â•‘                                       â•‘
â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£
â•‘    [ACEPTAR (F5)]   [CANCELAR (Esc)]  â•‘
â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
```

**Modelo CashMovement:**
```csharp
[Table("cash_movements")]
public class CashMovement
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("cash_close_id")]
    public int CashCloseId { get; set; }
    
    [Column("type")]
    public string Type { get; set; } // "expense" o "income"
    
    [Column("concept")]
    public string Concept { get; set; }
    
    [Column("amount")]
    public decimal Amount { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("user_id")]
    public int UserId { get; set; }
}
```

### 4.4 ImpresiÃ³n de Corte (DÃ­a 4)

```
Archivos a actualizar:
â”œâ”€â”€ Services/TicketService.cs (agregar GenerateCashCloseTicket)
â””â”€â”€ Services/PrintService.cs (si es necesario)
```

**Formato del Ticket de Corte:**
```
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
         CORTE DE CAJA
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
Sucursal: [NOMBRE SUCURSAL]
Caja: [NÃšMERO]
Folio: [FOLIO_CORTE]
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Fecha Apertura: [DD/MM/YYYY HH:MM]
Fecha Corte:    [DD/MM/YYYY HH:MM]
Cajero: [NOMBRE CAJERO]
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
       VENTAS POR MÃ‰TODO DE PAGO
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Efectivo:              $[MONTO]
Tarjeta DÃ©bito:        $[MONTO]
Tarjeta CrÃ©dito:       $[MONTO]
Transferencias:        $[MONTO]
Cheques:               $[MONTO]
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
       CRÃ‰DITOS Y APARTADOS
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Efectivo Apartados:    $[MONTO]
Total Apartados:       $[MONTO]
Efectivo CrÃ©ditos:     $[MONTO]
Total CrÃ©ditos:        $[MONTO]
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
           MOVIMIENTOS
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Fondo Apertura:        $[MONTO]
(+) Ingresos:          $[MONTO]
(-) Gastos:            $[MONTO]
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            TOTALES
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Total Esperado:        $[MONTO]
Total Declarado:       $[MONTO]
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Diferencia:            $[MONTO]
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
```

---

## CONVENCIONES DE CÃ“DIGO

### Nomenclatura
```csharp
// Clases: PascalCase
public class CashCloseService { }

// Campos privados: _camelCase
private readonly DatabaseService _databaseService;

// Propiedades pÃºblicas: PascalCase
public decimal TotalCash { get; set; }

// ObservableProperty: camelCase (el generador crea PascalCase)
[ObservableProperty]
private decimal _totalCash;

// Tablas SQLite: snake_case plural
[Table("cash_closes")]
[Column("opening_amount")]
```

### PatrÃ³n de Servicios (Result Pattern)
```csharp
public class CashCloseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public CashClose? CashClose { get; set; }
    
    public static CashCloseResult Ok(CashClose close) => 
        new() { Success = true, CashClose = close };
    public static CashCloseResult Error(string msg) => 
        new() { Success = false, ErrorMessage = msg };
}
```

### PatrÃ³n de ViewModels
```csharp
public partial class OpenCashViewModel : ViewModelBase
{
    private readonly CashCloseService _cashCloseService;
    private readonly AuthService _authService;
    
    [ObservableProperty]
    private decimal _openingAmount;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading;
    
    public OpenCashViewModel(CashCloseService cashCloseService, AuthService authService)
    {
        _cashCloseService = cashCloseService;
        _authService = authService;
    }
    
    [RelayCommand]
    private async Task OpenCashAsync()
    {
        if (OpeningAmount < 0)
        {
            ErrorMessage = "El monto debe ser mayor o igual a 0";
            return;
        }
        
        IsLoading = true;
        try
        {
            var result = await _cashCloseService.OpenCashAsync(
                OpeningAmount, 
                _authService.CurrentUser!.Id,
                _authService.CurrentBranchId);
            
            if (result.Success)
            {
                CashOpened?.Invoke(this, result.CashClose!);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al abrir caja";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
    
    public event EventHandler<CashClose>? CashOpened;
    public event EventHandler? Cancelled;
}
```

### PatrÃ³n de Views (Code-Behind)
```csharp
public partial class OpenCashView : Window
{
    public OpenCashView()
    {
        InitializeComponent();
        
        // Focus inicial
        Opened += (s, e) => AmountTextBox.Focus();
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is OpenCashViewModel vm)
        {
            vm.CashOpened += OnCashOpened;
            vm.Cancelled += OnCancelled;
        }
    }
    
    private void OnCashOpened(object? sender, CashClose cashClose)
    {
        Tag = cashClose;
        Close();
    }
    
    private void OnCancelled(object? sender, EventArgs e)
    {
        Tag = null;
        Close();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is OpenCashViewModel vm)
        {
            vm.CashOpened -= OnCashOpened;
            vm.Cancelled -= OnCancelled;
        }
        base.OnClosed(e);
    }
    
    // Keyboard shortcuts
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        if (DataContext is OpenCashViewModel vm)
        {
            switch (e.Key)
            {
                case Key.F5:
                    vm.OpenCashCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    vm.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
```

---

## MODELO CashClose EXISTENTE

Revisa el modelo actual en `Models/CashClose.cs`. Probablemente necesites agregar campos:

```csharp
[Table("cash_closes")]
public class CashClose
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("folio")]
    public string Folio { get; set; } = string.Empty;
    
    [Column("branch_id")]
    public int BranchId { get; set; }
    
    [Column("user_id")]
    public int UserId { get; set; }
    
    // === CAMPOS DE APERTURA ===
    [Column("opening_amount")]
    public decimal OpeningAmount { get; set; }
    
    [Column("opening_date")]
    public DateTime OpeningDate { get; set; }
    
    // === CAMPOS DE CIERRE ===
    [Column("closing_date")]
    public DateTime? ClosingDate { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "open"; // "open", "closed"
    
    // === TOTALES POR MÃ‰TODO DE PAGO ===
    [Column("total_cash")]
    public decimal TotalCash { get; set; }
    
    [Column("total_debit")]
    public decimal TotalDebit { get; set; }
    
    [Column("total_credit")]
    public decimal TotalCredit { get; set; }
    
    [Column("total_transfer")]
    public decimal TotalTransfer { get; set; }
    
    [Column("total_check")]
    public decimal TotalCheck { get; set; }
    
    // === APARTADOS Y CRÃ‰DITOS ===
    [Column("layaway_cash")]
    public decimal LayawayCash { get; set; }
    
    [Column("layaway_total")]
    public decimal LayawayTotal { get; set; }
    
    [Column("credit_cash")]
    public decimal CreditCash { get; set; }
    
    [Column("credit_total")]
    public decimal CreditTotal { get; set; }
    
    // === GASTOS E INGRESOS ===
    [Column("total_expenses")]
    public decimal TotalExpenses { get; set; }
    
    [Column("total_income")]
    public decimal TotalIncome { get; set; }
    
    // === DIFERENCIA ===
    [Column("expected_amount")]
    public decimal ExpectedAmount { get; set; }
    
    [Column("declared_amount")]
    public decimal DeclaredAmount { get; set; }
    
    [Column("difference")]
    public decimal Difference { get; set; }
    
    // === AUDITORÃA ===
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    [Column("sync_status")]
    public int SyncStatus { get; set; } = 1;
    
    [Column("last_sync")]
    public DateTime LastSync { get; set; }
}
```

---

## INTEGRACIÃ“N CON EL FLUJO DE POS

### Al entrar al mÃ³dulo POS:
```csharp
// En POSMainViewModel o SalesViewModel
public async Task CheckCashStatusAsync()
{
    var openCash = await _cashCloseService.GetOpenCashAsync(_branchId);
    
    if (openCash == null)
    {
        // Mostrar modal de apertura
        var openView = new OpenCashView();
        var openVm = new OpenCashViewModel(_cashCloseService, _authService);
        openView.DataContext = openVm;
        
        await openView.ShowDialog(MainWindow);
        
        if (openView.Tag is CashClose cashOpen)
        {
            CurrentCashClose = cashOpen;
            // Continuar al POS
        }
        else
        {
            // Usuario cancelÃ³, volver al selector de mÃ³dulos
            NavigateToModuleSelector();
        }
    }
    else
    {
        CurrentCashClose = openCash;
    }
}
```

### Al hacer corte:
```csharp
// Accesible desde menÃº o shortcut (F10 por ejemplo)
[RelayCommand]
private async Task DoCashCloseAsync()
{
    var closeView = new CashCloseView();
    var closeVm = new CashCloseViewModel(_cashCloseService, _authService);
    await closeVm.LoadDataAsync();
    closeView.DataContext = closeVm;
    
    await closeView.ShowDialog(MainWindow);
    
    if (closeView.Tag is CashClose completed)
    {
        // Imprimir ticket de corte
        await _ticketService.PrintCashCloseTicketAsync(completed);
        
        // Volver al login o selector
        NavigateToLogin();
    }
}
```

---

## ARCHIVOS DE REFERENCIA

Revisa estos archivos existentes para mantener consistencia:

1. **Views/POS/SalesView.axaml** - Estructura de vista POS
2. **Views/POS/PaymentView.axaml** - Modal de cobro (similar a corte)
3. **ViewModels/POS/SalesViewModel.cs** - PatrÃ³n de ViewModel
4. **ViewModels/POS/CashCloseViewModel.cs** - Ya existe, actualizar
5. **Services/SalesService.cs** - PatrÃ³n de servicios
6. **Services/TicketService.cs** - Para agregar formato de corte
7. **Models/Sale.cs** - Referencia de modelo con atributos

---

## CRITERIOS DE ACEPTACIÃ“N

### âœ… Apertura de Caja
- [ ] Modal aparece si no hay caja abierta
- [ ] Permite ingresar fondo >= 0
- [ ] Guarda registro con status "open"
- [ ] Bloquea ventas sin caja abierta
- [ ] Shortcuts F5 y Esc funcionan

### âœ… Corte de Caja
- [ ] Muestra todos los totales calculados automÃ¡ticamente
- [ ] Permite registrar gastos e ingresos
- [ ] Calcula diferencia correctamente
- [ ] Genera folio Ãºnico
- [ ] Guarda con status "closed"
- [ ] Shortcuts funcionan

### âœ… Gastos e Ingresos
- [ ] Modal para agregar gasto/ingreso
- [ ] Campos: concepto y monto
- [ ] Se reflejan en el corte
- [ ] Shortcuts funcionan

### âœ… ImpresiÃ³n
- [ ] Genera ticket con formato correcto
- [ ] Incluye todos los totales
- [ ] Muestra diferencia

### âœ… IntegraciÃ³n
- [ ] Flujo completo funciona
- [ ] DespuÃ©s del corte regresa al login
- [ ] Historial de cortes visible (opcional)

---

## NOTAS IMPORTANTES

1. **No modificar diseÃ±o de vistas existentes** - Solo crear nuevas
2. **Mantener convenciones de cÃ³digo** - Ver ejemplos arriba
3. **Async/await siempre** - Nunca bloquear UI
4. **Result Pattern** - Para todos los mÃ©todos de servicio
5. **Tickets inmutables** - Guardar datos, no recalcular
6. **Probar en macOS** - El desarrollo es en Mac

---

## ENTREGABLES ESPERADOS

Al finalizar la Fase 4, deben existir:

```
Views/POS/
â”œâ”€â”€ OpenCashView.axaml          âœ… NUEVO
â”œâ”€â”€ OpenCashView.axaml.cs       âœ… NUEVO
â”œâ”€â”€ CashCloseView.axaml         âœ… NUEVO
â”œâ”€â”€ CashCloseView.axaml.cs      âœ… NUEVO
â”œâ”€â”€ CashMovementView.axaml      âœ… NUEVO
â””â”€â”€ CashMovementView.axaml.cs   âœ… NUEVO

ViewModels/POS/
â”œâ”€â”€ OpenCashViewModel.cs        âœ… NUEVO
â”œâ”€â”€ CashCloseViewModel.cs       âœ… ACTUALIZADO
â””â”€â”€ CashMovementViewModel.cs    âœ… NUEVO

Models/
â””â”€â”€ CashMovement.cs             âœ… NUEVO (si no existe)

Services/
â”œâ”€â”€ CashCloseService.cs         âœ… ACTUALIZADO
â””â”€â”€ TicketService.cs            âœ… ACTUALIZADO
```

---

*Prompt generado: 28 de Enero, 2026*
*Proyecto: Casa Ceja Remake*
*Fase: 4 - Cortes de Caja*
*DuraciÃ³n estimada: 4 dÃ­as*
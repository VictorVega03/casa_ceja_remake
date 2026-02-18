# Plan Técnico: Módulos de Configuración, Impresión y Exportación

> **Proyecto:** Casa Ceja POS Remake (Avalonia UI / .NET 8)  
> **Fecha:** Febrero 2026  
> **Alcance:** Puntos 5 y 6 del proyecto + Módulo de Exportación Excel

---

## Índice

1. [Arquitectura Actual (Contexto)](#1-arquitectura-actual-contexto)
2. [Módulo de Configuración](#2-módulo-de-configuración)
3. [Reglas de Negocio y Seguridad](#3-reglas-de-negocio-y-seguridad)
4. [Servicio de Impresión (PrintService)](#4-servicio-de-impresión-printservice)
5. [Gestión de Documentos y Exportación](#5-gestión-de-documentos-y-exportación)
6. [Diagrama de Dependencias](#6-diagrama-de-dependencias)
7. [Archivos a Crear / Modificar](#7-archivos-a-crear--modificar)
8. [Plan de Implementación por Fases](#8-plan-de-implementación-por-fases)

---

## 1. Arquitectura Actual (Contexto)

### Stack Tecnológico

| Componente | Tecnología |
|---|---|
| Framework UI | Avalonia UI 11.3.0 |
| Target | .NET 8.0 (multiplataforma: Windows/macOS) |
| Patrón | MVVM (CommunityToolkit.Mvvm 8.3.2) |
| Base de Datos | SQLite (sqlite-net-pcl 1.9.172) |
| Excel | ClosedXML 0.102.3 (ya incluido en .csproj) |
| JSON | Newtonsoft.Json 13.0.3 + System.Text.Json |

### Estructura de Capas

```
CasaCejaRemake/
├── Models/          → Entidades SQLite (Branch, User, Sale, CashClose, etc.)
├── Data/
│   ├── DatabaseService.cs       → Conexión SQLite, inicialización de tablas
│   └── Repositories/
│       ├── IRepository.cs       → Interfaz genérica
│       └── BaseRepository.cs    → Implementación CRUD genérica
├── Services/
│   ├── AuthService.cs           → Autenticación, roles (Admin=1, Cajero=2)
│   ├── SalesService.cs          → Procesamiento de ventas
│   ├── TicketService.cs         → Generación de texto para tickets
│   ├── CashCloseService.cs      → Cortes de caja
│   ├── CartService.cs           → Carrito multicolección (A, B, C, D)
│   ├── PricingService.cs        → Cálculo de precios/descuentos
│   ├── ConfigService.cs         → (VACÍO - por implementar)
│   ├── PrintService.cs          → (VACÍO - por implementar)
│   ├── ExportService.cs         → (VACÍO - por implementar)
│   └── NotificationService.cs   → (VACÍO - por implementar)
├── Helpers/
│   ├── Constants.cs             → (VACÍO - por implementar)
│   ├── FormatHelper.cs          → (VACÍO - por implementar)
│   ├── DialogHelper.cs          → Diálogos de Avalonia
│   ├── JsonCompressor.cs        → Compresión de datos de ticket
│   └── Extensions.cs            → Métodos de extensión
├── ViewModels/
│   ├── ViewModelBase.cs         → Base: ObservableObject
│   ├── Shared/
│   │   ├── ConfigViewModel.cs   → (VACÍO - por implementar)
│   │   ├── LoginViewModel.cs
│   │   └── ModuleSelectorViewModel.cs
│   ├── POS/                     → 26 ViewModels (ventas, cortes, créditos, etc.)
│   ├── Admin/                   → 8 ViewModels (productos, sucursales, reportes, etc.)
│   └── Inventory/               → 5 ViewModels (catálogo, entradas, salidas)
└── Views/
    ├── Shared/                  → Login, ModuleSelector
    ├── POS/                     → 21 vistas (.axaml + .axaml.cs)
    ├── Admin/                   → (pendientes de crear)
    └── Inventory/               → (pendientes de crear)
```

### Servicios Clave Existentes

**`AuthService`** — Maneja autenticación y autorización:
- `IsAdmin` → `CurrentUser.UserType == 1`
- `IsCajero` → `CurrentUser.UserType == 2`
- `SetCurrentBranch(int branchId)` → Solo Admin puede cambiar sucursal
- `HasAccessLevel(int requiredLevel)` → Admin (1) tiene acceso a todo

**`TicketService`** — Genera texto formateado para tickets:
- `GenerateTicketText(TicketData, TicketType, lineWidth)` → Formato texto plano
- `GenerateCashCloseTicketText(...)` → Texto para corte de caja
- Métodos `CenterText()` y `FormatAmountLine()` para formateo
- Soporta tipos: `Sale`, `Credit`, `Layaway`

**`DatabaseService`** — Almacena BD en:
- Ruta: `{ApplicationData}/CasaCeja/casaceja.db`
- Detecta SO automáticamente vía `Environment.SpecialFolder.ApplicationData`

---

## 2. Módulo de Configuración

### 2.1 Modelo de Configuración (`PosConfig`)

La configuración se persistirá como un archivo JSON local, **no en SQLite**, ya que es específica de cada máquina/punto de venta.

```
Ruta del archivo:
  Windows: %APPDATA%/CasaCeja/pos_config.json
  macOS:   ~/Library/Application Support/CasaCeja/pos_config.json
```

> Nota: Esta ruta coincide con la que ya usa `DatabaseService` para la BD (`{ApplicationData}/CasaCeja/`).

#### Clase: `Models/PosConfig.cs`

```csharp
namespace CasaCejaRemake.Models
{
    /// <summary>
    /// Configuración local del punto de venta.
    /// Se persiste como JSON en disco, NO en la base de datos.
    /// Es específica por máquina/terminal.
    /// </summary>
    public class PosConfig
    {
        // ============ SUCURSAL ============
        /// <summary>ID de la sucursal seleccionada (solo Admin puede cambiar)</summary>
        public int BranchId { get; set; } = 1;

        // ============ CAJA ============
        /// <summary>Identificador de la caja/terminal (solo Admin puede cambiar)</summary>
        public string CashRegisterId { get; set; } = "CAJA-01";

        // ============ IMPRESORA ============
        /// <summary>Nombre del sistema de la impresora seleccionada</summary>
        public string PrinterName { get; set; } = string.Empty;

        /// <summary>Formato de impresión: "thermal" = ticket térmico, "letter" = hoja carta</summary>
        public string PrintFormat { get; set; } = "thermal";

        // ============ PARÁMETROS DEL TICKET ============
        /// <summary>Pie de página personalizado del ticket</summary>
        public string TicketFooter { get; set; } = "Gracias por su compra";

        /// <summary>Tamaño de letra para impresión (8, 9, 10, 11, 12)</summary>
        public int FontSize { get; set; } = 9;

        /// <summary>Familia de fuente: "Courier New", "Consolas", "Lucida Console"</summary>
        public string FontFamily { get; set; } = "Courier New";

        /// <summary>Ancho de línea en caracteres para ticket térmico (32, 40, 48)</summary>
        public int TicketLineWidth { get; set; } = 40;

        // ============ METADATA ============
        /// <summary>Fecha de última modificación</summary>
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}
```

### 2.2 Servicio de Configuración: `ConfigService.cs`

Responsabilidades:
- Cargar/guardar `PosConfig` desde JSON en disco.
- Proveer acceso global a la configuración actual.
- Detección de ruta multiplataforma (reutilizar patrón de `DatabaseService`).

#### Clase: `Services/ConfigService.cs`

```csharp
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio para gestión de la configuración local del POS.
    /// Persiste un archivo pos_config.json en {ApplicationData}/CasaCeja/.
    /// </summary>
    public class ConfigService
    {
        private readonly string _configFilePath;
        private PosConfig _currentConfig = new();

        /// <summary>Configuración actual en memoria.</summary>
        public PosConfig Current => _currentConfig;

        /// <summary>Se dispara cuando la configuración cambia.</summary>
        public event EventHandler? ConfigChanged;

        public ConfigService()
        {
            // Misma carpeta que DatabaseService usa para la BD
            var appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            var casaCejaFolder = Path.Combine(appDataPath, "CasaCeja");
            _configFilePath = Path.Combine(casaCejaFolder, "pos_config.json");
        }

        /// <summary>
        /// Carga la configuración desde disco. Si no existe, crea una por defecto.
        /// Llamar una vez al iniciar la aplicación.
        /// </summary>
        public async Task LoadAsync()
        {
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                _currentConfig = JsonSerializer.Deserialize<PosConfig>(json) ?? new PosConfig();
            }
            else
            {
                _currentConfig = new PosConfig();
                await SaveAsync(); // Crear archivo con valores por defecto
            }
        }

        /// <summary>
        /// Guarda la configuración actual en disco.
        /// </summary>
        public async Task SaveAsync()
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _currentConfig.LastModified = DateTime.Now;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_currentConfig, options);
            await File.WriteAllTextAsync(_configFilePath, json);

            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Actualiza un campo y guarda automáticamente.
        /// </summary>
        public async Task UpdateAsync(Action<PosConfig> updateAction)
        {
            updateAction(_currentConfig);
            await SaveAsync();
        }
    }
}
```

### 2.3 ViewModel de Configuración: `ConfigViewModel.cs`

Ubicación: `ViewModels/Shared/ConfigViewModel.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.Shared
{
    public partial class ConfigViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly AuthService _authService;
        private readonly PrintService _printService;

        // ============ SUCURSAL ============
        [ObservableProperty] private ObservableCollection<Branch> _branches = new();
        [ObservableProperty] private Branch? _selectedBranch;

        // ============ IMPRESORA ============
        [ObservableProperty] private ObservableCollection<string> _availablePrinters = new();
        [ObservableProperty] private string? _selectedPrinter;

        // ============ CAJA ============
        [ObservableProperty] private string _cashRegisterId = "CAJA-01";

        // ============ TICKET ============
        [ObservableProperty] private string _ticketFooter = "Gracias por su compra";
        [ObservableProperty] private int _selectedFontSize = 9;
        [ObservableProperty] private string _selectedFontFamily = "Courier New";
        [ObservableProperty] private string _selectedPrintFormat = "thermal";

        // ============ PERMISOS ============
        /// <summary>Solo Admin puede editar Sucursal e ID de Caja</summary>
        public bool CanEditAdminFields => _authService.IsAdmin;
        public bool IsReadOnlyForCajero => !_authService.IsAdmin;

        // ============ OPCIONES ESTÁTICAS ============
        public List<int> FontSizeOptions { get; } = new() { 8, 9, 10, 11, 12 };
        public List<string> FontFamilyOptions { get; } = new()
        {
            "Courier New", "Consolas", "Lucida Console", "Menlo", "Monaco"
        };
        public List<string> PrintFormatOptions { get; } = new()
        {
            "thermal",  // Ticket Térmico
            "letter"    // Hoja Carta
        };

        // ============ ESTADO ============
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public event EventHandler? CloseRequested;

        public ConfigViewModel(
            ConfigService configService,
            AuthService authService,
            PrintService printService)
        {
            _configService = configService;
            _authService = authService;
            _printService = printService;
        }

        /// <summary>
        /// Inicializa la vista: carga config, sucursales e impresoras.
        /// </summary>
        public async Task InitializeAsync(List<Branch> branches)
        {
            IsLoading = true;
            try
            {
                // 1. Cargar sucursales
                Branches = new ObservableCollection<Branch>(branches);

                // 2. Cargar impresoras del sistema
                var printers = _printService.GetAvailablePrinters();
                AvailablePrinters = new ObservableCollection<string>(printers);

                // 3. Aplicar configuración guardada a los controles
                var config = _configService.Current;
                SelectedBranch = /* buscar por config.BranchId */;
                SelectedPrinter = config.PrinterName;
                CashRegisterId = config.CashRegisterId;
                TicketFooter = config.TicketFooter;
                SelectedFontSize = config.FontSize;
                SelectedFontFamily = config.FontFamily;
                SelectedPrintFormat = config.PrintFormat;

                StatusMessage = "Configuración cargada";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            await _configService.UpdateAsync(config =>
            {
                if (_authService.IsAdmin)
                {
                    config.BranchId = SelectedBranch?.Id ?? config.BranchId;
                    config.CashRegisterId = CashRegisterId;
                }
                config.PrinterName = SelectedPrinter ?? string.Empty;
                config.PrintFormat = SelectedPrintFormat;
                config.TicketFooter = TicketFooter;
                config.FontSize = SelectedFontSize;
                config.FontFamily = SelectedFontFamily;
            });

            StatusMessage = "✓ Configuración guardada";
        }

        [RelayCommand]
        private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
```

### 2.4 Vista de Configuración: `ConfigView.axaml`

Ubicación: `Views/Shared/ConfigView.axaml` + `ConfigView.axaml.cs`

Elementos de UI requeridos (Avalonia Controls):

| Sección | Control | Binding | Restricción |
|---|---|---|---|
| Sucursal | `ComboBox` | `SelectedBranch`, `ItemsSource=Branches` | `IsEnabled=CanEditAdminFields` |
| ID de Caja | `TextBox` | `CashRegisterId` | `IsReadOnly=IsReadOnlyForCajero` |
| Impresora | `ComboBox` | `SelectedPrinter`, `ItemsSource=AvailablePrinters` | Todos |
| Formato | `ComboBox` | `SelectedPrintFormat`, `ItemsSource=PrintFormatOptions` | Todos |
| Pie de Ticket | `TextBox` | `TicketFooter` | Todos |
| Tamaño Fuente | `ComboBox` | `SelectedFontSize`, `ItemsSource=FontSizeOptions` | Todos |
| Tipo Fuente | `ComboBox` | `SelectedFontFamily`, `ItemsSource=FontFamilyOptions` | Todos |
| Guardar | `Button` | `Command=SaveCommand` | Todos |

---

## 3. Reglas de Negocio y Seguridad

### 3.1 Validación de Roles

La lógica de roles **ya existe** en `AuthService`:

```
User.UserType == 1  →  Admin   →  Acceso total
User.UserType == 2  →  Cajero  →  Acceso restringido
```

**Métodos existentes utilizables:**
- `AuthService.IsAdmin` → `bool` — Determina si el usuario actual es administrador.
- `AuthService.SetCurrentBranch(int branchId)` → Solo permite cambio si `IsAdmin`.
- `AuthService.HasAccessLevel(int requiredLevel)` → Admin (nivel 1) tiene acceso a todo.

### 3.2 Restricciones en `ConfigViewModel`

| Campo | Admin | Cajero |
|---|---|---|
| Sucursal (`ComboBox`) | ✅ Editable | 🔒 Solo lectura (muestra su sucursal asignada) |
| ID de Caja (`TextBox`) | ✅ Editable | 🔒 Solo lectura |
| Impresora | ✅ Editable | ✅ Editable |
| Formato de impresión | ✅ Editable | ✅ Editable |
| Pie de ticket | ✅ Editable | ✅ Editable |
| Fuente y tamaño | ✅ Editable | ✅ Editable |

### 3.3 Implementación en AXAML

```xml
<!-- Sucursal - solo Admin puede cambiar -->
<ComboBox ItemsSource="{Binding Branches}"
          SelectedItem="{Binding SelectedBranch}"
          IsEnabled="{Binding CanEditAdminFields}" />

<!-- ID Caja - solo Admin puede editar -->
<TextBox Text="{Binding CashRegisterId}"
         IsReadOnly="{Binding IsReadOnlyForCajero}" />
```

Las propiedades `CanEditAdminFields` e `IsReadOnlyForCajero` del `ConfigViewModel` se derivan directamente de `AuthService.IsAdmin`.

---

## 4. Servicio de Impresión (`PrintService`)

### 4.1 Responsabilidades

- Detectar impresoras instaladas en el SO (Windows/macOS).
- Enviar texto formateado a la impresora seleccionada.
- Soportar **dos formatos**: ticket térmico y hoja carta.
- Recibir los datos de venta y aplicar el formato según `PosConfig`.

### 4.2 Relación con `TicketService` existente

`TicketService` **ya genera el texto** del ticket (`GenerateTicketText`, `GenerateCashCloseTicketText`). `PrintService` se encarga exclusivamente de **enviar ese texto a la impresora física**.

```
Flujo de impresión:
  TicketService.GenerateTicketText(ticketData)  →  string ticketText
       ↓
  PrintService.PrintAsync(ticketText, printFormat)  →  Envía a impresora
```

### 4.3 Detección de Impresoras Multiplataforma

| SO | Método de detección |
|---|---|
| **Windows** | `System.Drawing.Printing.PrinterSettings.InstalledPrinters` o comando `wmic printer get name` |
| **macOS** | Comando `lpstat -p` (CUPS) vía `Process.Start` |

> **Nota:** Avalonia no tiene API nativa de impresión. Se usará `System.Diagnostics.Process` para interactuar con los sistemas de impresión del SO.

### 4.4 Clase: `Services/PrintService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio de impresión multiplataforma.
    /// Soporta impresoras térmicas (ticket) y convencionales (carta).
    /// </summary>
    public class PrintService
    {
        private readonly ConfigService _configService;

        public PrintService(ConfigService configService)
        {
            _configService = configService;
        }

        // ============================================================
        // DETECCIÓN DE IMPRESORAS
        // ============================================================

        /// <summary>
        /// Obtiene la lista de impresoras instaladas en el sistema.
        /// Detecta automáticamente si es Windows o macOS.
        /// </summary>
        public List<string> GetAvailablePrinters()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsPrinters();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacPrinters();
            else
                return new List<string> { "(Sin impresoras detectadas)" };
        }

        /// <summary>Windows: usa wmic para listar impresoras.</summary>
        private List<string> GetWindowsPrinters()
        {
            var printers = new List<string>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = "printer get name",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && trimmed != "Name")
                        printers.Add(trimmed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error detectando impresoras Windows: {ex.Message}");
            }
            return printers;
        }

        /// <summary>macOS: usa lpstat (CUPS) para listar impresoras.</summary>
        private List<string> GetMacPrinters()
        {
            var printers = new List<string>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lpstat",
                        Arguments = "-p",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (var line in output.Split('\n'))
                {
                    // Formato: "printer NOMBRE_IMPRESORA is idle..."
                    if (line.StartsWith("printer "))
                    {
                        var parts = line.Split(' ');
                        if (parts.Length >= 2)
                            printers.Add(parts[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error detectando impresoras macOS: {ex.Message}");
            }
            return printers;
        }

        // ============================================================
        // IMPRESIÓN
        // ============================================================

        /// <summary>
        /// Imprime texto usando la configuración actual (impresora y formato).
        /// Punto de entrada principal para todos los módulos.
        /// </summary>
        public async Task<bool> PrintAsync(string content)
        {
            var config = _configService.Current;
            return config.PrintFormat == "thermal"
                ? await PrintThermalAsync(content, config.PrinterName)
                : await PrintLetterAsync(content, config.PrinterName, config);
        }

        /// <summary>
        /// Impresión térmica: envía texto plano directamente a la impresora.
        /// Ideal para impresoras de tickets de 58mm y 80mm.
        /// </summary>
        public async Task<bool> PrintThermalAsync(string text, string printerName)
        {
            // ... Implementación con Process:
            //   Windows: escribir a archivo temporal + "print /d:\\nombre_impresora"
            //   macOS:   "lp -d nombre_impresora archivo_temporal"
        }

        /// <summary>
        /// Impresión en hoja carta: genera formato con márgenes y tipografía.
        /// Para impresoras láser/inyección convencionales.
        /// </summary>
        public async Task<bool> PrintLetterAsync(
            string text, string printerName, PosConfig config)
        {
            // ... Implementación con Process:
            //   Generar archivo de texto con formato de página
            //   Enviar a impresora del sistema
        }

        /// <summary>
        /// Imprime un ticket de venta usando TicketService + configuración.
        /// </summary>
        public async Task<bool> PrintSaleTicketAsync(string ticketText)
        {
            return await PrintAsync(ticketText);
        }

        /// <summary>
        /// Imprime un ticket de corte de caja.
        /// </summary>
        public async Task<bool> PrintCashCloseTicketAsync(string cashCloseText)
        {
            return await PrintAsync(cashCloseText);
        }
    }
}
```

### 4.5 Integración con Flujos Existentes

#### En `SalesService` (después de procesar venta):

```csharp
// Flujo actual (ya existe):
string ticketText = _ticketService.GenerateTicketText(ticketData);
return SaleResult.Ok(sale, ticketData, ticketText);

// El ViewModel (POSMainViewModel/SalesViewModel) recibe ticketText
// y llama a PrintService:
await _printService.PrintSaleTicketAsync(result.TicketText);
```

#### En `CashCloseViewModel` (después de cerrar caja):

```csharp
// Flujo: generar texto → imprimir
string closeText = _ticketService.GenerateCashCloseTicketText(...);
await _printService.PrintCashCloseTicketAsync(closeText);
```

### 4.6 Parámetros del `TicketService` afectados por `PosConfig`

El `TicketService` ya acepta un parámetro `lineWidth` (default 40). Este valor se tomará de `PosConfig.TicketLineWidth`:

```csharp
// Antes (hardcodeado):
_ticketService.GenerateTicketText(ticketData, TicketType.Sale, 40);

// Después (configurable):
var lineWidth = _configService.Current.TicketLineWidth;
_ticketService.GenerateTicketText(ticketData, TicketType.Sale, lineWidth);
```

---

## 5. Gestión de Documentos y Exportación

### 5.1 Helper de Directorios: `FileHelper.cs`

Ubicación: `Helpers/FileHelper.cs`

#### Estructura de Carpetas a Crear

```
{Documentos del Usuario}/
└── CasaCejaDocs/
    ├── POS/              → Reportes de ventas, cortes de caja
    ├── Inventario/       → Reportes de entradas, salidas, catálogo
    └── Administrador/    → Reportes administrativos generales
```

> **Nota:** Por ahora los tickets NO se guardan aquí, solo reportes Excel.

#### Clase: `Helpers/FileHelper.cs`

```csharp
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Tipo de módulo para determinar la subcarpeta de destino.
    /// </summary>
    public enum DocumentModule
    {
        POS,
        Inventario,
        Administrador
    }

    /// <summary>
    /// Helper multiplataforma para gestión de directorios de documentos.
    /// Crea y gestiona la estructura CasaCejaDocs/{POS,Inventario,Administrador}.
    /// </summary>
    public static class FileHelper
    {
        private const string ROOT_FOLDER = "CasaCejaDocs";

        private static readonly string[] SUB_FOLDERS = { "POS", "Inventario", "Administrador" };

        /// <summary>
        /// Obtiene la ruta raíz de documentos según el SO.
        ///   Windows: %USERPROFILE%\Documents\CasaCejaDocs
        ///   macOS:   ~/Documents/CasaCejaDocs
        /// </summary>
        public static string GetRootPath()
        {
            string documentsPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                documentsPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // En macOS, SpecialFolder.MyDocuments puede devolver ~/Documents
                documentsPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);
                
                // Fallback si devuelve ruta vacía
                if (string.IsNullOrEmpty(documentsPath))
                    documentsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Documents");
            }
            else
            {
                documentsPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);
            }

            return Path.Combine(documentsPath, ROOT_FOLDER);
        }

        /// <summary>
        /// Obtiene la ruta completa de un módulo específico.
        /// Ejemplo: ~/Documents/CasaCejaDocs/POS
        /// </summary>
        public static string GetModulePath(DocumentModule module)
        {
            string subFolder = module switch
            {
                DocumentModule.POS => "POS",
                DocumentModule.Inventario => "Inventario",
                DocumentModule.Administrador => "Administrador",
                _ => "POS"
            };

            return Path.Combine(GetRootPath(), subFolder);
        }

        /// <summary>
        /// Inicializa toda la estructura de carpetas.
        /// Verifica si existen antes de crearlas.
        /// Llamar una vez al iniciar la aplicación.
        /// </summary>
        /// <returns>true si todas las carpetas existen/fueron creadas correctamente</returns>
        public static bool EnsureDirectoriesExist()
        {
            try
            {
                var rootPath = GetRootPath();

                // Crear raíz si no existe
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                // Crear subcarpetas
                foreach (var subFolder in SUB_FOLDERS)
                {
                    var subPath = Path.Combine(rootPath, subFolder);
                    if (!Directory.Exists(subPath))
                        Directory.CreateDirectory(subPath);
                }

                Console.WriteLine($"[FileHelper] Directorios verificados en: {rootPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileHelper] Error creando directorios: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Genera un nombre de archivo con timestamp para evitar colisiones.
        /// Ejemplo: "Reporte_Ventas_20260210_143025.xlsx"
        /// </summary>
        public static string GenerateFileName(string baseName, string extension = ".xlsx")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // Sanitizar nombre base
            var safeName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
            return $"{safeName}_{timestamp}{extension}";
        }

        /// <summary>
        /// Obtiene la ruta completa para un archivo nuevo en el módulo indicado.
        /// </summary>
        public static string GetFilePath(DocumentModule module, string baseName, string extension = ".xlsx")
        {
            EnsureDirectoriesExist();
            var fileName = GenerateFileName(baseName, extension);
            return Path.Combine(GetModulePath(module), fileName);
        }
    }
}
```

### 5.2 Servicio de Exportación a Excel: `ExportService.cs`

Ubicación: `Services/ExportService.cs`

Utiliza **ClosedXML 0.102.3** (ya está referenciado en `casa_ceja_remake.csproj`).

#### Clase: `Services/ExportService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CasaCejaRemake.Helpers;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Resultado de una operación de exportación.
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }

        public static ExportResult Ok(string filePath) =>
            new() { Success = true, FilePath = filePath };

        public static ExportResult Error(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Definición de una columna para exportación.
    /// Permite mapear propiedades de cualquier objeto a columnas de Excel.
    /// </summary>
    public class ExportColumn<T>
    {
        public string Header { get; set; } = string.Empty;
        public Func<T, object?> ValueSelector { get; set; } = _ => null;
        public string Format { get; set; } = string.Empty; // Ej: "C2" para moneda
        public double Width { get; set; } = 15;
    }

    /// <summary>
    /// Servicio de exportación a Excel (.xlsx) usando ClosedXML.
    /// Toma datos de cualquier colección y genera archivos formateados.
    /// Los archivos se guardan en CasaCejaDocs/{módulo}/.
    /// </summary>
    public class ExportService
    {
        /// <summary>
        /// Exporta una colección de datos a Excel con columnas personalizadas.
        /// Método genérico que funciona con cualquier tipo de dato.
        /// </summary>
        /// <typeparam name="T">Tipo de los datos a exportar</typeparam>
        /// <param name="data">Colección de datos</param>
        /// <param name="columns">Definición de columnas</param>
        /// <param name="sheetName">Nombre de la hoja</param>
        /// <param name="reportTitle">Título del reporte (fila superior)</param>
        /// <param name="module">Módulo destino (POS, Inventario, Administrador)</param>
        /// <param name="fileBaseName">Nombre base del archivo</param>
        public async Task<ExportResult> ExportToExcelAsync<T>(
            IEnumerable<T> data,
            List<ExportColumn<T>> columns,
            string sheetName,
            string reportTitle,
            DocumentModule module,
            string fileBaseName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Asegurar que las carpetas existan
                    FileHelper.EnsureDirectoriesExist();

                    var filePath = FileHelper.GetFilePath(module, fileBaseName);

                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    int row = 1;

                    // ===== TÍTULO =====
                    worksheet.Cell(row, 1).Value = reportTitle;
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Cell(row, 1).Style.Font.FontSize = 14;
                    worksheet.Range(row, 1, row, columns.Count).Merge();
                    row++;

                    // ===== FECHA DE GENERACIÓN =====
                    worksheet.Cell(row, 1).Value = 
                        $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                    worksheet.Cell(row, 1).Style.Font.Italic = true;
                    worksheet.Range(row, 1, row, columns.Count).Merge();
                    row += 2; // Línea en blanco

                    // ===== ENCABEZADOS =====
                    for (int col = 0; col < columns.Count; col++)
                    {
                        var cell = worksheet.Cell(row, col + 1);
                        cell.Value = columns[col].Header;
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2196F3");
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    row++;

                    // ===== DATOS =====
                    var dataList = data.ToList();
                    foreach (var item in dataList)
                    {
                        for (int col = 0; col < columns.Count; col++)
                        {
                            var cell = worksheet.Cell(row, col + 1);
                            var value = columns[col].ValueSelector(item);

                            if (value is decimal decVal)
                                cell.Value = decVal;
                            else if (value is int intVal)
                                cell.Value = intVal;
                            else if (value is DateTime dtVal)
                                cell.Value = dtVal;
                            else
                                cell.Value = value?.ToString() ?? string.Empty;

                            // Aplicar formato
                            if (!string.IsNullOrEmpty(columns[col].Format))
                                cell.Style.NumberFormat.Format = columns[col].Format;
                        }
                        row++;
                    }

                    // ===== AJUSTAR ANCHOS =====
                    for (int col = 0; col < columns.Count; col++)
                    {
                        worksheet.Column(col + 1).Width = columns[col].Width;
                    }

                    // ===== BORDES =====
                    var dataRange = worksheet.Range(3, 1, row - 1, columns.Count);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // Guardar
                    workbook.SaveAs(filePath);

                    Console.WriteLine($"[ExportService] Archivo exportado: {filePath}");
                    return ExportResult.Ok(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExportService] Error exportando: {ex.Message}");
                    return ExportResult.Error($"Error al exportar: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Exporta datos simples (lista de diccionarios string→string).
        /// Útil para exportaciones rápidas desde DataGrids.
        /// </summary>
        public async Task<ExportResult> ExportSimpleAsync(
            List<Dictionary<string, object>> rows,
            string[] headers,
            string sheetName,
            string reportTitle,
            DocumentModule module,
            string fileBaseName)
        {
            var columns = headers.Select(h => new ExportColumn<Dictionary<string, object>>
            {
                Header = h,
                ValueSelector = dict => dict.TryGetValue(h, out var val) ? val : "",
                Width = 18
            }).ToList();

            return await ExportToExcelAsync(rows, columns, sheetName, reportTitle, module, fileBaseName);
        }
    }
}
```

### 5.3 Ejemplos de Uso por Módulo

#### Exportar Historial de Ventas (POS)

```csharp
// En SalesHistoryViewModel o ReportsViewModel:
var columns = new List<ExportColumn<Sale>>
{
    new() { Header = "Folio",   ValueSelector = s => s.Folio,   Width = 20 },
    new() { Header = "Fecha",   ValueSelector = s => s.SaleDate, Width = 18, Format = "dd/MM/yyyy HH:mm" },
    new() { Header = "Total",   ValueSelector = s => s.Total,   Width = 15, Format = "$#,##0.00" },
    new() { Header = "Método",  ValueSelector = s => s.PaymentSummary, Width = 25 },
};

var result = await _exportService.ExportToExcelAsync(
    ventas, columns, "Ventas", "Reporte de Ventas",
    DocumentModule.POS, "Reporte_Ventas");
```

#### Exportar Catálogo de Productos (Inventario)

```csharp
var columns = new List<ExportColumn<Product>>
{
    new() { Header = "Código",     ValueSelector = p => p.Barcode,     Width = 18 },
    new() { Header = "Nombre",     ValueSelector = p => p.Name,        Width = 35 },
    new() { Header = "Precio",     ValueSelector = p => p.RetailPrice, Width = 15, Format = "$#,##0.00" },
    new() { Header = "Categoría",  ValueSelector = p => p.CategoryName, Width = 20 },
};

var result = await _exportService.ExportToExcelAsync(
    productos, columns, "Productos", "Catálogo de Productos",
    DocumentModule.Inventario, "Catalogo_Productos");
```

#### Exportar Cortes de Caja (Administrador)

```csharp
var columns = new List<ExportColumn<CashClose>>
{
    new() { Header = "Folio",         ValueSelector = c => c.Folio,       Width = 18 },
    new() { Header = "Apertura",      ValueSelector = c => c.OpeningDate, Width = 18, Format = "dd/MM/yyyy HH:mm" },
    new() { Header = "Cierre",        ValueSelector = c => c.CloseDate,   Width = 18, Format = "dd/MM/yyyy HH:mm" },
    new() { Header = "Fondo",         ValueSelector = c => c.OpeningCash, Width = 15, Format = "$#,##0.00" },
    new() { Header = "Total Ventas",  ValueSelector = c => c.TotalSales,  Width = 15, Format = "$#,##0.00" },
    new() { Header = "Esperado",      ValueSelector = c => c.ExpectedCash, Width = 15, Format = "$#,##0.00" },
    new() { Header = "Diferencia",    ValueSelector = c => c.Surplus,      Width = 15, Format = "$#,##0.00" },
};

var result = await _exportService.ExportToExcelAsync(
    cortes, columns, "Cortes", "Reporte de Cortes de Caja",
    DocumentModule.Administrador, "Reporte_Cortes");
```

---

## 6. Diagrama de Dependencias

```
┌─────────────────────────────────────────────────────────────────┐
│                         VISTAS (AXAML)                          │
│  ┌──────────────┐  ┌───────────────┐  ┌──────────────────────┐  │
│  │  ConfigView   │  │  SalesHistory │  │  Reports (Admin)     │  │
│  │  (Shared)     │  │  (POS)        │  │                      │  │
│  └──────┬───────┘  └───────┬───────┘  └──────────┬───────────┘  │
│         │                  │                     │               │
├─────────┼──────────────────┼─────────────────────┼───────────────┤
│         ▼                  ▼                     ▼               │
│      VIEWMODELS                                                  │
│  ┌──────────────┐  ┌───────────────┐  ┌──────────────────────┐  │
│  │ConfigViewModel│  │SalesHistoryVM │  │  ReportsViewModel    │  │
│  └──┬──┬──┬─────┘  └───────┬───────┘  └──────────┬───────────┘  │
│     │  │  │                │                     │               │
├─────┼──┼──┼────────────────┼─────────────────────┼───────────────┤
│     │  │  │                │                     │               │
│  SERVICIOS                 │                     │               │
│     │  │  │                │                     │               │
│     │  │  └──► PrintService ◄──────────┐         │               │
│     │  │       ├── GetAvailablePrinters()        │               │
│     │  │       ├── PrintThermalAsync()           │               │
│     │  │       └── PrintLetterAsync()            │               │
│     │  │              │                          │               │
│     │  │              ▼                          │               │
│     │  │       TicketService (existente)         │               │
│     │  │       ├── GenerateTicketText()          │               │
│     │  │       └── GenerateCashCloseTicketText() │               │
│     │  │                                         │               │
│     │  └──► ConfigService                        │               │
│     │       ├── LoadAsync() / SaveAsync()        │               │
│     │       └── Current: PosConfig               │               │
│     │              │                             │               │
│     │              ▼                             │               │
│     │       PosConfig (JSON en disco)            │               │
│     │                                            │               │
│     └──► AuthService (existente)    ExportService ◄──────────────┘
│          ├── IsAdmin                ├── ExportToExcelAsync<T>()
│          └── SetCurrentBranch()     └── ExportSimpleAsync()
│                                            │
├────────────────────────────────────────────┼─────────────────────┤
│  HELPERS                                   │                     │
│  ┌──────────────────────────────────────┐  │                     │
│  │ FileHelper (static)                  │◄─┘                     │
│  │ ├── GetRootPath()        [OS detect] │                        │
│  │ ├── GetModulePath()                  │                        │
│  │ ├── EnsureDirectoriesExist()         │                        │
│  │ └── GenerateFileName()               │                        │
│  └──────────────────────────────────────┘                        │
│                                                                  │
│  Carpetas en disco:                                              │
│  ~/Documents/CasaCejaDocs/                                       │
│      ├── POS/                                                    │
│      ├── Inventario/                                             │
│      └── Administrador/                                          │
└──────────────────────────────────────────────────────────────────┘
```

---

## 7. Archivos a Crear / Modificar

### Archivos NUEVOS a Crear

| # | Archivo | Tipo | Descripción |
|---|---|---|---|
| 1 | `Models/PosConfig.cs` | Modelo | Configuración local del POS (JSON) |
| 2 | `Helpers/FileHelper.cs` | Helper | Gestión de directorios multiplataforma |
| 3 | `Views/Shared/ConfigView.axaml` | Vista | Interfaz de configuración |
| 4 | `Views/Shared/ConfigView.axaml.cs` | Code-behind | Code-behind de la vista |

### Archivos EXISTENTES a Implementar (actualmente vacíos)

| # | Archivo | Estado Actual | Acción |
|---|---|---|---|
| 5 | `Services/ConfigService.cs` | Vacío | Implementar completo |
| 6 | `Services/PrintService.cs` | Vacío | Implementar completo |
| 7 | `Services/ExportService.cs` | Vacío | Implementar completo |
| 8 | `ViewModels/Shared/ConfigViewModel.cs` | Vacío | Implementar completo |
| 9 | `Helpers/Constants.cs` | Vacío | Agregar constantes de rutas y defaults |

### Archivos EXISTENTES a Modificar

| # | Archivo | Modificación |
|---|---|---|
| 10 | `Services/TicketService.cs` | Recibir `lineWidth` desde `PosConfig` (ya acepta el parámetro, solo conectar) |
| 11 | `ViewModels/POS/SalesViewModel.cs` | Agregar llamada a `PrintService` después de venta exitosa |
| 12 | `ViewModels/POS/CashCloseViewModel.cs` | Agregar llamada a `PrintService` para ticket de corte |
| 13 | `ViewModels/Admin/ReportsViewModel.cs` | Agregar botón de exportar Excel con `ExportService` |
| 14 | `ViewModels/POS/SalesHistoryViewModel.cs` | Agregar exportación del historial de ventas |
| 15 | `App.axaml.cs` | Registrar `ConfigService`, `PrintService`, `ExportService` + inicializar `FileHelper` |

---

## 8. Plan de Implementación por Fases

### Fase 1: Infraestructura Base
**Archivos:** `PosConfig.cs`, `FileHelper.cs`, `Constants.cs`

1. Crear el modelo `PosConfig` con todos los campos de configuración.
2. Implementar `FileHelper` con detección de SO y creación de carpetas.
3. Definir constantes en `Constants.cs` (valores por defecto, rutas, etc.).
4. Verificar que `FileHelper.EnsureDirectoriesExist()` funcione en Windows y macOS.

### Fase 2: Servicios Core
**Archivos:** `ConfigService.cs`, `PrintService.cs`, `ExportService.cs`

1. Implementar `ConfigService` (lectura/escritura JSON).
2. Implementar `PrintService` con detección de impresoras.
3. Implementar `ExportService` con método genérico de exportación.
4. Registrar los 3 servicios en `App.axaml.cs`.
5. Llamar `ConfigService.LoadAsync()` y `FileHelper.EnsureDirectoriesExist()` al inicio.

### Fase 3: Vista de Configuración
**Archivos:** `ConfigViewModel.cs`, `ConfigView.axaml`, `ConfigView.axaml.cs`

1. Implementar `ConfigViewModel` con bindings a `PosConfig`.
2. Crear la vista AXAML con los controles definidos en la sección 2.4.
3. Implementar restricciones de rol (Admin vs Cajero).
4. Probar guardar/cargar configuración.

### Fase 4: Integración de Impresión
**Archivos:** `SalesViewModel.cs`, `CashCloseViewModel.cs`, `TicketService.cs`

1. Conectar `PrintService.PrintAsync()` al flujo de ventas.
2. Conectar `PrintService.PrintAsync()` al flujo de cortes de caja.
3. Pasar `PosConfig.TicketLineWidth` a `TicketService.GenerateTicketText()`.
4. Pruebas de impresión térmica y carta.

### Fase 5: Exportación Excel
**Archivos:** `ReportsViewModel.cs`, `SalesHistoryViewModel.cs`

1. Agregar botones de "Exportar a Excel" en las vistas de reportes.
2. Implementar exportación del historial de ventas.
3. Implementar exportación de cortes de caja.
4. Implementar exportación de catálogo de productos.
5. Mostrar notificación con ruta del archivo generado.

---

## Notas Adicionales

- **Tickets NO se guardan en `CasaCejaDocs`**: Solo los reportes Excel. Los datos de tickets ya están comprimidos en `Sale.TicketData` (blob SQLite vía `JsonCompressor`).
- **ClosedXML ya está en el proyecto**: No requiere agregar paquetes NuGet adicionales.
- **`ConfigService` vs `DatabaseService`**: La configuración es local por máquina (JSON en disco), mientras que los datos de negocio son por sucursal (SQLite sincronizable). Esto permite que cada terminal tenga su propia impresora y caja sin conflictos.
- **Patrón Event-Driven**: `ConfigService.ConfigChanged` permite que otros componentes reaccionen a cambios de configuración sin acoplamiento directo.

# Patrón: Tunnel KeyDown para interceptar teclas globalmente

## Problema

En Avalonia (y WPF), cuando un control como `TextBox` tiene el foco del sistema operativo,
los eventos de teclado llegan a ese control **antes** de que la ventana pueda procesarlos.
Esto causa que teclas como `Esc` inserten caracteres extraños en el campo en lugar de ejecutar
la acción esperada (ej. cerrar un dialog).

En macOS esto es especialmente notorio porque el OS **ignora** las llamadas programáticas de
foco (`Activate()`, `Focus()`) si otra ventana ya tiene el foco de teclado asignado.

## Solución: Tunnel routing

Avalonia tiene dos fases en el routing de eventos:

```
Window  ← Tunnel ▼ (se ejecuta primero, de padre a hijo)
  └── Grid
       └── TextBox  ← Bubble ▲ (se ejecuta después, de hijo a padre)
```

Registrando un handler con `RoutingStrategies.Tunnel` en la ventana raíz, el evento se
intercepta **antes** de que llegue a cualquier control hijo, sin importar cuál tenga el foco.

## Implementación

### 1. Registrar el handler en `OnLoaded`

```csharp
private void OnLoaded(object? sender, RoutedEventArgs e)
{
    this.AddHandler(InputElement.KeyDownEvent, OnWindowTunnelKeyDown, RoutingStrategies.Tunnel);
    // ... resto de inicialización
}
```

### 2. Handler que intercepta la tecla

```csharp
private void OnWindowTunnelKeyDown(object? sender, KeyEventArgs e)
{
    if (_activeDialog != null && e.Key == Key.Escape)
    {
        _activeDialog.Close();
        e.Handled = true; // cancela el routing — ningún control hijo recibe el evento
    }
}
```

`e.Handled = true` es crítico: detiene el routing y el `TextBox` nunca ve el `Esc`.

### 3. Guardar referencia al dialog abierto

El método que abre el dialog expone un callback para entregar la referencia antes de bloquearse:

```csharp
// En DialogHelper:
public static async Task ShowMyDialog(Window parent, Action<Window>? onCreated = null)
{
    var dialog = new Window { ... };
    onCreated?.Invoke(dialog);          // entrega referencia antes del await
    await dialog.ShowDialog(parent);
}

// En la vista:
private Window? _activeDialog;

private async Task OpenDialog()
{
    await DialogHelper.ShowMyDialog(this, d => _activeDialog = d);
    _activeDialog = null;               // se limpia al cerrarse
}
```

## Cuándo usar este patrón

- Un `TextBox` u otro control tiene el foco y está "robando" teclas que deberían cerrar un dialog.
- Se necesita un atajo de teclado global que funcione **independientemente del foco actual**.
- El comportamiento debe ser consistente entre macOS y Windows.

## Cuándo NO usarlo

- Si el handler de `OnKeyDown` de la ventana ya es suficiente (cuando ningún control
  consume la tecla antes). El tunnel agrega un handler extra que siempre se ejecuta,
  por lo que conviene usarlo solo cuando el bubbling normal falla.

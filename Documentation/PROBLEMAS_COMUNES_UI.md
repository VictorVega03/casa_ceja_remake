# Problemas Comunes en UI — Soluciones Documentadas

> Documento de referencia para evitar errores recurrentes en implementaciones futuras.

---

## Problema 1: Vistas/Diálogos que se cortan al fondo

### Síntoma
El contenido de un diálogo o ventana queda visualmente cortado en la parte inferior. Los botones del footer no son visibles o están parcialmente ocultos. El problema empeora o mejora dependiendo de la resolución del monitor, lo que indica que la solución no es robusta.

### Causa Raíz
Usar `Grid` con una fila `Height="*"` combinado con `ScrollViewer` y `SizeToContent="Height"` en la ventana. Esta combinación es incompatible:

- `Height="*"` le dice a la fila que ocupe **todo el espacio disponible**, pero con `SizeToContent` la ventana no tiene un tamaño fijo de referencia.
- El `ScrollViewer` dentro de esa fila no reporta una altura concreta al layout.
- Resultado: Avalonia no puede calcular correctamente la altura total de la ventana y la corta.

```xml
<!-- ❌ INCORRECTO — causa corte de contenido -->
<Window SizeToContent="Height">
    <Grid RowDefinitions="Auto,*,Auto">
        <Border Grid.Row="0" /> <!-- Header -->
        <ScrollViewer Grid.Row="1" /> <!-- Contenido — fila * es incompatible con SizeToContent -->
        <Border Grid.Row="2" /> <!-- Footer -->
    </Grid>
</Window>
```

### Solución Correcta
Usar `StackPanel` como elemento raíz de la ventana. Un `StackPanel` mide sus hijos secuencialmente y reporta la suma exacta de sus alturas. Con `SizeToContent="Height"`, la ventana se ajusta con precisión al contenido real sin importar la resolución del monitor.

```xml
<!-- ✅ CORRECTO — se ajusta al contenido en cualquier resolución -->
<Window SizeToContent="Height" MaxHeight="800">
    <StackPanel>
        <Border />  <!-- Header -->
        <StackPanel Margin="28,16,28,0" Spacing="16">
            <!-- Secciones de contenido directamente, sin ScrollViewer -->
        </StackPanel>
        <Border />  <!-- Footer -->
    </StackPanel>
</Window>
```

### Regla General
- Diálogos con contenido **conocido y fijo** (formularios, configuraciones): usar `StackPanel` raíz + `SizeToContent="Height"` + `MaxHeight` como tope de seguridad.
- Diálogos con contenido **variable o largo** (listas, historiales): usar `Grid` con filas fijas (`Auto`) para header/footer y `*` solo para el `ScrollViewer` central, con `Height` fijo en la ventana (no `SizeToContent`).

---

## Problema 2: Atajo de teclado Esc no cierra los diálogos

### Síntoma
Presionar `Esc` en un diálogo no hace nada, aunque el código aparentemente tiene un handler para esa tecla.

### Causa Raíz
Hay dos patrones incorrectos que no funcionan confiablemente en Avalonia para ventanas modales:

**Patrón incorrecto 1 — suscripción con `+=` en el constructor:**
```csharp
// ❌ INCORRECTO — el event handler no recibe eventos cuando un control hijo tiene el foco
public MyView()
{
    InitializeComponent();
    KeyDown += OnKeyDown; // No funciona si un TextBox o ComboBox tiene el foco
}
```

**Patrón incorrecto 2 — `KeyBinding` en XAML:**
```xml
<!-- ❌ INCORRECTO — no funciona confiablemente en diálogos modales -->
<Window.KeyBindings>
    <KeyBinding Gesture="Escape" Command="{Binding CloseCommand}" />
</Window.KeyBindings>
```

**Patrón incorrecto 3 — usar `OnDataContextChanged` para suscribir eventos:**
```csharp
// ❌ INCORRECTO — el DataContext puede cambiar antes de que la vista esté lista
protected override void OnDataContextChanged(EventArgs e)
{
    if (DataContext is MyViewModel vm)
        vm.CloseRequested += (s, args) => Close();
}
```

### Solución Correcta
El patrón estándar usado en **toda la aplicación** (`CashCloseView`, `CreateLayawayView`, `CreditLayawayDetailView`, etc.) es:

1. Campo privado `_viewModel` del tipo del ViewModel.
2. Suscripción a eventos en el handler de `Loaded` (no en el constructor ni en `OnDataContextChanged`).
3. `protected override void OnKeyDown` con `base.OnKeyDown(e)` al final.
4. Verificar `if (_viewModel != null)` antes de procesar atajos.
5. Ejecutar el **comando del ViewModel** (`_viewModel.CloseCommand.Execute(null)`), no llamar `Close()` directamente.
6. `Focusable="True"` en el AXAML de la ventana.

```csharp
// ✅ CORRECTO — patrón estándar de la aplicación
public partial class MyDialogView : Window
{
    private MyDialogViewModel? _viewModel;

    public MyDialogView()
    {
        InitializeComponent();
        Loaded += OnLoaded; // Suscribir en Loaded, no en el constructor
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as MyDialogViewModel;

        if (_viewModel != null)
        {
            _viewModel.CloseRequested += (s, args) => Close();
            // Otros eventos del ViewModel aquí
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_viewModel != null)
        {
            var shortcuts = new Dictionary<Key, Action>
            {
                { Key.Escape, () => _viewModel.CloseCommand.Execute(null) }
                // Otros atajos aquí
            };

            if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                return; // Salir si el atajo fue manejado
        }

        base.OnKeyDown(e); // Siempre llamar al base al final
    }
}
```

```xml
<!-- ✅ AXAML — agregar Focusable="True" en la ventana -->
<Window ...
        Focusable="True"
        ...>
```

### Por qué funciona este patrón
- `Loaded` garantiza que el `DataContext` ya está asignado y la vista está completamente inicializada.
- `protected override OnKeyDown` intercepta eventos de teclado a nivel de ventana **antes** de que los controles hijos los consuman, a diferencia de `KeyDown +=`.
- Ejecutar el comando del ViewModel en lugar de `Close()` respeta el flujo MVVM y permite que el ViewModel haga limpieza antes de cerrar.
- `base.OnKeyDown(e)` al final permite que otros controles reciban el evento si no fue manejado.

---

## Checklist para nuevos diálogos

Antes de dar por terminado un nuevo diálogo, verificar:

- [ ] La ventana usa `StackPanel` raíz si el contenido es fijo, o `Grid` con `Height` fijo si tiene lista con scroll.
- [ ] Si usa `SizeToContent="Height"`, tiene `MaxHeight` definido como tope de seguridad.
- [ ] El code-behind tiene campo `_viewModel` privado.
- [ ] Los eventos del ViewModel se suscriben en `Loaded`, no en el constructor.
- [ ] `OnKeyDown` usa `protected override` + `base.OnKeyDown(e)` al final.
- [ ] Esc ejecuta `_viewModel.CloseCommand.Execute(null)`.
- [ ] El AXAML tiene `Focusable="True"` en la ventana.

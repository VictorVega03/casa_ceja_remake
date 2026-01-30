using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace casa_ceja_remake.Helpers
{
    /// <summary>
    /// Helper para manejar atajos de teclado de forma consistente en toda la aplicación.
    /// Evita la propagación de eventos a ventanas padre cuando los atajos son manejados.
    /// </summary>
    public static class KeyboardShortcutHelper
    {
        /// <summary>
        /// Procesa un evento de teclado con un diccionario de atajos simples (sin modificadores).
        /// Si el atajo es manejado, marca el evento como Handled automáticamente.
        /// </summary>
        /// <param name="e">El evento de teclado</param>
        /// <param name="shortcuts">Diccionario de teclas y acciones correspondientes</param>
        /// <returns>true si el atajo fue manejado, false si no</returns>
        public static bool HandleShortcut(KeyEventArgs e, Dictionary<Key, Action> shortcuts)
        {
            if (shortcuts.TryGetValue(e.Key, out var action))
            {
                action.Invoke();
                e.Handled = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Procesa un evento de teclado con modificadores (Ctrl, Alt, Shift).
        /// Si el atajo es manejado, marca el evento como Handled automáticamente.
        /// </summary>
        /// <param name="e">El evento de teclado</param>
        /// <param name="shortcuts">Diccionario de combinaciones de tecla+modificador y acciones</param>
        /// <returns>true si el atajo fue manejado, false si no</returns>
        public static bool HandleShortcutWithModifiers(
            KeyEventArgs e, 
            Dictionary<(Key key, KeyModifiers modifiers), Action> shortcuts)
        {
            var combo = (e.Key, e.KeyModifiers);
            if (shortcuts.TryGetValue(combo, out var action))
            {
                action.Invoke();
                e.Handled = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Método de extensión para procesar múltiples teclas con la misma acción.
        /// Útil para casos como Enter y F5 haciendo lo mismo.
        /// </summary>
        public static bool HandleShortcuts(KeyEventArgs e, Action action, params Key[] keys)
        {
            foreach (var key in keys)
            {
                if (e.Key == key)
                {
                    action.Invoke();
                    e.Handled = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Alias de HandleShortcutWithModifiers para compatibilidad.
        /// Procesa un evento de teclado con modificadores (Ctrl, Alt, Shift).
        /// </summary>
        /// <param name="e">El evento de teclado</param>
        /// <param name="shortcuts">Diccionario de combinaciones de tecla+modificador y acciones</param>
        /// <returns>true si el atajo fue manejado, false si no</returns>
        public static bool HandleComplexShortcut(
            KeyEventArgs e,
            Dictionary<(Key key, KeyModifiers modifiers), Action> shortcuts)
        {
            return HandleShortcutWithModifiers(e, shortcuts);
        }
    }
}

# 📁 Cómo Encontrar la Base de Datos

La base de datos de Casa Ceja se guarda en la ubicación estándar de **Application Data** de cada sistema operativo. Esta carpeta está **oculta por defecto** para proteger datos de aplicaciones.

---

## 🍎 macOS

### Ubicación
```
/Users/[TuUsuario]/Library/Application Support/CasaCeja/casaceja.db
```
o en forma corta:
```
~/Library/Application Support/CasaCeja/casaceja.db
```

### Método 1: Ir directamente (RECOMENDADO)
1. Abre **Finder**
2. Presiona `⌘ Cmd + ⇧ Shift + G` (o menú "Ir" → "Ir a la carpeta...")
3. Pega exactamente: `~/Library/Application Support/CasaCeja`
4. Presiona `Enter`
5. Verás el archivo **casaceja.db**

### Método 2: Mostrar carpetas ocultas
1. Abre **Finder**
2. Presiona `⌘ Cmd + ⇧ Shift + .` (punto)
3. Ahora verás carpetas antes ocultas (se ven transparentes)
4. Ve a tu carpeta de usuario → `Library` → `Application Support` → `CasaCeja`

### Método 3: Desde Terminal
```bash
open ~/Library/Application\ Support/CasaCeja/
```

### Crear acceso directo en el escritorio
```bash
ln -s ~/Library/Application\ Support/CasaCeja ~/Desktop/CasaCeja_DB
```

---

## 🪟 Windows

### Ubicación
```
C:\Users\[TuUsuario]\AppData\Roaming\CasaCeja\casaceja.db
```

### Método 1: Ir directamente (RECOMENDADO)
1. Abre **Explorador de Archivos**
2. Presiona `Windows + R` o escribe en la barra de direcciones
3. Pega exactamente: `%APPDATA%\CasaCeja`
4. Presiona `Enter`
5. Verás el archivo **casaceja.db**

### Método 2: Mostrar carpetas ocultas
1. Abre **Explorador de Archivos**
2. Ve a la pestaña **Vista**
3. Marca la casilla **"Elementos ocultos"**
4. Ve a `C:\Users\[TuUsuario]\AppData\Roaming\CasaCeja`

### Método 3: Desde CMD/PowerShell
```cmd
explorer %APPDATA%\CasaCeja
```

---

## 🐧 Linux

### Ubicación
```
~/.local/share/CasaCeja/casaceja.db
```

### Método 1: Desde Terminal
```bash
nautilus ~/.local/share/CasaCeja/
```
o
```bash
xdg-open ~/.local/share/CasaCeja/
```

### Método 2: Mostrar archivos ocultos
1. Abre el **Explorador de Archivos**
2. Presiona `Ctrl + H` para mostrar archivos ocultos
3. Ve a tu carpeta home → `.local` → `share` → `CasaCeja`

---

## 🔧 Para Desarrolladores

### Ver la ruta en consola al iniciar la app
Al ejecutar con `dotnet run`, la consola muestra:
```
💾 Inicializando DatabaseService...
```

### Abrir desde código (Debug)
Agrega este código temporal:
```csharp
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
    "CasaCeja"
);
Console.WriteLine($"📂 Ruta BD: {dbPath}");
```

### Acceso rápido con script

**macOS/Linux** - Crea `open-db.sh`:
```bash
#!/bin/bash
open ~/Library/Application\ Support/CasaCeja/
```

**Windows** - Crea `open-db.bat`:
```batch
@echo off
explorer %APPDATA%\CasaCeja
```

---

## 🗃️ Herramientas para ver la BD

- **DB Browser for SQLite** (Gratis, multiplataforma)
- **DataGrip** (JetBrains, pago)
- **DBeaver** (Gratis, multiplataforma)
- **TablePlus** (Mac, pago con free tier)

---

## ⚠️ Importante

- ✅ Esta ubicación es el **estándar de la industria**
- ✅ Se respalda automáticamente con Time Machine / Backup de Windows
- ✅ Cada usuario tiene su propia base de datos (multiusuario)
- ⚠️ **NO mover** el archivo a otra ubicación - la app no lo encontrará
- 💾 Para backups, **copia** el archivo a otra ubicación, no lo muevas

---

## 🆘 Solución de Problemas

### "No encuentro la carpeta Library en Mac"
→ Está oculta. Usa `⌘ Cmd + ⇧ Shift + G` y pega la ruta directamente.

### "La carpeta AppData no existe en Windows"
→ Está oculta. Usa `Windows + R` y escribe `%APPDATA%`

### "¿Puedo mover la BD a otra ubicación?"
→ No recomendado. Si es necesario, habría que modificar el código en `DatabaseService.cs`

### "Necesito hacer backup"
→ Simplemente **copia** el archivo `casaceja.db` a donde quieras (USB, Dropbox, etc.)

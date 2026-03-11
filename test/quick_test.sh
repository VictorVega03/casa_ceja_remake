#!/bin/bash

# ================================================
# Build Rápido para Pruebas - Casa Ceja
# ================================================

set -e

clear
echo ""
echo "🔨 Compilando Casa Ceja para Windows..."
echo ""

# Verificar que estamos en el proyecto
if [ ! -f "casa_ceja_remake.csproj" ]; then
    echo "❌ Error: Ejecuta este script desde la raíz del proyecto"
    exit 1
fi

# Limpiar
echo "🧹 Limpiando..."
dotnet clean > /dev/null 2>&1
rm -rf bin/Release/net8.0/win-x64/ 2>/dev/null

# Publicar
echo "📦 Generando ejecutable para Windows..."
dotnet publish -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  > /dev/null 2>&1

if [ $? -ne 0 ]; then
    echo "❌ Error en la compilación"
    echo ""
    echo "Intenta ejecutar manualmente para ver el error:"
    echo "dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true"
    exit 1
fi

# Verificar que se generó el exe
if [ ! -f "bin/Release/net8.0/win-x64/publish/casa_ceja_remake.exe" ]; then
    echo "❌ No se encontró el ejecutable"
    exit 1
fi

# ── Carpeta Windows en el escritorio ──────────────────────────────────────────
echo "📋 Preparando carpeta Windows..."
DESKTOP_DIR=~/Desktop/CasaCeja
mkdir -p "$DESKTOP_DIR/Data/Database"

cp bin/Release/net8.0/win-x64/publish/casa_ceja_remake.exe "$DESKTOP_DIR/"
cp Data/Database/ScriptInicial_CasaCeja.sql "$DESKTOP_DIR/Data/Database/ScriptInicial_CasaCeja.sql"

SIZE=$(du -h "$DESKTOP_DIR/casa_ceja_remake.exe" | cut -f1)

# ── Acceso directo de macOS en el escritorio ──────────────────────────────────
echo "🍎 Creando acceso directo para macOS..."
APP_SHORTCUT=~/Desktop/"Casa Ceja.command"
cat > "$APP_SHORTCUT" << 'HEREDOC'
#!/bin/bash
# Acceso directo - Casa Ceja POS
cd "$(dirname "$0")"
open -n "/Applications/CasaCeja/casa_ceja_remake.app" 2>/dev/null || \
dotnet "/Applications/CasaCeja/casa_ceja_remake.dll" 2>/dev/null || \
echo "No se encontró la instalación de Casa Ceja"
HEREDOC
chmod +x "$APP_SHORTCUT"

# Aplicar el .icns directamente al acceso directo
ICNS_FILE="$(pwd)/Assets/LogoCasaCejaMac.icns"
if [ -f "$ICNS_FILE" ]; then
    osascript << APPLESCRIPT > /dev/null 2>&1
use framework "Foundation"
use framework "AppKit"
set iconPath to "$ICNS_FILE"
set filePath to "$APP_SHORTCUT"
set iconImage to current application's NSImage's alloc()'s initWithContentsOfFile:iconPath
current application's NSWorkspace's sharedWorkspace()'s setIcon:iconImage forFile:filePath options:0
APPLESCRIPT
    echo "   ✓ Ícono aplicado al acceso directo"
fi

echo ""
echo "✅ ¡Listo!"
echo ""
echo "💻 Windows:"
echo "   📁 Carpeta:  ~/Desktop/CasaCeja/"
echo "      ├── casa_ceja_remake.exe  ($SIZE)"
echo "      └── Data/Database/ScriptInicial_CasaCeja.sql"
echo "   ℹ️  El ícono está embebido en el .exe"
echo ""
echo "🍎 macOS:"
echo "   📄 Acceso directo: ~/Desktop/Casa Ceja.command"
echo ""
echo "🔄 Pasos para Windows:"
echo "   1. Copiar la carpeta CasaCeja/ completa a Windows"
echo "   2. Doble clic en casa_ceja_remake.exe"
echo "   3. Login: admin / admin"
echo ""
echo "💡 Tip: Para ver la carpeta en Finder:"
echo "   open ~/Desktop/CasaCeja"
echo ""
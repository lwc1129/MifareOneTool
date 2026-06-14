#!/usr/bin/env bash
# MifareOneTool — macOS Release Build Script
# Run from repo root: ./scripts/build-macos.sh [version] [arch]
# arch: arm64 (default, Apple Silicon) | x64 (Intel)
set -euo pipefail

VERSION="${1:-1.0.0}"
ARCH="${2:-arm64}"   # arm64 or x64
RID="osx-${ARCH}"

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT_DIR="$REPO_ROOT/src/MifareOneTool.UI"
OUT_DIR="$REPO_ROOT/publish/$RID"
APP_NAME="MifareOneTool"
APP_BUNDLE="$REPO_ROOT/publish/${APP_NAME}-${VERSION}-${RID}.app"
DMG_PATH="$REPO_ROOT/publish/${APP_NAME}-${VERSION}-${RID}.dmg"

echo "=== MifareOneTool macOS Build ==="
echo "Version : $VERSION"
echo "Arch    : $ARCH ($RID)"
echo "Output  : $OUT_DIR"

# Check nfc-bin-macos has tools
NFC_BIN="$PROJECT_DIR/nfc-bin-macos"
REQUIRED_TOOLS=("nfc-scan-device" "nfc-mfclassic" "nfc-list" "mfoc" "nfc-mfsetuid")
MISSING=()
for tool in "${REQUIRED_TOOLS[@]}"; do
    [[ -f "$NFC_BIN/$tool" ]] || MISSING+=("$tool")
done
if [[ ${#MISSING[@]} -gt 0 ]]; then
    echo "⚠️  Missing NFC tools in nfc-bin-macos/: ${MISSING[*]}"
    echo "   Run: brew install libnfc mfoc"
    echo "   Then: cp \$(which mfoc) $NFC_BIN/"
    echo "   (App will fall back to system Homebrew tools if not bundled)"
fi

# Publish
echo ""
echo "Publishing..."
dotnet publish "$PROJECT_DIR" \
    --configuration Release \
    --runtime "$RID" \
    --self-contained true \
    --output "$OUT_DIR"

# Build .app bundle structure
echo ""
echo "Building .app bundle..."
APP_CONTENTS="$APP_BUNDLE/Contents"
mkdir -p "$APP_CONTENTS/MacOS"
mkdir -p "$APP_CONTENTS/Resources"

# Copy published output
cp -R "$OUT_DIR/." "$APP_CONTENTS/MacOS/"

# Make binary executable
chmod +x "$APP_CONTENTS/MacOS/MifareOneTool.UI"

# Info.plist
cat > "$APP_CONTENTS/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>MifareOneTool.UI</string>
    <key>CFBundleIdentifier</key>
    <string>com.mifareonetool.app</string>
    <key>CFBundleName</key>
    <string>MifareOneTool</string>
    <key>CFBundleDisplayName</key>
    <string>MifareOneTool</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSUSBAccessGranted</key>
    <true/>
</dict>
</plist>
PLIST

# Fix execute permissions on bundled nfc tools
if [[ -d "$APP_CONTENTS/MacOS/nfc-bin-macos" ]]; then
    chmod +x "$APP_CONTENTS/MacOS/nfc-bin-macos/"* 2>/dev/null || true
    echo "✓ Set executable permissions on nfc-bin-macos/ tools"
fi

echo ""
echo "=== Done ==="
echo ".app : $APP_BUNDLE"
echo ""
echo "To create a DMG (optional, requires create-dmg):"
echo "  brew install create-dmg"
echo "  create-dmg \\"
echo "    --volname 'MifareOneTool ${VERSION}' \\"
echo "    --window-size 600 400 \\"
echo "    --app-drop-link 450 200 \\"
echo "    '$DMG_PATH' \\"
echo "    '$APP_BUNDLE'"
echo ""
echo "To bypass Gatekeeper on first launch (unsigned app):"
echo "  xattr -d com.apple.quarantine '$APP_BUNDLE'"

#!/bin/bash
# Create macOS .app bundle
# Run this after build-macos.sh

PROJECT_NAME="WeathersnakeAvalonia"
BUNDLE_NAME="Weather Juice"
BUNDLE_ID="com.weathersnake.avalonia"
BUNDLE_VERSION="1.0.0"
UNIVERSAL_BINARY="./publish/Universal/$PROJECT_NAME"
OUTPUT_DIR="./publish"

echo "Creating .app bundle..."

# Create directory structure
mkdir -p "$OUTPUT_DIR/$BUNDLE_NAME.app/Contents/MacOS"
mkdir -p "$OUTPUT_DIR/$BUNDLE_NAME.app/Contents/Resources"

# Copy the universal binary
cp "$UNIVERSAL_BINARY" "$OUTPUT_DIR/$BUNDLE_NAME.app/Contents/MacOS/$PROJECT_NAME"

# Create Info.plist
cat > "$OUTPUT_DIR/$BUNDLE_NAME.app/Contents/Info.plist" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>Weather Juice</string>
    <key>CFBundleIdentifier</key>
    <string>com.weathersnake.avalonia</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>WSAV</string>
    <key>CFBundleExecutable</key>
    <string>WeathersnakeAvalonia</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright 2026 WeatherSnake Team</string>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.13</string>
</dict>
</plist>
EOF

# Copy assets if they exist
if [ -f "./Assets/logo.png" ]; then
    cp "./Assets/logo.png" "$OUTPUT_DIR/$BUNDLE_NAME.app/Contents/Resources/AppIcon.png"
fi

# Set executable permissions
chmod +x "$OUTPUT_DIR/$BUNDLE_NAME.app/Contents/MacOS/$PROJECT_NAME"

echo ".app bundle created at $OUTPUT_DIR/$BUNDLE_NAME.app"
echo ""
echo "To create distributable zip:"
echo "  ditto -c -k --keepParent $OUTPUT_DIR/$BUNDLE_NAME.app $OUTPUT_DIR/$BUNDLE_NAME.zip"
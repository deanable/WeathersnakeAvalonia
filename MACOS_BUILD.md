# WeathersnakeAvalonia macOS Build Scripts

## Building on Windows (Cross-compilation)

From Windows, you can build but cannot link to a universal binary. Run these commands:

```powershell
# Build for macOS x64
dotnet publish -r osx-x64 -c Release

# Build for macOS arm64
dotnet publish -r osx-arm64 -c Release
```

## Building on macOS

Copy build-macos.sh to your Mac and run:

```bash
chmod +x build-macos.sh
./build-macos.sh
```

Then create the .app bundle:

```bash
./create-app-bundle.sh
```

## Code Signing and Notarization (macOS only)

Requires Apple Developer account:

```bash
# Sign the app
codesign --force --options runtime --sign "Developer ID Application: Your Name (XXXXXXXXXX)" WeathersnakeAvalonia.app

# Verify signature
codesign --verify --deep --strict WeathersnakeAvalonia.app

# Submit for notarization
xcrun notarytool submit WeathersnakeAvalonia.zip --wait --keychain-profile "AC_PASSWORD"

# Staple the notarization ticket
xcrun stapler staple WeathersnakeAvalonia.app
```
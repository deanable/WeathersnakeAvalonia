# Weather Juice

Historical weather data visualization tool that fetches and displays temperature and precipitation data from Open-Meteo.

## Features

- Fetches historical weather data from Open-Meteo API
- Displays temperature (max/min) and precipitation on an interactive chart
- Supports multiple locations worldwide
- Configurable time periods and depths
- Export charts as JPG images

## Download

Pre-built releases are available on the [Releases](https://github.com/deanable/WeathersnakeAvalonia/releases) page:

- **Windows**: `WeatherJuice-Setup.exe` (Windows installer)
- **macOS**: `WeatherJuice-macos.zip` (ZIP archive) or `WeatherJuice.pkg` (macOS installer)

## Installation

### macOS

#### Option 1: ZIP Archive

1. Download `WeatherJuice-macos.zip` from [Releases](https://github.com/deanable/WeathersnakeAvalonia/releases)
2. Open Finder and navigate to your Downloads folder
3. Double-click `WeatherJuice-macos.zip` to extract it
4. Drag `WeatherJuice.app` to your Applications folder
5. **Launch the app for the first time:**

   - Attempt to open the app by double-clicking it in Applications
   - If you see a warning: "Weather Juice can't be opened because it is from an unidentified developer"

     1. Open **System Settings** → **Privacy & Security**
     2. Look for the message: "Weather Juice was blocked from use because it is not from an identified developer"
     3. Click **"Open Anyway"** and confirm

   - Alternatively, right-click the app and select **Open**, then click **Open** in the dialog

#### Option 2: PKG Installer (Recommended)

1. Download `WeatherJuice.pkg` from [Releases](https://github.com/deanable/WeathersnakeAvalonia/releases)
2. Double-click `WeatherJuice.pkg` to launch the installer
3. If you see a security warning: "Weather Juice.pkg can't be opened because it is from an unidentified developer":

   - **Press Ctrl** while clicking the file, then select **Open**
   - Or go to **System Settings** → **Privacy & Security** and click **"Open Anyway"**

4. Follow the installer prompts:
   - Click **Continue** → **Install** (you may need to enter your password)
5. The app will be installed to `/Applications/WeatherJuice.app`
6. Launch from Applications folder (see Step 5 above for first-run instructions)

#### Allowing Apps from Unidentified Developers (Permanent Fix)

If you want to disable this warning permanently for all apps:

1. Open **Terminal** (from Applications → Utilities)
2. Run: `sudo spctl --master-disable`
3. Enter your password when prompted

This allows apps from any developer. To re-enable later: `sudo spctl --master-enable`

### Windows

#### Installer (Recommended)

1. Download `WeatherJuice-Setup.exe` from [Releases](https://github.com/deanable/WeathersnakeAvalonia/releases)
2. Double-click the installer to run it
3. If you see a SmartScreen warning: "Windows Defender SmartScreen prevented an unrecognized app from starting":

   - Click **More info** (if visible)
   - Click **Run anyway**

4. Follow the installer prompts to complete installation

#### Portable Version (No Install)

1. Download the portable ZIP from [Releases](https://github.com/deanable/WeathersnakeAvalonia/releases)
2. Right-click the ZIP file → **Extract All**
3. Open the extracted folder
4. Double-click `WeathersnakeAvalonia.exe`

#### Bypassing the Untrusted App Warning (Permanent Fix)

If you prefer not to see the SmartScreen warning:

1. Open **Windows Security** → **App & browser control**
2. Turn off **SmartScreen for Microsoft Edge** and **SmartScreen for Windows Store apps**
3. Or right-click the exe file → **Properties** → check **Unblock** under "Security"

## Building from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 or macOS 10.15+

### Build Commands

```bash
# Clone the repository
git clone https://github.com/deanable/WeathersnakeAvalonia.git
cd WeathersnakeAvalonia

# Build
dotnet build

# Run
dotnet run

# Publish for your platform
dotnet publish -c Release

# Publish for Windows
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win-x64

# Publish for macOS
dotnet publish -c Release -r osx-x64 --self-contained -o ./publish/osx-x64
dotnet publish -c Release -r osx-arm64 --self-contained -o ./publish/osx-arm64
```

## Usage

1. **Select Location**: Choose a preset city or enter a custom city name
2. **Select Period**: Choose a date range or use a custom range
3. **Configure Depth**: Set how many years of historical data to fetch
4. **Select Units**: Celsius or Fahrenheit
5. **Click "Fetch Data"**: Retrieve weather data from Open-Meteo API
6. **View Chart**: Interactive chart shows temperature and precipitation over time
7. **Export**: Click "Save to JPG" to save the chart as an image

### Chart Legend

- **Red line/dots**: Maximum temperature
- **Orange line/dots**: Minimum temperature
- **Blue bars**: Precipitation

## Data Source

Weather data is sourced from the [Open-Meteo Archive API](https://open-meteo.com/en/archive), providing historical weather data globally at no cost.

## License

Copyright 2026 Weather Juice. See LICENSE file for details.

## Support

For issues or feature requests, please [open an issue](https://github.com/deanable/WeathersnakeAvalonia/issues).
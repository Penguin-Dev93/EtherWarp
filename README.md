# EtherWarp

A modern WPF/.NET 8 Windows desktop tool for IT professionals to manage and apply network interface presets. Save static IP configurations (IP, subnet, gateway, DNS) as named presets and apply them to your network adapters instantly.

## Requirements

- Windows 10 or Windows 11 (x64)
- **Must run as Administrator** — required to modify network adapter settings via `netsh`

## Features

- **Configuration Tab** — Create, edit, and delete named network presets
- **Interface Tab** — Select a preset and apply it to an adapter, or reset an adapter to DHCP
- Dark/Light theme toggle
- Presets saved locally in `%APPDATA%\EtherWarp\presets.json`
- Self-contained single-file EXE — no .NET runtime installation required

## Build Instructions

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Development build

```powershell
dotnet build EtherWarp/EtherWarp.csproj
```

### Self-contained publish (produces single EXE)

```powershell
dotnet publish EtherWarp/EtherWarp.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish
```

The output EXE will be at `./publish/EtherWarp.exe` (~60–90 MB self-contained).

## GitHub Releases / CI

The build pipeline in `.github/workflows/build-release.yml` triggers automatically when you push a version tag.

To create a release:

```bash
git tag v1.2.0
git push origin v1.2.0
```

GitHub Actions will build the self-contained EXE, attach it to a new GitHub Release, and auto-generate release notes from commits.

You can also trigger the workflow manually via **Actions → Build & Release → Run workflow**.

## Usage

### Configuration Tab

1. Click **New** to create a preset.
2. Fill in the preset name, select the target adapter from the dropdown, and enter the IP address, subnet mask, and optionally gateway and DNS servers.
3. Click **Save** — the preset appears in the left panel.
4. Select an existing preset and click **Edit** to modify it, or **Delete** to remove it.

### Interface Tab

1. Select a preset from the dropdown. An info card shows the configuration that will be applied.
2. Click **▶ Execute** to apply the static IP configuration to the adapter.
3. Click **↺ Reset to DHCP** to revert the adapter to automatic (DHCP) configuration.

> **Note:** The app must be run as Administrator for network changes to take effect. A UAC elevation prompt will appear on launch.

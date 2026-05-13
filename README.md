# Smart Bulb Controller WPF

A Windows desktop app for controlling DAYBETTER RGBCW smart bulbs over local LAN using the Tuya local API. No cloud required after initial setup.

![CI](https://github.com/rongner/SmartBulbControllerWPF/actions/workflows/ci.yml/badge.svg)

---

## Features

- **Device discovery** — scan your local network for Tuya-compatible bulbs
- **Power toggle** — instant on/off with status indicator
- **Brightness** — 0–100 % slider with debounced device updates
- **Color temperature** — warm to cool white (0–100 %)
- **RGB color mode** — red/green/blue sliders + hex input with live color swatch
- **Presets** — 7 built-in scenes (Warm White, Movie Night, Focus, …); save and delete custom presets
- **NBA game alerts** — set the lights to your team's color before game time; auto-revert after
- **Theme** — follows Windows dark/light mode; manual override via View menu
- **Launch on startup** — register/remove the Windows startup shortcut from within the app
- **Installer** — Inno Setup package with automatic .NET 10 runtime download

---

## Requirements

- Windows 10 / 11 (x64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — the installer will download this automatically if missing
- DAYBETTER RGBCW bulb connected to the same LAN

---

## Getting the Tuya Local Key

The app communicates locally and needs your bulb's **Device ID** and **Local Key**:

1. Create an account at [iot.tuya.com](https://iot.tuya.com) (free tier is sufficient)
2. Go to **Cloud → Development → Create Cloud Project** — choose "Smart Home"
3. Link your DAYBETTER app account under **Devices → Link Tuya App Account**
4. Your devices will appear with their **Device ID** visible in the list
5. Open the device detail page to find the **Local Key** (also called "Device Secret")

> The Local Key is stored encrypted using Windows DPAPI and never leaves your machine.

---

## Installation (Installer)

1. Download `SmartBulbController-Setup.exe` from the [Releases](../../releases) page
2. Run the installer — it will automatically download .NET 10 if needed and reboot if required
3. Launch **Smart Bulb Controller** from the Start menu or desktop shortcut

---

## Installation (Manual / Dev)

```powershell
git clone https://github.com/rongner/SmartBulbControllerWPF.git
cd SmartBulbControllerWPF
dotnet build SmartBulbControllerWPF.slnx --configuration Release
dotnet run --project src/SmartBulbControllerWPF
```

---

## Connecting to a Bulb

1. Click **Scan Network** — discovered devices appear in the left panel
2. Select a device and click **Connect** (or right-click → Connect)
3. In the dialog, enter the IP address, Device ID, and Local Key
4. The status indicator turns green and controls become active

> On first run you can also enter the IP/ID/Key directly without scanning.

---

## NBA Game Alerts

1. Enable alerts with the toggle in the **NBA Game Alerts** section
2. Pick your team from the dropdown
3. Set how many minutes before tip-off to trigger the alert (default 5)
4. Set how many hours after tip-off to revert the lights (default 3)
5. Set the brightness for the alert flash (default 100 %)

The app polls the ESPN schedule every 5 minutes. No API key is required.

---

## Architecture

| Layer | Technology |
|---|---|
| UI | WPF + MahApps.Metro 2.4 |
| MVVM | CommunityToolkit.Mvvm (source generators) |
| DI | Microsoft.Extensions.DependencyInjection |
| Device protocol | TuyaNet 1.0.3 (local LAN, AES-128) |
| Settings | JSON + DPAPI encryption for the local key |
| Logging | Serilog → `%AppData%\SmartBulbControllerWPF\logs\app-YYYY-MM-DD.log` |
| Tests | xUnit + Moq (105 tests) |
| Installer | Inno Setup 6.3 |

---

## Known Limitations

- **Windows only** — DPAPI and registry startup require Windows; TuyaNet is Windows-focused
- **No code signing** — Windows SmartScreen will warn on first run; dismiss with "More info → Run anyway"
- **Single bulb** — the app connects to one device at a time; multi-device support is a future feature
- **Tuya protocol v3.3** — tested against DAYBETTER bulbs; other Tuya devices may work but are untested

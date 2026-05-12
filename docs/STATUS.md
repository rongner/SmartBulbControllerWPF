# SmartBulbControllerWPF — Project Status

## Goal

Portfolio project demonstrating WPF, MVVM, and local IoT device control.
Controls DAYBETTER RGBCW Wi-Fi smart bulbs (Tuya-based) over the local network
without any cloud dependency.

---

## Tech Stack

- **WPF** — Windows desktop app, .NET 8
- **MVVM** — CommunityToolkit.Mvvm (source generators)
- **TuyaNet** (NuGet) — local LAN Tuya device control, no cloud required
- **Bulbs** — DAYBETTER A19 E26 RGBCW (Tuya-based), controlled via local key

---

## Requirements

### Device Discovery & Connection
- [ ] Scan local network for Tuya devices
- [ ] Display found devices with name and IP
- [ ] Connect to a device using device ID + local key
- [ ] Store device credentials in local settings (no hardcoding)
- [ ] One-time setup guide in README for obtaining device ID and local key

### Color Control
- [ ] Color wheel picker
- [ ] RGB sliders
- [ ] Hex color input
- [ ] Brightness slider (0–100%)
- [ ] Color temperature slider (warm white ↔ cool white)
- [ ] Apply changes in real time (debounced — no flooding the device)

### Scenes / Presets
- [ ] Named presets: Warm White, Cool White, Reading, Movie, Party
- [ ] Animated scenes: slow color cycle, pulse, strobe
- [ ] Save custom presets by name

### UI / UX
- [ ] MVVM architecture — no code-behind logic
- [ ] Dark theme
- [ ] Device on/off toggle
- [ ] Connection status indicator

### Project Setup
- [ ] WPF project scaffolded (.NET 8)
- [ ] GitHub repo with CI (build + installer)
- [ ] README with setup steps including how to get Tuya local key

### Installer (Inno Setup)
- [ ] CI pipeline produces a `.exe` installer via Inno Setup
- [ ] `publish` step builds self-contained single-file WPF executable
- [ ] Inno Setup script (`installer/setup.iss`) packages the published output
- [ ] Installer handles: install directory, Start Menu shortcut, desktop shortcut (optional), uninstaller
- [ ] Versioned installer filename: `SmartBulbControllerWPF-{version}-setup.exe`
- [ ] GitHub Actions uploads installer as a release asset (or artifact) on each build
- [ ] Prerequisite detection: check if required .NET Desktop Runtime is already installed
- [ ] If missing, download .NET installer from Microsoft at runtime (using Inno Setup 6.1+ `DownloadTemporaryFile`)
- [ ] Run .NET installer silently; if it requires a reboot, prompt user and reboot
- [ ] After reboot, installer automatically resumes and completes (via registry run key)
- [ ] Additional prereqs (if any) follow the same detect → download → install → reboot-if-needed pattern

---

### Team Game Alerts
- [ ] User selects a favorite NBA team from a dropdown (pre-loaded list with team primary colors)
- [ ] App fetches the team's schedule via ESPN's public API (`site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{id}/schedule`)
- [ ] Background service polls schedule and triggers light change N minutes before tip-off (default: 5 min, configurable)
- [ ] Lights set to team primary color at configured brightness when alert fires
- [ ] Configurable lead time (1–60 minutes)
- [ ] Revert lights to previous state after a fixed duration from tip-off (default: 3 hours, configurable)
- [ ] Team color library: at minimum the 30 NBA teams with their primary hex colors
- [ ] Alert can be enabled/disabled per-team or globally
- [ ] Works passively in background — no need to have the main window open (pairs with tray icon)

---

## Optional Enhancements

- [ ] Control multiple bulbs simultaneously (groups)
- [ ] Schedule — turn on/off at a set time
- [ ] Reactive scene driven by audio input (mic volume → brightness)
- [ ] Tray icon for quick on/off without opening full UI

---

## Device Notes

- **Brand:** DAYBETTER A19 E26 RGBCW (6-pack) — Amazon ASIN B08THGPCQX
- **Protocol:** Tuya local LAN (port 6668)
- **Local key:** obtained via tuyapy or iot.tuya.com developer account (one-time)
- **TuyaNet docs:** https://github.com/ClusterM/tuyanet

---

## Status

Project not yet started. Next step: scaffold WPF project and install TuyaNet.
Prerequisite: obtain device ID and local key for the DAYBETTER bulbs.

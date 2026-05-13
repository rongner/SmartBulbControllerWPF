# SmartBulbControllerWPF — Project Status

## Goal

Portfolio project demonstrating WPF, MVVM, and local IoT device control.
Controls DAYBETTER RGBCW Wi-Fi smart bulbs (Tuya-based) over the local network
without any cloud dependency.

---

## Tech Stack

- **WPF** — Windows desktop app, .NET 10
- **MVVM** — CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]` source generators)
- **UI Theme** — MahApps.Metro (polished WPF controls, built-in light/dark theme support)
- **TuyaNet** (NuGet) — local LAN Tuya device control, no cloud required
- **NAudio** (NuGet) — microphone capture for audio-reactive scene
- **Serilog** (NuGet) — structured rolling-file logging
- **H.NotifyIcon.Wpf** (NuGet) — system tray icon
- **Bulbs** — DAYBETTER A19 E26 RGBCW (Tuya-based), controlled via local key

---

## Requirements

### Device Discovery & Connection
- [x] Scan local network for Tuya devices
- [x] Display found devices with name and IP
- [x] Connect to a device using device ID + local key
- [x] Store device credentials in local settings (no hardcoding)
- [x] One-time setup guide in README for obtaining device ID and local key

### Color Control
- [x] Color wheel picker
- [x] RGB sliders
- [x] Hex color input
- [x] Brightness slider (0–100%)
- [x] Color temperature slider (warm white ↔ cool white)
- [x] Apply changes in real time (debounced — no flooding the device)

### Scenes / Presets
- [x] Named presets: Warm White, Cool White, Reading, Movie, Party
- [x] Animated scenes: slow color cycle, pulse, strobe
- [x] Save custom presets by name

### UI / UX
- [x] MVVM architecture — no code-behind logic; ViewModels extend `ObservableObject`, commands via `[RelayCommand]` source generator
- [x] `ViewModelBase` — extends `ObservableObject`; adds shared properties (`IsBusy`, `ErrorMessage`) and common error-handling logic; all ViewModels inherit from this
- [x] `ServiceBase` — base class for services; provides shared logger access and consistent error handling pattern; all services inherit from this
- [x] Theme via MahApps.Metro — defaults to Windows system theme (light/dark)
- [x] Manual theme override in settings: System / Light / Dark
- [x] Theme preference persisted in settings file
- [x] Device on/off toggle
- [x] Connection status indicator
- [x] Option to launch on Windows startup (registry run key, toggled in settings)

### Menus
- [x] Menu bar on main window: **File** (Exit) | **Help** (About)
- [x] Context menu on device list items: Connect, Remove
- [x] All menu commands bound via `[RelayCommand]` on the ViewModel — no code-behind handlers

### Dialogs
- [x] All dialogs use MahApps.Metro dialog framework (`ShowMetroDialogAsync`) for consistent styling
- [x] `IDialogService` interface — ViewModels call dialogs through this service, never reference Views directly
- [x] **Connect to Device dialog** — fields for IP address, device ID, local key; validates input before confirming
- [x] **Add / Edit Preset dialog** — name field + colour picker; used for both create and edit flows
- [x] **Confirm dialog** — generic yes/no for destructive actions (e.g. delete preset, remove device)
- [x] **About dialog** — app name, version, link to repo
- [x] `IDialogService` mocked in ViewModel tests to verify correct dialogs are shown at the right times

### Settings Persistence
- [x] All user settings persisted to a local JSON file in `%AppData%\SmartBulbControllerWPF\settings.json`
- [x] Settings include: device credentials, alert enabled/disabled, launch behavior, startup on login, schedule entries
- [x] Settings loaded on startup, saved on change

### Error Handling & Resilience
- [x] Device unreachable: show error state in UI, retry connection automatically with backoff
- [x] Device command failure: surface error to user, do not crash
- [x] Global unhandled exception handlers (DispatcherUnhandledException, AppDomain, TaskScheduler)
- [x] Network loss during schedule poll: retry on next poll interval

### Logging
- [x] Structured logging via `Microsoft.Extensions.Logging` with a rolling file sink (Serilog)
- [x] Log location: `%AppData%\SmartBulbControllerWPF\logs\`
- [x] Log key events: app start/stop, device connect/disconnect, errors
- [x] Log level configurable (default: Information)

### Project Setup
- [x] WPF project scaffolded (.NET 10)
- [x] GitHub repo created
- [x] README with setup steps including how to get Tuya local key

### CI/CD Pipeline (GitHub Actions)
**Triggers:**
- [x] Every push and PR to `main` — runs build and test jobs
- [ ] Git tag push (`v*.*.*`) — additionally runs publish and installer jobs

**Jobs:**
- [x] `build` — `dotnet restore` + `dotnet build` (no-restore); fails fast on compile errors
- [x] `test` — `dotnet test` with results reported; build fails if any test fails; runs after `build`
- [ ] `publish` — `dotnet publish` self-contained single-file executable; runs on tag push only
- [ ] `installer` — runs Inno Setup against published output; produces versioned `.exe`; runs after `publish`
- [ ] `release` — creates GitHub Release, uploads installer as release asset; runs after `installer`

**Versioning:**
- [ ] Version sourced from git tag (e.g. `v1.0.0`); written into assembly version and installer filename
- [ ] Installer filename: `SmartBulbControllerWPF-{version}-setup.exe`

**Artifact retention:**
- [ ] Installer `.exe` retained as a workflow artifact for 30 days on every tag build
- [ ] Test results retained as a workflow artifact for 7 days on every run

### Security
- [x] Device local key encrypted at rest using Windows DPAPI (`ProtectedData.Protect`) — tied to the current Windows user account, no password required
- [x] Credentials decrypted in memory only when needed for a device call; not held in plain text in ViewModel state
- [x] Logger must never write the device local key or device ID — sanitize before any log output
- [x] Settings file (`settings.json`) stores only the DPAPI-encrypted blob, never the raw key
- [ ] All outbound HTTP uses HTTPS *(no outbound HTTP in current scope)*
- **Known limitation:** Tuya LAN protocol (port 6668) is unencrypted on the local network — inherent to the protocol, documented in README

### Code Signing *(out of scope for portfolio build)*
- [ ] Both the app executable and installer `.exe` should be signed with a trusted code signing certificate
- [ ] Unsigned builds will trigger Windows SmartScreen warnings on install — acceptable for portfolio/dev use

### Installer (Inno Setup)
- [ ] Inno Setup script at `installer/setup.iss`
- [ ] Installer handles: install directory, Start Menu shortcut, desktop shortcut (optional), uninstaller
- [ ] Prerequisite detection: check if required .NET Desktop Runtime is already installed

### Team Game Alerts
- [ ] User selects a favorite NBA team from a dropdown (pre-loaded list with team primary colors)
- [ ] App fetches the team's schedule via ESPN's public API
- [ ] Background service polls schedule and triggers light change N minutes before tip-off
- [ ] Lights set to team primary color at configured brightness when alert fires
- [ ] Revert lights to previous state after a fixed duration from tip-off
- [ ] Alert can be enabled/disabled per-team or globally

### Testing
- [x] Separate `SmartBulbControllerWPF.Tests` xUnit project in the solution
- [x] **ViewModel tests** — cover all commands, property change notifications, and state transitions using Moq for injected services
- [x] **Service tests** — unit tests for schedule polling, scene transitions, audio service, color helpers
- [x] **Device communication tests** — `IDeviceService` interface enables mocking in all unit tests
- [ ] **Team alert tests** — pending team alert feature implementation
- [x] CI runs the test suite on every push (`dotnet test`) and fails the build on any test failure
- **116 tests passing** as of latest commit

---

## Optional Enhancements

- [x] Control multiple bulbs simultaneously (groups) — add/remove via context menu; fan-out in DeviceService
- [x] Schedule — turn on/off at a set time; hour/minute spinners, daily/once repeat, persisted in settings
- [x] Reactive scene driven by audio input (mic volume → brightness) — NAudio WaveInEvent, exponential smoothing
- [x] Tray icon — minimize to system tray with quick on/off menu (H.NotifyIcon.Wpf)

---

## Device Notes

- **Brand:** DAYBETTER A19 E26 RGBCW (6-pack) — Amazon ASIN B08THGPCQX
- **Protocol:** Tuya local LAN (port 6668)
- **Local key:** obtained via tuyapy or iot.tuya.com developer account (one-time)
- **TuyaNet docs:** https://github.com/ClusterM/tuyanet

---

## Status

Feature-complete for portfolio scope. All optional enhancements implemented.
Remaining open items: tag-based CI/CD (publish + installer jobs), NBA team alert feature, Inno Setup installer script.

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
- [ ] MVVM architecture — no code-behind logic; ViewModels extend `ObservableObject`, commands via `[RelayCommand]` source generator
- [ ] `ViewModelBase` — extends `ObservableObject`; adds shared properties (`IsBusy`, `ErrorMessage`) and common error-handling logic; all ViewModels inherit from this
- [ ] `ServiceBase` — base class for services; provides shared logger access and consistent error handling pattern; all services inherit from this
- [ ] Theme via MahApps.Metro — defaults to Windows system theme (light/dark)
- [ ] Manual theme override in settings: System / Light / Dark
- [ ] Theme preference persisted in settings file
- [ ] Device on/off toggle
- [ ] Connection status indicator
- [ ] Option to launch on Windows startup (registry run key, toggled in settings)

### Menus
- [ ] Menu bar on main window: **File** (Exit) | **Help** (About)
- [ ] Context menu on device list items: Connect, Remove
- [ ] All menu commands bound via `[RelayCommand]` on the ViewModel — no code-behind handlers

### Dialogs
- [ ] All dialogs use MahApps.Metro dialog framework (`ShowMetroDialogAsync`) for consistent styling
- [ ] `IDialogService` interface — ViewModels call dialogs through this service, never reference Views directly
- [ ] **Connect to Device dialog** — fields for IP address, device ID, local key; validates input before confirming
- [ ] **Add / Edit Preset dialog** — name field + colour picker; used for both create and edit flows
- [ ] **Confirm dialog** — generic yes/no for destructive actions (e.g. delete preset, remove device)
- [ ] **About dialog** — app name, version, link to repo
- [ ] `IDialogService` mocked in ViewModel tests to verify correct dialogs are shown at the right times

### Settings Persistence
- [ ] All user settings persisted to a local JSON file in `%AppData%\SmartBulbControllerWPF\settings.json`
- [ ] Settings include: device credentials, lead time, revert duration, selected team, alert enabled/disabled, launch behavior, startup on login
- [ ] Settings loaded on startup, saved on change

### Error Handling & Resilience
- [ ] Device unreachable: show error state in UI, retry connection automatically with backoff
- [ ] Device command failure: surface error to user, do not crash
- [ ] ESPN API unavailable: log warning, skip alert gracefully — do not silently fail without any record
- [ ] Network loss during schedule poll: retry on next poll interval, notify user in UI if multiple consecutive failures

### Logging
- [ ] Structured logging via `Microsoft.Extensions.Logging` with a rolling file sink (e.g. Serilog)
- [ ] Log location: `%AppData%\SmartBulbControllerWPF\logs\`
- [ ] Log key events: app start/stop, device connect/disconnect, alert fired, alert reverted, API fetch success/failure, errors
- [ ] Log level configurable (default: Information)

### Project Setup
- [x] WPF project scaffolded (.NET 10)
- [ ] GitHub repo with branch protection on `main` — PRs require passing CI before merge
- [ ] README with setup steps including how to get Tuya local key

### CI/CD Pipeline (GitHub Actions)
**Triggers:**
- [ ] Every push and PR to `main` — runs build and test jobs
- [ ] Git tag push (`v*.*.*`) — additionally runs publish and installer jobs

**Jobs:**
- [ ] `build` — `dotnet restore` + `dotnet build` (no-restore); fails fast on compile errors
- [ ] `test` — `dotnet test` with results reported; build fails if any test fails; runs after `build`
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
- [ ] Device local key encrypted at rest using Windows DPAPI (`ProtectedData.Protect`) — tied to the current Windows user account, no password required
- [ ] Credentials decrypted in memory only when needed for a device call; not held in plain text in ViewModel state
- [ ] Logger must never write the device local key or device ID — sanitize before any log output
- [ ] All outbound HTTP (ESPN API) uses HTTPS
- [ ] **Known limitation:** Tuya LAN protocol (port 6668) is unencrypted on the local network — inherent to the protocol, not fixable at the app level; document in README
- [ ] Settings file (`settings.json`) stores only the DPAPI-encrypted blob, never the raw key

### Code Signing *(out of scope for portfolio build)*
- [ ] Both the app executable and installer `.exe` should be signed with a trusted code signing certificate
- [ ] Unsigned builds will trigger Windows SmartScreen warnings on install — acceptable for portfolio/dev use
- [ ] For production: obtain an OV or EV certificate from a trusted CA (DigiCert, Sectigo, GlobalSign)
- [ ] EV certificates require a cloud HSM signing service (e.g. DigiCert KeyLocker, SSL.com eSigner) for CI compatibility — physical USB tokens cannot be used in GitHub Actions
- [ ] Signing integrated into the `installer` CI job via `signtool.exe` once a certificate is available

### Installer (Inno Setup)
- [ ] Inno Setup script at `installer/setup.iss`
- [ ] Installer handles: install directory, Start Menu shortcut, desktop shortcut (optional), uninstaller
- [ ] Prerequisite detection: check if required .NET Desktop Runtime is already installed
- [ ] If missing, download .NET installer from Microsoft at runtime (Inno Setup 6.1+ `DownloadTemporaryFile`)
- [ ] Run .NET installer silently; if reboot required, prompt user and reboot
- [ ] After reboot, installer automatically resumes and completes (via registry run key)
- [ ] Additional prereqs follow the same detect → download → install → reboot-if-needed pattern

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
- [ ] Game alert background service runs while the app is open; launch on startup (see UI/UX) keeps it active

### Testing
- [ ] Separate `SmartBulbControllerWPF.Tests` xUnit project in the solution
- [ ] **ViewModel tests** — cover all commands, property change notifications, and state transitions using Moq for injected services
- [ ] **Service tests** — unit tests for schedule polling logic, pre-game timing calculations, and light state snapshot/restore
- [ ] **Device communication tests** — integration tests for TuyaNet interactions; real device calls wrapped behind an `IDeviceService` interface so they can be mocked in unit tests and exercised against a real bulb in integration tests
- [ ] **Team alert tests** — verify correct trigger time (tip-off minus lead time), correct revert time (tip-off plus 3 hours), and correct team color lookup
- [ ] CI runs the test suite on every push (`dotnet test`) and fails the build on any test failure

---

## Optional Enhancements

- [ ] Control multiple bulbs simultaneously (groups)
- [ ] Schedule — turn on/off at a set time
- [ ] Reactive scene driven by audio input (mic volume → brightness)
- [ ] Tray icon — minimize to system tray with quick on/off menu

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

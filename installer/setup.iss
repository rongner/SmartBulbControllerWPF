; Smart Bulb Controller WPF - Inno Setup 6.3+
;
; Local build:   iscc installer\setup.iss
; CI (override): iscc /DSourceDir=publish /DOutputDir=artifacts installer\setup.iss

#define AppName     "Smart Bulb Controller"
#define AppPublisher "Michael Rongner"
#define AppVersion   GetFileVersion("..\src\SmartBulbControllerWPF\bin\Release\net10.0-windows\SmartBulbControllerWPF.exe")
#define AppExe      "SmartBulbControllerWPF.exe"
#define AppGuid     "{A3F8C2D1-1E4B-4A7F-9C3E-5B2D8F6A0E1C}"

; Allow CI to override source and output paths
#ifndef SourceDir
  #define SourceDir "..\src\SmartBulbControllerWPF\bin\Release\net10.0-windows"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

[Setup]
AppId={#AppGuid}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=SmartBulbController-Setup
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
RestartIfNeededByRun=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startup";     Description: "Launch automatically at Windows startup"; GroupDescription: "Startup:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";         Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall";          Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "SmartBulbControllerWPF"; \
    ValueData: """{app}\{#AppExe}"""; \
    Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; \
    Flags: nowait postinstall skipifsilent

[Code]

const
  DotNetVersion     = '10.0';
  DotNetDownloadUrl = 'https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.0/windowsdesktop-runtime-10.0.0-win-x64.exe';
  ResumeRegKey      = 'SOFTWARE\{#AppPublisher}\{#AppName}\Install';

// Returns true if .NET 10 Desktop Runtime is present.
function IsDotNet10Installed(): Boolean;
var
  Key, Version: String;
begin
  // Check both native and WoW6432 paths
  for Key in [
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
    'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App'
  ] do
  begin
    if RegQueryStringValue(HKLM, Key, '10.0.0', Version) then
    begin
      Result := True;
      Exit;
    end;
  end;
  Result := False;
end;

// Downloads .NET runtime with PowerShell and runs the installer.
// Returns the exit code of the .NET installer.
function DownloadAndInstallDotNet(): Integer;
var
  TmpFile, Cmd, Args: String;
  ResultCode: Integer;
begin
  TmpFile := ExpandConstant('{tmp}\dotnet10-desktop-runtime-win-x64.exe');

  // Download via PowerShell Invoke-WebRequest
  Cmd  := 'powershell.exe';
  Args := Format('-NoProfile -ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri ''%s'' -OutFile ''%s'' -UseBasicParsing"', [
    DotNetDownloadUrl, TmpFile]);

  if not Exec(Cmd, Args, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('Failed to download .NET 10 runtime. Please install it manually from https://dot.net.', mbError, MB_OK);
    Result := -1;
    Exit;
  end;

  // Run the installer silently
  Exec(TmpFile, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
  Result := ResultCode;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExitCode: Integer;
begin
  Result      := '';
  NeedsRestart := False;

  if IsDotNet10Installed() then Exit;

  if MsgBox(
    '.NET 10 Desktop Runtime is not installed and is required.' + #13#10 +
    'The installer will download and install it now (~65 MB).' + #13#10 + #13#10 +
    'Continue?', mbConfirmation, MB_YESNO) = IDNO
  then
  begin
    Result := '.NET 10 Desktop Runtime is required to continue.';
    Exit;
  end;

  ExitCode := DownloadAndInstallDotNet();

  if ExitCode = 3010 then  // Reboot required
  begin
    // Store flag so setup.exe can be re-launched after reboot
    RegWriteStringValue(HKCU, ResumeRegKey, 'PendingInstall', ExpandConstant('{src}'));
    RegWriteStringValue(HKCU, 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce',
      'SmartBulbControllerSetup', ExpandConstant('"{srcexe}"'));
    NeedsRestart := True;
  end
  else if ExitCode <> 0 then
    Result := Format('.NET 10 runtime installation failed (exit code %d).', [ExitCode]);
end;

procedure InitializeWizard();
begin
  // Clear any leftover resume flag
  RegDeleteValue(HKCU, ResumeRegKey, 'PendingInstall');
end;

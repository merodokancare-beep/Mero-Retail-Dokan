; Script generated for Mero Dokan POS & Retail Billing Setup
; Compatible with Inno Setup 6+

#define MyAppName "Mero Dokan"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Mero Dokan"
#define MyAppExeName "MeroDokan.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".mdk"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; App Identity
AppId={{9F27E9F1-A81C-4B2E-8E1E-234B5C8E9D0A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\MeroDokan
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=

; Visual & Branding
SetupIconFile=app.ico
WizardStyle=modern
WizardImageFile=SetupAssets\wizard_large.bmp
WizardSmallImageFile=SetupAssets\wizard_small.bmp
DisableWelcomePage=no

; Output settings
OutputDir=Installer_Output
OutputBaseFilename=MeroDokan_Setup_v1.0
SolidCompression=yes
Compression=lzma2/ultra64

; Permissions: allow installing for Current User or All Users
PrivilegesRequiredOverridesAllowed=dialog
PrivilegesRequired=lowest

; Automatic closing of running instances on upgrade
CloseApplications=yes
RestartApplications=no

; Uninstall settings
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} - Retail & Shop Management System

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
; Primary application executable and configuration
Source: "bin\Release\net40\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net40\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net40\QRCoder.dll"; DestDir: "{app}"; Flags: ignoreversion

; Official branding and icons
Source: "bin\Release\net40\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net40\logo.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net40\icon_512.png"; DestDir: "{app}"; Flags: ignoreversion

; Database configuration (only create default if target doesn't already have one)
Source: "bin\Release\net40\dbconfig.txt"; DestDir: "{app}"; Flags: onlyifdoesntexist

; Debug symbols
Source: "bin\Release\net40\MeroDokan.pdb"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\Database Settings"; Filename: "notepad.exe"; Parameters: """{app}\dbconfig.txt"""
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\*.log"
Type: files; Name: "{app}\*.tmp"

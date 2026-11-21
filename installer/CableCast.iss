; Installer CableCast. Si compila con build.ps1 (passa AppVersion e PublishDir).
#ifndef AppVersion
  #define AppVersion "1.1.5"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif
#define AppName "CableCast"
#define AppExe "CableCast.exe"
#define AppUninstallKey "Software\Microsoft\Windows\CurrentVersion\Uninstall\{6B1D2C0E-4F3A-4E7B-9C55-CA81E0CA57C1}_is1"

[Setup]
AppId={{6B1D2C0E-4F3A-4E7B-9C55-CA81E0CA57C1}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppName}
DefaultDirName={autopf}\{#AppName}
UsePreviousAppDir=yes
UsePreviousGroup=yes
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
SetupIconFile=..\src\CableCast\Resources\app.ico
UninstallDisplayIcon={app}\{#AppExe}
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Disinstalla {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  ExistingVersion: String;
  ExistingUninstallString: String;
  MaintenancePage: TInputOptionWizardPage;
  ReinstallCompleted: Boolean;

procedure StopAdb;
var
  Code: Integer;
  Adb: String;
begin
  Adb := ExpandConstant('{app}\tools\scrcpy\adb.exe');
  if FileExists(Adb) then
    Exec(Adb, 'kill-server', '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

function NextVersionPart(var Value: String): Integer;
var
  DotPos: Integer;
  Part: String;
begin
  DotPos := Pos('.', Value);
  if DotPos = 0 then
  begin
    Part := Value;
    Value := '';
  end
  else
  begin
    Part := Copy(Value, 1, DotPos - 1);
    Delete(Value, 1, DotPos);
  end;
  Result := StrToIntDef(Part, 0);
end;

function CompareVersions(Left, Right: String): Integer;
var
  LeftPart, RightPart: Integer;
begin
  while (Left <> '') or (Right <> '') do
  begin
    LeftPart := NextVersionPart(Left);
    RightPart := NextVersionPart(Right);
    if LeftPart > RightPart then
    begin
      Result := 1;
      Exit;
    end;
    if LeftPart < RightPart then
    begin
      Result := -1;
      Exit;
    end;
  end;
  Result := 0;
end;

function ReadInstalledValue(const ValueName: String; var Value: String): Boolean;
begin
  Result := RegQueryStringValue(HKLM64, '{#AppUninstallKey}', ValueName, Value);
end;

function InitializeSetup(): Boolean;
var
  Comparison: Integer;
begin
  ExistingVersion := '';
  ExistingUninstallString := '';
  Result := True;
  if not ReadInstalledValue('DisplayVersion', ExistingVersion) then
    Exit;

  ReadInstalledValue('UninstallString', ExistingUninstallString);
  Comparison := CompareVersions('{#AppVersion}', ExistingVersion);
  if Comparison < 0 then
  begin
    Result := MsgBox(
      'È già installata una versione più recente di CableCast (' + ExistingVersion +
      '). Vuoi continuare e installare la versione precedente ' + '{#AppVersion}' + '?',
      mbConfirmation, MB_YESNO) = IDYES;
  end;
end;

procedure InitializeWizard();
var
  Comparison: Integer;
begin
  if ExistingVersion = '' then
  begin
    WizardForm.WelcomeLabel1.Caption := 'Benvenuto nell''installazione di CableCast';
    WizardForm.WelcomeLabel2.Caption := 'Questa procedura installerà CableCast ' + '{#AppVersion}' + ' sul computer.';
    Exit;
  end;

  Comparison := CompareVersions('{#AppVersion}', ExistingVersion);
  if Comparison > 0 then
  begin
    WizardForm.WelcomeLabel1.Caption := 'Aggiornamento di CableCast';
    WizardForm.WelcomeLabel2.Caption := 'CableCast verrà aggiornato dalla versione ' + ExistingVersion +
      ' alla versione ' + '{#AppVersion}' + '. Le impostazioni e i profili personali verranno mantenuti.';
  end
  else if Comparison = 0 then
  begin
    WizardForm.WelcomeLabel1.Caption := 'Manutenzione di CableCast';
    WizardForm.WelcomeLabel2.Caption := 'La versione ' + ExistingVersion +
      ' è già installata. Scegli se riparare l''installazione o reinstallare la versione corrente.';
    MaintenancePage := CreateInputOptionPage(wpWelcome,
      'Manutenzione dell''installazione',
      'Scegli un''operazione',
      'In entrambi i casi le impostazioni e i profili personali vengono mantenuti.',
      True, False);
    MaintenancePage.Add('Ripara l''installazione mantenendo i file personali e le scorciatoie.');
    MaintenancePage.Add('Disinstalla e reinstalla la versione corrente; i dati personali restano conservati.');
    MaintenancePage.SelectedValueIndex := 0;
  end
  else
  begin
    WizardForm.WelcomeLabel1.Caption := 'Installazione di una versione precedente';
    WizardForm.WelcomeLabel2.Caption := 'Stai installando CableCast ' + '{#AppVersion}' +
      ' sopra la versione ' + ExistingVersion + '. Le impostazioni e i profili personali verranno mantenuti.';
  end;

end;

function UninstallerExecutable(const CommandLine: String): String;
var
  Value: String;
  QuotePos: Integer;
  SpacePos: Integer;
begin
  Value := Trim(CommandLine);
  Result := '';
  if Value = '' then Exit;
  if Value[1] = '"' then
  begin
    Delete(Value, 1, 1);
    QuotePos := Pos('"', Value);
    if QuotePos > 0 then
      Result := Copy(Value, 1, QuotePos - 1);
  end
  else
  begin
    SpacePos := Pos(' ', Value);
    if SpacePos > 0 then
      Result := Copy(Value, 1, SpacePos - 1)
    else
      Result := Value;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Uninstaller: String;
  ResultCode: Integer;
begin
  StopAdb;
  Result := '';
  if ExistingVersion <> '' then
  begin
    if CompareVersions('{#AppVersion}', ExistingVersion) = 0 then
    begin
      if (MaintenancePage.SelectedValueIndex = 1) and not ReinstallCompleted then
      begin
        Uninstaller := UninstallerExecutable(ExistingUninstallString);
        if (Uninstaller = '') or not FileExists(Uninstaller) then
        begin
          Result := 'Non è stato possibile trovare il programma di disinstallazione esistente.';
          Exit;
        end;
        if not Exec(Uninstaller, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '',
          SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
        begin
          Result := 'La disinstallazione della versione corrente non è riuscita; CableCast non è stata modificata.';
          Exit;
        end;
        ReinstallCompleted := True;
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then StopAdb;
end;

[UninstallRun]
Filename: "{app}\tools\scrcpy\adb.exe"; Parameters: "kill-server"; Flags: runhidden skipifdoesntexist; RunOnceId: "KillAdb"

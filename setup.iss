[Setup]
AppName=MyHippocrates
AppVersion=1.0
DefaultDirName={autopf}\MyHippocrates
DefaultGroupName=MyHippocrates
OutputDir=.\Output
OutputBaseFilename=MyHippocrates_Setup
SetupIconFile=Installer\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "Installer\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Dirs]
Name: "{app}\Backups"

[Icons]
Name: "{group}\MyHippocrates"; Filename: "{app}\MyHippocrates.exe"
Name: "{commondesktop}\MyHippocrates"; Filename: "{app}\MyHippocrates.exe"

[Code]
var
  PgPasswordPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  PgPasswordPage := CreateInputQueryPage(
    wpWelcome,
    'Настройка PostgreSQL',
    'Укажите пароль пользователя postgres',
    '');
  PgPasswordPage.Add('Пароль postgres:', True);
  PgPasswordPage.Values[0] := 'root';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = PgPasswordPage.ID then
  begin
    if PgPasswordPage.Values[0] = '' then
    begin
      MsgBox('Введите пароль postgres.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  PgPassword: String;
  PsqlPath: String;
  TempBat: String;
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    PgPassword := PgPasswordPage.Values[0];
    PsqlPath := 'C:\Program Files\PostgreSQL\18\bin\psql.exe';
    TempBat := ExpandConstant('{tmp}\hippocrates_db.bat');

    SaveStringToFile(TempBat,
      '@echo off' + #13#10 +
      'set PGPASSWORD=' + PgPassword + #13#10 +
      '"' + PsqlPath + '" -U postgres -c "CREATE DATABASE ""Hippocrates"" ENCODING=''UTF8''" 2>nul' + #13#10 +
      '"' + PsqlPath + '" -U postgres -d Hippocrates -f "' +
      ExpandConstant('{app}\db_setup.sql') + '"' + #13#10,
      False);

    Exec(ExpandConstant('{cmd}'), '/c "' + TempBat + '"',
  '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

    // Создаём appsettings.json с паролем
    SaveStringToFile(TempBat,
  '@echo off' + #13#10 +
  'chcp 65001' + #13#10 +  // ← добавить UTF-8
  'set PGPASSWORD=' + PgPassword + #13#10 +
  '"' + PsqlPath + '" -U postgres -c "CREATE DATABASE ""Hippocrates"" ENCODING=''UTF8''" 2>nul' + #13#10 +
  '"' + PsqlPath + '" -U postgres -d Hippocrates -f "' +
  ExpandConstant('{app}\db_setup.sql') + '"' + #13#10 +
  'pause' + #13#10,
  False);

    if ResultCode <> 0 then
      MsgBox('Ошибка при инициализации базы данных.' + #13#10 +
             'Запустите db_setup.sql вручную через pgAdmin.' + #13#10 +
             'Код ошибки: ' + IntToStr(ResultCode),
             mbError, MB_OK);
  end;
end;
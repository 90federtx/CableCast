# CLAUDE.md

App WPF (.NET 8, C# 12, MVVM) che pilota scrcpy per mostrare/controllare un telefono Android via USB. UI in italiano.

## Comandi (dalla radice)

- Build: `dotnet build CableCast.sln -nologo -v q`
- Test: `dotnet test CableCast.sln -nologo -v q`
- Tutto (scrcpy + build + test + publish + installer): `powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1` (`-SkipTests`, `-SkipInstaller`)
- Output: `artifacts\publish`, `artifacts\CableCast-Setup-<ver>.exe`. L'installer richiede Inno Setup 6 (ISCC).
- Il progetto non è un repo git. L'output di dotnet è in italiano.

## Struttura

- `src\CableCast\` – app. `Services\` (adb, scrcpy, log, ProfileStore, DiagnosticsBuilder, Loc, AppServices), `ViewModels\`, `Views\` (pagine + MainWindow), `Models\`, `Resources\` (Strings.resx, icone).
- `tests\CableCast.Tests\` – xunit, nessun test richiede un telefono.
- `scrcpy.lock.json` – versione, URL e SHA256 di scrcpy; `scripts\fetch-scrcpy.ps1` scarica e verifica.
- `installer\CableCast.iss` – Inno Setup.

## Convenzioni

- Il nome app è solo in `AppInfo.Name`; versioni e costanti in `AppInfo.cs`.
- Testi UI solo in `Resources\Strings.resx`: XAML `{loc:L Chiave}` (`xmlns:loc="clr-namespace:CableCast.Services"`), codice `Loc.Get/Format`. Per una nuova lingua aggiungi `Strings.xx.resx`.
- Nuova pagina: crea `XPage` (+ ViewModel nel costruttore se serve), registrala in `AppServices.GetService`, aggiungi il `NavigationViewItem` in `MainWindow.xaml` con `TargetPageType`. Le pagine senza costruttore parametrico funzionano col fallback `Activator`.
- UI con WPF-UI (tema Fluent scuro), MVVM con CommunityToolkit.Mvvm.
- Nessun crash: ogni errore va catturato, tradotto in messaggio italiano e scritto nel log.
- Impostazioni in `%APPDATA%\CableCast\settings.json`, salvate automaticamente da `ProfileStore`.

## Regole scrcpy (non negoziabili)

- Solo release ufficiale GitHub Genymobile, versione fissata in `scrcpy.lock.json`, SHA256 verificato e mostrato (About).
- Verifica le opzioni con `scrcpy --help` prima di usarle; non modificare il codice di scrcpy.
- Costruisci gli argomenti come **lista**, mai concatenando stringhe.
- Licenza e crediti in `THIRD-PARTY-NOTICES.md` e nella schermata Informazioni; la licenza Apache-2.0 è in `tools\scrcpy\LICENSE.txt`.
- Aggiornando scrcpy: cambia lock, `AppInfo.ScrcpyVersion/ScrcpySha256`, THIRD-PARTY-NOTICES.

## Privacy

La diagnostica non deve contenere il numero seriale del dispositivo (c'è un test).

## Non verificato senza telefono reale

Streaming vero, gamepad UHID/AOA, PS Remote Play, scollegamento durante l'uso, installazione/disinstallazione su macchina pulita. Inoltre: mappatura controller "come PS5" (variabili SDL_GAMECONTROLLERCONFIG, assunzione b0..b14 per i pulsanti HIDAPI) e cambio tema dal vivo. Anche il controllo aggiornamenti all'avvio (banner): non visibile finché non esiste una release più recente su GitHub; offline resta silenzioso (solo una riga nel log).

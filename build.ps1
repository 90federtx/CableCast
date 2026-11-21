# Build + test + publish (win-x64 self-contained) + installer Inno Setup.
param([switch]$SkipTests, [switch]$SkipInstaller)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$out = Join-Path $PSScriptRoot 'artifacts'
$pub = Join-Path $out 'publish'

function Step($t) { Write-Host "`n== $t ==" -ForegroundColor Cyan }
function Run($exe, $argList) { & $exe @argList; if ($LASTEXITCODE -ne 0) { throw "$exe e' fallito (codice $LASTEXITCODE)" } }

Step 'scrcpy (versione fissata, SHA256 verificato)'
& (Join-Path $PSScriptRoot 'scripts\fetch-scrcpy.ps1')
if ($LASTEXITCODE -ne 0) { throw 'fetch-scrcpy fallito' }

Step 'Build'
Run dotnet @('build', 'CableCast.sln', '-c', 'Release', '-nologo', '-v', 'q')

if (-not $SkipTests) {
    Step 'Test'
    Run dotnet @('test', 'CableCast.sln', '-c', 'Release', '--no-build', '-nologo', '-v', 'q')
}

Step 'Publish win-x64 self-contained'
if (Test-Path $pub) { Remove-Item $pub -Recurse -Force }
Run dotnet @('publish', 'src\CableCast\CableCast.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
    '-o', $pub, '-nologo', '-v', 'q')

if (-not $SkipInstaller) {
    Step 'Installer (Inno Setup)'
    $iscc = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc) { throw 'Inno Setup 6 non trovato (winget install --source winget JRSoftware.InnoSetup).' }
    $ver = ([xml](Get-Content src\CableCast\CableCast.csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    Run $iscc @("/DAppVersion=$ver", "/DPublishDir=$pub", 'installer\CableCast.iss')
    $setup = Get-ChildItem $out -Filter '*Setup*.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "`nInstaller: $($setup.FullName)" -ForegroundColor Green
}

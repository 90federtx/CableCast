# Scarica scrcpy dalla release ufficiale GitHub, verifica lo SHA256 fissato in scrcpy.lock.json
# e lo estrae (senza modifiche) in src\CableCast\tools\scrcpy.
param([switch]$Force)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$lock = Get-Content (Join-Path $root 'scrcpy.lock.json') -Raw | ConvertFrom-Json
$dest = Join-Path $root 'src\CableCast\tools\scrcpy'
$exe  = Join-Path $dest 'scrcpy.exe'

if ((Test-Path $exe) -and -not $Force) {
    Write-Host "scrcpy gia' presente in $dest (usa -Force per riscaricare)."
    exit 0
}

$tmp = Join-Path ([IO.Path]::GetTempPath()) ("cablecast-scrcpy-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $tmp | Out-Null
try {
    $zip = Join-Path $tmp 'scrcpy.zip'
    Write-Host "Scarico $($lock.url)"
    Invoke-WebRequest -Uri $lock.url -OutFile $zip -UseBasicParsing
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash
    Write-Host "SHA256 calcolato: $hash"
    Write-Host "SHA256 atteso:    $($lock.sha256)"
    if ($hash -ne $lock.sha256.ToUpperInvariant()) {
        throw "SHA256 NON corrispondente: file scartato."
    }
    $ex = Join-Path $tmp 'x'
    Expand-Archive $zip -DestinationPath $ex
    $inner = Get-ChildItem $ex -Directory | Select-Object -First 1
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    New-Item -ItemType Directory -Force $dest | Out-Null
    Copy-Item (Join-Path $inner.FullName '*') $dest -Recurse
    Write-Host "scrcpy $($lock.version) installato in $dest"
} finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

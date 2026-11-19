# Componenti di terze parti

CableCast include o usa i seguenti componenti. Grazie ai rispettivi autori.

## scrcpy (incluso, non modificato)

- Versione: **5.0.1** (release ufficiale, https://github.com/Genymobile/scrcpy/releases/tag/v5.0.1)
- Archivio: `scrcpy-win64-v5.0.1.zip`
- SHA256: `B12A2C4EE8BE317422451FC7DCF8EE20A71B5EA7EF9AD73DDD825A25316227A5`
- Autori: Genymobile e collaboratori (Romain Vimont)
- Licenza: Apache License 2.0. Testo completo in `tools\scrcpy\LICENSE.txt`.
- Include a sua volta `adb` (Android Debug Bridge, Google, Apache 2.0), SDL3 (zlib), FFmpeg (LGPL 2.1+) e libusb (LGPL 2.1+), come distribuiti dalla release ufficiale di scrcpy; le loro licenze sono quelle della release.

CableCast non modifica il codice di scrcpy: lo avvia come programma esterno.
Copyright 2018 Genymobile. Licenziato secondo la Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0); salvo diversa indicazione di legge o accordo scritto, il software è distribuito "COSÌ COM'È", senza garanzie di alcun tipo.

## WPF-UI

- https://github.com/lepoco/wpfui, licenza MIT, © lepo.co

## CommunityToolkit.Mvvm

- https://github.com/CommunityToolkit/dotnet, licenza MIT, © .NET Foundation e collaboratori

## System.Management

- Pacchetto Microsoft .NET, licenza MIT

## Inno Setup (solo per creare l'installer)

- https://jrsoftware.org/isinfo.php, © Jordan Russell e Martijn Laan

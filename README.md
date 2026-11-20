# CableCast

Mostra e controlla il tuo telefono Android sul PC tramite cavo USB. Pensato soprattutto per giocare con **PS Remote Play** usando un controller collegato al computer. Il motore è [scrcpy](https://github.com/Genymobile/scrcpy), incluso nell'installer: non devi scaricare nulla altro.

> Il nome "CableCast" è provvisorio.

## Requisiti

- Windows 11 (64 bit)
- Un telefono Android con cavo USB che trasferisce dati (non solo ricarica)

## Installazione

1. Esegui `CableCast-Setup-1.1.2.exe` e segui la procedura guidata.
2. Avvia CableCast dal menu Start.

## Preparare il telefono (una sola volta)

1. **Impostazioni → Info sul telefono** e tocca 7 volte "Numero build": si attivano le *Opzioni sviluppatore*.
2. **Impostazioni → Sistema → Opzioni sviluppatore** e attiva **Debug USB**.
3. Collega il cavo. Sul telefono compare "Consentire il debug USB?": spunta "Consenti sempre" e conferma.

L'app ha una procedura guidata in italiano che ti accompagna in questi passaggi e mostra lo stato del dispositivo (non collegato, non autorizzato, pronto…).

## Uso

1. Collega il telefono: nella schermata iniziale deve comparire "Pronto".
2. Premi **Avvia**. Per fermare, premi **Ferma** o chiudi la finestra del telefono.
3. Chiudendo la finestra dell'app, CableCast resta nell'area di notifica (tray). Per uscire davvero usa il menu dell'icona.

### Impostazioni

Risoluzione massima, FPS massimi, bitrate, audio, schermo spento, schermo sempre acceso, finestra sempre in primo piano, schermo intero, titolo della finestra. Le modifiche si salvano da sole in `%APPDATA%\CableCast\settings.json`.

### Profili

- **Bassa latenza (PS Remote Play)**: predefinito, pensato per giocare.
- **Qualità alta**: immagine migliore, più latenza.
- Puoi salvare profili personalizzati. I profili integrati non si possono cancellare.

### Controller: UHID o AOA?

- **UHID** (consigliato): il controller del PC appare al telefono come controller Bluetooth/HID. Funziona con cavo normale e non richiede permessi speciali. Scelta giusta nella maggior parte dei casi.
- **AOA**: il controller viene inoltrato tramite il protocollo accessori USB di Android. Usalo solo se UHID non viene riconosciuto dal gioco o dal telefono. Con AOA il debug USB e alcune funzioni del cavo possono comportarsi in modo diverso.

### Controller "come PS5" e mappa tasti

In Impostazioni puoi attivare «Usa il controller con il layout PS5» (PS4, Switch Pro, Xbox e altri) e cambiare l'assegnazione di ogni tasto. La mappatura avviene sul PC: il telefono non vede un vero DualSense, perché scrcpy presenta un proprio gamepad generico. Funzione non verificata con controller reali.

### Aspetto

Impostazioni → Aspetto: tema Giorno o Notte, applicato subito e ricordato.

Aggiornamenti: all'avvio, se sei online, l'app controlla se esiste una nuova release su GitHub e mostra un banner con il pulsante "Scarica". Si può disattivare in Impostazioni.

## Problemi comuni

| Sintomo | Cosa fare |
|---|---|
| "Nessun dispositivo" | Prova un altro cavo o un'altra porta USB; controlla che il telefono sia in modalità trasferimento file. |
| "Non autorizzato" | Sblocca il telefono e accetta la richiesta di debug USB. Se non compare: Opzioni sviluppatore → "Revoca autorizzazioni debug USB", poi ricollega. |
| Il gioco non vede il controller | Cambia modalità da UHID ad AOA (o viceversa) e riavvia lo streaming. |
| Immagine in ritardo | Usa il profilo "Bassa latenza", riduci risoluzione e FPS. |
| Altro | Vai in **Log** e premi **Copia diagnostica**: il testo (senza numero seriale) è utile per chiedere aiuto. |

### Xiaomi, Redmi e POCO

Su Xiaomi, Redmi e POCO la compatibilità dipende anche dalle autorizzazioni di MIUI o HyperOS:

- Attiva **Debug USB** e, nelle Opzioni sviluppatore, **Debug USB (Impostazioni sicurezza)**.
- Sblocca il telefono durante la prima autorizzazione e tocca **Consenti sempre da questo computer**.
- Imposta la connessione USB su **Trasferimento file**.
- Se il telefono resta offline, scollega il cavo, cambia porta USB e ricollega: CableCast prova automaticamente a aggiornare e riavviare adb.
- Se UHID non viene inizializzato, prova **AOA** solo con il cavo USB oppure disattiva il gamepad.

Il rapporto diagnostico contiene produttore, modello, versione Android, versione MIUI/HyperOS e modalità controller, senza includere il numero seriale.

## Compilare dai sorgenti

Servono .NET 8 SDK e, per l'installer, Inno Setup 6.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Opzioni: `-SkipTests`, `-SkipInstaller`. Lo script scarica scrcpy dalla release ufficiale GitHub (versione fissata in `scrcpy.lock.json`), ne verifica lo SHA256, compila, esegue i test, pubblica in `artifacts\publish` e crea l'installer in `artifacts\`.

## Licenza

CableCast è sotto licenza MIT (vedi `LICENSE`). scrcpy è Apache 2.0, incluso senza modifiche. Crediti e licenze di terze parti: `THIRD-PARTY-NOTICES.md` (e schermata Informazioni).

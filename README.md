# CableCast

Mostra e controlla il tuo telefono Android sul PC tramite cavo USB. Pensato soprattutto per giocare con **PS Remote Play** usando un controller collegato al computer. Il motore è [scrcpy](https://github.com/Genymobile/scrcpy), incluso nell'installer: non devi scaricare nulla altro.

> Il nome "CableCast" è provvisorio.

## Requisiti

- Windows 11 (64 bit)
- Un telefono Android con cavo USB che trasferisce dati (non solo ricarica)

## Installazione

1. Esegui `CableCast-Setup-1.1.5.exe` e segui la procedura guidata.
2. Avvia CableCast dal menu Start.

## Preparare il telefono (una sola volta)

1. **Impostazioni → Info sul telefono** e tocca 7 volte "Numero build": si attivano le *Opzioni sviluppatore*.
2. **Impostazioni → Sistema → Opzioni sviluppatore** e attiva **Debug USB**.
3. Collega il cavo. Sul telefono compare "Consentire il debug USB?": spunta "Consenti sempre" e conferma.
Non trovi la voce? Guida per ogni marca (Xiaomi/Redmi, Samsung, Pixel, OnePlus, OPPO, Huawei…): [TUTORIAL-TELEFONI.md](TUTORIAL-TELEFONI.md).

L'app ha una procedura guidata in italiano che ti accompagna in questi passaggi e mostra lo stato del dispositivo (non collegato, non autorizzato, pronto…).

## Uso

1. Collega il telefono: nella schermata iniziale deve comparire "Pronto". Se ci sono più dispositivi pronti, scegli quello da usare; cambiando dispositivo durante il mirroring, CableCast lo ferma prima di passare al nuovo telefono.
2. Premi **Avvia**. Per fermare, premi **Ferma** o chiudi la finestra del telefono.
3. Chiudendo la finestra dell'app, CableCast resta nell'area di notifica (tray). Per uscire davvero usa il menu dell'icona.

### Impostazioni

Risoluzione massima, FPS massimi, bitrate, audio, schermo spento, schermo sempre acceso, finestra sempre in primo piano, schermo intero, titolo della finestra. Le modifiche si salvano da sole in `%APPDATA%\CableCast\settings.json`.

### Profili

- **Bassa latenza (PS Remote Play)**: predefinito, pensato per giocare.
- **Qualità alta**: immagine migliore, più latenza.
- Puoi salvare profili personalizzati. I profili integrati non si possono cancellare.

### Controller: UHID o AOA?

- **UHID** (consigliato): scrcpy simula un controller HID tramite il modulo UHID del telefono; può non funzionare su alcune versioni Android o dispositivi meno recenti.
- **AOA**: usa il protocollo accessori USB. Su Windows può non funzionare durante il mirroring perché adb ha già aperto il dispositivo USB; se UHID non funziona, prova AOA sapendo di questo limite oppure disattiva il gamepad.

### Controller "come PS5" e mappa tasti

In Impostazioni puoi attivare «Usa il controller con il layout PS5» (PS4, Switch Pro, Xbox e altri) e cambiare l'assegnazione di ogni tasto. La mappatura avviene sul PC: il telefono non vede un vero DualSense, perché scrcpy presenta un proprio gamepad generico. Funzione non verificata con controller reali.

### Aspetto

Impostazioni → Aspetto: tema Giorno o Notte, applicato subito e ricordato.

Aggiornamenti: all'avvio, se sei online, l'app controlla se esiste una nuova release su GitHub. Puoi scaricare l'aggiornamento e avviare l'upgrade dall'app. Chiudendo il banner, la stessa versione non viene riproposta; la scelta è ricordata. L'upgrade mantiene impostazioni e profili in `%APPDATA%\CableCast`. La ricerca si può disattivare in Impostazioni.

I registri dettagliati di sessione (adb, scrcpy, connessioni e controller) sono salvati in `%APPDATA%\CableCast\Logs`. Le copie del registro mascherano seriali rilevati, indirizzi di rete, email e il percorso del profilo Windows; i file ruotano e vengono conservati per un periodo limitato.

## Problemi comuni

| Sintomo | Cosa fare |
|---|---|
| "Nessun dispositivo" | Prova un altro cavo o un'altra porta USB; controlla che il telefono sia in modalità trasferimento file. |
| "Non autorizzato" | Sblocca il telefono e accetta la richiesta di debug USB. Se non compare, scollega e ricollega; se necessario revoca le autorizzazioni di debug USB e riprova. |
| Il gioco non vede il controller | Cambia modalità da UHID ad AOA (o viceversa) e riavvia lo streaming. |
| Immagine in ritardo | Usa il profilo "Bassa latenza", riduci risoluzione e FPS. |
| Altro | Vai in **Log** e premi **Copia diagnostica**: il testo (senza numero seriale) è utile per chiedere aiuto. |

### Xiaomi, Redmi e POCO

Su alcuni dispositivi Xiaomi, Redmi e POCO l'invio di comandi può richiedere un'autorizzazione aggiuntiva:

- Attiva **Debug USB**. Se il telefono rifiuta i comandi e l'opzione è disponibile, prova anche **Debug USB (Impostazioni sicurezza)**; scrcpy segnala che può essere necessario riavviare il telefono.
- Sblocca il telefono durante la prima autorizzazione e tocca **Consenti sempre da questo computer**.
- Se la richiesta di autorizzazione non compare, scollega e ricollega il cavo; può essere utile revocare le autorizzazioni debug USB dal telefono e riprovare.
- Se UHID non viene inizializzato, prova **AOA** via USB o disattiva il gamepad. AOA può non funzionare durante il mirroring su Windows se adb ha già aperto il dispositivo USB.

Le indicazioni variano per modello e versione MIUI/HyperOS; CableCast non presume che un'opzione sia presente. Il rapporto diagnostico associa i metadati al dispositivo selezionato e non include il numero seriale.

## Compilare dai sorgenti

Servono .NET 8 SDK e, per l'installer, Inno Setup 6.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Opzioni: `-SkipTests`, `-SkipInstaller`. Lo script scarica scrcpy dalla release ufficiale GitHub (versione fissata in `scrcpy.lock.json`), ne verifica lo SHA256, compila, esegue i test, pubblica in `artifacts\publish` e crea l'installer in `artifacts\`.

## Licenza

CableCast è sotto licenza MIT (vedi `LICENSE`). scrcpy è Apache 2.0, incluso senza modifiche. Crediti e licenze di terze parti: `THIRD-PARTY-NOTICES.md` (e schermata Informazioni).

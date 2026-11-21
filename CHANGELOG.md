# Changelog

## 1.1.5

### Corretto

- I metadati del telefono vengono invalidati al cambio di seriale; le diagnostiche non riutilizzano produttore, modello o versioni appartenenti al dispositivo precedente.
- Le informazioni dei dispositivi vengono ritentate senza attribuire al telefono selezionato i dati di un altro dispositivo.
- L'installer non termina più indiscriminatamente ogni processo `adb.exe` del computer: ferma solo il server adb usato dall'installazione.

### Migliorato

- Con più telefoni pronti è possibile scegliere quale controllare; la preferenza USB resta quella iniziale, i comandi usano il seriale selezionato e il mirroring si ferma prima di passare a un altro telefono.
- Le notifiche di aggiornamento duplicate sono evitate durante la sessione; chiudendo il banner, la versione viene ricordata anche dopo il riavvio.
- L'aggiornamento avvia un percorso esplicitamente riconosciuto come upgrade. Impostazioni e profili restano in `%APPDATA%\CableCast`.
- A parità di versione l'installer offre riparazione o reinstallazione; un aggiornamento a una versione più recente è automatico e una versione precedente richiede conferma.
- Le diagnostiche distinguono gli errori `INJECT_EVENTS` e UHID e forniscono indicazioni Xiaomi/Redmi/POCO basate sui messaggi effettivi di scrcpy. Le note su MIUI/HyperOS sono condizionali, non presumono la presenza di opzioni.
- I log di adb, scrcpy, connessione e modalità controller vengono salvati in `%APPDATA%\CableCast\Logs`, con rotazione, retention e mascheramento di identificativi noti, indirizzi di rete, email e percorso profilo.
- Aggiornati i messaggi dell'upgrade e le istruzioni italiane.

## 1.1.4

### Corretto

- Alla chiusura l'app ferma il server adb, che prima restava attivo e bloccava `adb.exe` impedendo aggiornamento e disinstallazione.
- L'installer e il pulsante "Installa" dell'aggiornamento fermano adb prima di sostituire i file.
- Se CableCast è nascosta nell'area di notifica, riaprirla dall'icona la riporta in primo piano invece di non fare nulla.

## 1.1.3

### Novità

- Il banner di aggiornamento scarica l'installer direttamente nell'app, con barra di avanzamento e pulsante Annulla; l'installazione parte solo quando premi "Installa".
- Dimensione e SHA256 dell'installer vengono verificati quando GitHub li fornisce.
- Se il download non è possibile, si apre la pagina della release come prima.

## 1.1.2

### Corretto

- L'eliminazione del profilo personalizzato attivo ripristina immediatamente tutte le impostazioni del profilo predefinito.
- Le impostazioni ora vengono raggruppate e salvate dopo circa 600 ms, con svuotamento finale durante la chiusura.
- Gli URL degli aggiornamenti accettano solo la repository GitHub ufficiale CableCast tramite HTTPS.
- Lo stato di scrcpy viene sempre riportato a fermo durante lo smaltimento delle risorse.
- Lo stato `no permissions` è ora riconosciuto come mancata autorizzazione ADB.
- Il rilevamento dei dispositivi Xiaomi, Redmi e POCO include indicazioni specifiche per MIUI e HyperOS.
- Il processo scrcpy viene registrato prima dell'avvio per evitare stati obsoleti in caso di uscita immediata o errore.
- Le informazioni adb incomplete vengono ritentate fino al completamento, invece di essere memorizzate una sola volta.

### Migliorato

- Rilevamento automatico del produttore, modello, Android, MIUI e HyperOS tramite adb.
- Recupero automatico dopo disconnessioni temporanee e riavvio controllato del server adb.
- Diagnostica ampliata con Android, produttore, modello, MIUI/HyperOS e modalità controller.
- Gestione degli errori non gestiti con distinzione tra problemi recuperabili e critici.
- Migliore rilevamento degli errori UHID con suggerimento automatico per AOA o gamepad disattivato.
- Percorso di salvataggio delle impostazioni più resistente a chiusure rapide e modifiche rapide dei controlli.

### Sicurezza

- Nessuna vulnerabilità sfruttabile rilevata nella revisione della release.
- Validazione stricata degli host e dei percorsi degli aggiornamenti.

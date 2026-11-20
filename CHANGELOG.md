# Changelog

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

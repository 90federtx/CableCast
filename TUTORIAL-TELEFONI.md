# Attivare il Debug USB su ogni telefono

CableCast ha bisogno che sul telefono sia attivo il **Debug USB**. La voce è nascosta e cambia nome da marca a marca. Qui trovi i passaggi per i telefoni più comuni.

**Scorciatoia:** apri Impostazioni e scrivi `sviluppatore` nella barra di ricerca. Se le Opzioni sviluppatore sono già attive, compaiono subito.

## Passaggi comuni a tutti

1. **Sblocca le Opzioni sviluppatore.** Tocca 7 volte di fila la voce indicata nella tua marca (vedi sotto). Appare "Ora sei uno sviluppatore". Se richiesto, inserisci il PIN.
2. **Attiva Debug USB** dentro Opzioni sviluppatore.
3. **Collega il cavo** al PC. Il cavo deve trasferire dati, non solo caricare. Sul telefono scegli "Trasferimento file" (MTP) se te lo chiede.
4. Sul telefono compare **"Consentire il debug USB?"**. Spunta "Consenti sempre da questo computer" e premi OK.

## Xiaomi, Redmi, POCO (MIUI / HyperOS)

Vale anche per il **Redmi Note 11 Pro 5G**.

1. **Impostazioni → Informazioni sul telefono.**
2. Tocca 7 volte **"Versione MIUI"**. Su HyperOS la voce si chiama **"Versione OS"** o **"Versione HyperOS"**.
3. Torna in Impostazioni e vai su **Impostazioni aggiuntive → Opzioni sviluppatore**. Non sono nel menu principale.
4. Attiva **Debug USB**.
5. Attiva anche **Debug USB (Impostazioni di sicurezza)**. Senza questa voce lo schermo si vede, ma tastiera, mouse e controller non funzionano.
6. Attiva anche **Installa via USB**, se c'è.

Su Xiaomi le voci di sicurezza possono chiedere di **accedere con l'account Mi** e di avere una **SIM inserita**. Il telefono non permette di attivarle altrimenti. Se la voce è grigia o dà errore, è quasi sempre questo il motivo. Dopo l'attivazione puoi anche togliere la SIM.

## Samsung (One UI)

1. **Impostazioni → Informazioni sul telefono → Informazioni sul software.**
2. Tocca 7 volte **"Numero build"**.
3. Torna indietro: in fondo alle Impostazioni c'è **Opzioni sviluppatore**. Attiva **Debug USB**.

## Google Pixel, Motorola, Sony, Nothing (Android puro)

1. **Impostazioni → Informazioni sul telefono.**
2. Tocca 7 volte **"Numero build"**.
3. **Impostazioni → Sistema → Opzioni sviluppatore → Debug USB.**

## OnePlus (OxygenOS)

1. **Impostazioni → Informazioni sul dispositivo.**
2. Tocca 7 volte **"Numero build"**.
3. **Impostazioni → Sistema → Opzioni sviluppatore → Debug USB.**

## OPPO e Realme (ColorOS / Realme UI)

1. **Impostazioni → Informazioni sul dispositivo → Versione.**
2. Tocca 7 volte **"Numero build"**.
3. **Impostazioni → Impostazioni aggiuntive → Opzioni sviluppatore → Debug USB.**

## Vivo e iQOO (Funtouch / OriginOS)

1. **Impostazioni → Informazioni sul telefono → Informazioni sulla versione.**
2. Tocca 7 volte **"Numero build"**.
3. **Impostazioni → Sistema (o Altre impostazioni) → Opzioni sviluppatore → Debug USB.**

## Huawei e Honor (EMUI / Magic UI)

1. **Impostazioni → Informazioni sul telefono.**
2. Tocca 7 volte **"Numero build"**.
3. **Impostazioni → Sistema e aggiornamenti → Opzioni sviluppatore → Debug USB.**
4. Se compare la scelta della modalità USB, scegli "Trasferisci file".

## Altre marche

Il procedimento è quasi sempre lo stesso:

1. Cerca **Informazioni sul telefono** e tocca 7 volte **"Numero build"** (o "Versione software", "Versione MIUI").
2. Cerca **Opzioni sviluppatore** (spesso sotto Sistema o Impostazioni aggiuntive) e attiva **Debug USB**.

## Se non funziona

| Problema | Soluzione |
|---|---|
| Non trovo "Opzioni sviluppatore" | Non hai ancora toccato 7 volte la voce giusta. Usa la ricerca nelle Impostazioni. |
| Il telefono non compare in CableCast | Cambia cavo o porta USB. Sul telefono scegli "Trasferimento file". |
| Compare "Non autorizzato" | Sblocca il telefono e accetta "Consentire il debug USB?". Se non compare: Opzioni sviluppatore → "Revoca autorizzazioni debug USB", poi ricollega. |
| Lo schermo si vede ma i tasti non rispondono (Xiaomi) | Attiva "Debug USB (Impostazioni di sicurezza)". Servono account Mi e SIM. |
| Altro | In CableCast apri **Log** e premi **Copia diagnostica**. |

# AIDialogueService — Setup e integrazione

## File inclusi

| File | Scopo |
|---|---|
| `AIDialogueService.cs` | Core: 3 modalità API (AskNPC, GenerateFinalOptions, EvaluateInput) |
| `FreeInputController.cs` | Scene A2 input libero: STT → AI → feedback |
| `FinalSceneController.cs` | Scena finale: opzioni generate + reazione Luca |
| `EvaluationFeedbackUI.cs` | Feedback discreto sul polso del player |
| `proxy-server.js` | Proxy Node.js per proteggere la API key in produzione |

---

## STEP 1 — API key (IMPORTANTE)

**Non mettere mai la key nel codice o nel build.**

### In sviluppo (PC VR, editor)
1. Crea la cartella `Assets/StreamingAssets/`
2. Crea il file `api_key.txt` con solo la key: `sk-ant-api03-...`
3. Aggiungi `StreamingAssets/api_key.txt` al `.gitignore`

### In produzione (build distribuita)
Usa il proxy Node.js:
1. Deploya `proxy-server.js` su Railway o Render (free tier)
2. Imposta la variabile d'ambiente `API_KEY` nel servizio
3. In `AIDialogueService.cs`, cambia `apiEndpoint` con l'URL del tuo proxy:
   ```
   https://tuo-proxy.railway.app/v1/messages
   ```
4. Rimuovi gli header `x-api-key` e `anthropic-version` da `BuildRequest()`
   (il proxy li aggiunge lui)

---

## STEP 2 — Setup scena A2 con input libero

Per ogni scena che usa la meccanica 6 (input libero):

1. Aggiungi `FreeInputController` al GameObject del DialogueManager
2. Assegna: `npcData`, `dialogueUI`, `haptic`
3. Crea un bottone "Parla" nel Canvas VR con `XRSimpleInteractable`
4. Assegna il bottone al campo `talkButton`
5. Crea il `EvaluationFeedbackUI` prefab e aggancialo al polso sinistro:
   ```
   LeftHandController
   └── WristCanvas (Canvas WorldSpace, scala 0.001)
       └── EvaluationFeedbackUI (component)
   ```

**Nota Windows Speech:** `DictationRecognizer` richiede il microfono
e funziona solo su Windows. Per PC VR con SteamVR è perfetto.
Assicurati che Windows sia impostato in italiano:
`Impostazioni → Ora e lingua → Lingua → Aggiungi lingua → Italiano`

---

## STEP 3 — Setup scena finale

1. Nella scena `05_Festa`, crea un GameObject `FinalSceneController`
2. Aggiungi il component `FinalSceneController`
3. Assegna: `lucaData`, `dialogueUI`, `npcAudio`, `lucaQuestion` (AudioClip)
4. Crea 3 bottoni VR per le opzioni — assegna i `TMP_Text` ai campi `option1Text`, `option2Text`, `option3Text`
5. Su ogni bottone, aggiungi `XRSimpleInteractable` e nel listener chiama:
   - Bottone 1: `FinalSceneController.OnPlayerChose(0)`
   - Bottone 2: `FinalSceneController.OnPlayerChose(1)`
   - Bottone 3: `FinalSceneController.OnPlayerChose(2)`
6. Crea un `CanvasGroup` per il fade finale, assegnalo a `sceneFade`

---

## STEP 4 — Dove usare ogni modalità AI

| Scena | Metodo | Note |
|---|---|---|
| Cap. 2 Scena 1 (Giulia al mercato) | `AskNPC()` | Prima scena con input libero |
| Cap. 2 Scena 3 (serata bar) | `AskNPC()` | Conversazione a tre |
| Cap. 3 Scena 3 (Giulia traduttore) | `AskNPC()` | Input libero più complesso |
| Cap. 4 Scena 2 (festa hub) | `AskNPC()` | Hub libero con tutti gli NPC |
| Cap. 4 Scena finale | `GenerateFinalOptions()` | Opzioni personalizzate |
| Tutte le scene A2 | `EvaluatePlayerInput()` | Sempre in parallelo con AskNPC |

---

## Gestione latenza

Le chiamate API impiegano 1-3 secondi. Per non rompere l'immersione:

- Mostra un'animazione "Luca pensa..." (testa leggermente inclinata, occhi abbassati)
- Usa `loadingIndicator` in `FinalSceneController` per il caricamento iniziale
- Il `FreeInputController` mostra "..." nel testo NPC durante l'attesa
- In `AIDialogueService`, `maxRetries = 2` riprova automaticamente in caso di timeout

---

## Test offline (senza API key)

Per testare il flusso senza consumare crediti:

```csharp
// In AIDialogueService.cs, aggiungi un flag debug
#if UNITY_EDITOR
[SerializeField] bool useMockResponses = true;
[SerializeField] string mockNPCResponse = "Ah, capisco! Benvenuto a Bellavalle.";
#endif
```

Quando `useMockResponses = true`, le callback ricevono risposte simulate
istantaneamente senza fare chiamate HTTP.

---

## Costi stimati API

| Uso | Token/sessione | Costo stimato |
|---|---|---|
| 1 sessione completa (tutte le scene AI) | ~3.000-5.000 token | ~$0.015 |
| 100 sessioni di test | ~400.000 token | ~$1.50 |
| Build finale + beta test (1.000 sessioni) | ~4M token | ~$15 |

*Prezzi basati su claude-sonnet-4 a maggio 2025. Usa `max_tokens = 300` come impostato.*

# Bellavalle — Setup Unity passo per passo

## Dipendenze (Package Manager)

| Package | Come installare |
|---|---|
| XR Interaction Toolkit | già installato |
| Universal Render Pipeline (URP) | già installato se usi post-processing |
| TextMeshPro | già incluso in Unity |
| Newtonsoft Json | `com.unity.nuget.newtonsoft-json` — Add by name |

---

## Struttura cartelle da creare in Assets/

```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Core/          ← GameState, GameManager, EventBus, SaveSystem
│   │   ├── Data/          ← NPCData ScriptableObject
│   │   ├── Systems/       ← MoodSystem, LanguageTracker, FeedbackSystems, ColorProgression
│   │   ├── Scene/         ← DialogueManager, DialogueUI, SceneDirector
│   │   └── Characters/    ← NPCController
│   ├── Data/
│   │   └── NPCs/          ← Luca.asset, Enzo.asset, Carla.asset, ...
│   └── Prefabs/
│       ├── DialogueCanvas.prefab
│       ├── OptionButton.prefab
│       ├── WordFlash.prefab
│       └── WorldFeedbackText.prefab
└── Scenes/
    ├── 00_Tutorial
    ├── 01_Prologo
    ├── 02_Quartiere
    ├── 03_Conoscenze
    ├── 04_Guai
    └── 05_Festa
```

---

## STEP 1 — Copia gli script

Copia tutti i `.cs` nelle cartelle corrispondenti sotto `Assets/_Game/Scripts/`.
Cambia il namespace in cima se preferisci un nome diverso (cerca/sostituisci `Bellavalle`).

---

## STEP 2 — Crea i ScriptableObject NPC

1. Tasto destro in `Assets/_Game/Data/NPCs/`
2. **Create → Bellavalle → NPC Data**
3. Crea un asset per ogni NPC: `Luca.asset`, `Enzo.asset`, `Carla.asset`, `Giuseppe.asset`, `Giulia.asset`
4. Compila i campi: `npcId` (minuscolo, es. "luca"), `displayName`, `startMood`

Per aggiungere dialoghi usa i fields `chapters → trees → nodes` — tutto serializzato nell'Inspector.

---

## STEP 3 — Scene: GameObject "GameManager"

Nella scena `00_Tutorial` (o la prima che carichi):

1. Crea un GameObject vuoto → chiama `GameManager`
2. Aggiungi component: **GameManager**
3. Aggiungi component: **LanguageTracker** (assegna il Volume URP globale)
4. Il `DontDestroyOnLoad` nel codice lo rende persistente automaticamente

---

## STEP 4 — Prefab DialogueCanvas

1. Crea un Canvas → **Render Mode: World Space**
2. Dimensioni: Width 600, Height 400 (unità UI)
3. Scala: 0.002 su tutti gli assi (appare ~1.2m x 0.8m nel mondo)
4. Dentro il Canvas:
   - `NPCLine_TMP` — TextMeshPro per il testo italiano
   - `Translation_TMP` — TextMeshPro per la traduzione EN (più piccolo, grigio)
   - `OptionsContainer` — VerticalLayoutGroup, qui i bottoni vengono istanziati
5. Aggiungi component **DialogueUI** e assegna i riferimenti
6. Aggiungi component **DialogueManager** e assegna DialogueUI, AudioSource, WorldFeedback, HapticFeedback

---

## STEP 5 — Prefab OptionButton

1. Crea un GameObject UI con Image + TextMeshPro + **XRSimpleInteractable**
2. Aggiungi un BoxCollider (necessario per il ray interactor XRI)
3. Dimensioni consigliabili: Width 500, Height 60
4. Salva come prefab e assegna in DialogueUI → `optionButtonPrefab`

---

## STEP 6 — Setup NPC placeholder

Per ogni NPC placeholder esistente:

1. Aggiungi component **NPCController**
2. Assegna: `npcData` (es. Luca.asset), `sceneDirector` (SceneDirector della scena)
3. Aggiungi component **MoodSystem** → assegna `npcId` (es. "luca")
4. Aggiungi **XRSimpleInteractable** + BoxCollider (se non presenti)
5. Aggiungi **AudioSource** per le voci

---

## STEP 7 — SceneDirector per ogni scena

Per ogni conversazione narrativa (es. Bar Cap.1 Scena 1):

1. Crea un GameObject con un **SphereCollider** (trigger) → raggio 2m
2. Aggiungi component **SceneDirector**
3. Compila: `sceneId` (es. "bar_cap1_sc1"), `chapterIndex`, `npcData`, `dialogueTreeId`
4. Collega `onSceneComplete` all'evento che avanza la storia (es. apre la porta successiva)

---

## STEP 8 — Post-processing saturazione

1. Crea un **Global Volume** nella scena
2. Crea un Profile → aggiungi override **Color Adjustments**
3. Spunta `Saturation` → imposta -35 come valore iniziale
4. Crea un GameObject → aggiungi **ColorProgressionController** → assegna il Volume

---

## STEP 9 — Build Settings

Aggiungi le scene nell'ordine:
```
0: 00_Tutorial
1: 01_Prologo
2: 02_Quartiere
3: 03_Conoscenze
4: 04_Guai
5: 05_Festa
```

L'ordine deve corrispondere all'array `ChapterScenes` in `GameManager.cs`.

---

## Test rapido senza VR headset

XRI supporta la simulazione da tastiera/mouse in editor (XR Device Simulator).
Abilita **XR Device Simulator** nel package XRI e puoi testare il ray interactor
e le selezioni senza indossare il visore.

---

## Prossimi sistemi da agganciare

- `MissionManager` per la lista della spesa
- `AIDialogueService` per l'input libero A2 (chiama API Anthropic)
- `SubtitleAdaptiveController` per la logica confidence avanzata
- `DictationManager` per speech-to-text nelle scene A2


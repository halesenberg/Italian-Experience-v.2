# Meccanica 4 — Ricostruzione Frase (Giuseppe)

## File inclusi

| File | Scopo |
|---|---|
| `SentenceReconstructionData.cs` | ScriptableObject per ogni esercizio |
| `SentenceReconstructionManager.cs` | Orchestratore: spawn carte/slot, validazione, feedback |
| `FragmentCard.cs` | Carta fisica afferrabile (XRGrabInteractable) |
| `SnapSlot.cs` | Slot fisico con trigger, validazione posizione |
| `ReconstructionUI.cs` | Canvas con istruzioni, frase completa, feedback |

---

## Flusso in scena

```
Giuseppe parla → SceneDirector.onSceneComplete → Manager.StartNextExercise()
     ↓
Giuseppe legge la frase ad alta voce + UI mostra testo (3 secondi)
     ↓
Frase scompare → carte spawnate in ordine casuale davanti al player
     ↓
Player afferra carta (XRGrabInteractable) → porta su SnapSlot
     ↓
Trigger OnTriggerStay → AcceptCard() → Manager.OnCardPlacedInSlot()
     ↓
Corretto: carta snappa, pallino verde, haptic dolce
Sbagliato: carta snappa, pallino rosso, haptic forte, può riprovare
     ↓
Tutti corretti → particelle + audio + frase completa mostrata + avanza
```

---

## STEP 1 — Crea i ScriptableObject esercizi

Tasto destro → **Create → Bellavalle → Sentence Reconstruction**

Crea 3 asset per la scena di Giuseppe (Cap. 1, Scena 2):

### Ricostruzione_Giuseppe_1.asset
```
exerciseId: "giuseppe_domanda_1"
fullSentenceIT: "Come ti chiami?"
translationEN: "What is your name?"
contextHint: "Giuseppe ti sta chiedendo il tuo nome"

fragments:
  [0] textIT:"Come"    correctIndex:0
  [1] textIT:"ti"      correctIndex:1
  [2] textIT:"chiami"  correctIndex:2
  [3] textIT:"?"       correctIndex:3
```

### Ricostruzione_Giuseppe_2.asset
```
exerciseId: "giuseppe_domanda_2"
fullSentenceIT: "Da dove vieni?"
translationEN: "Where are you from?"

fragments:
  [0] textIT:"Da"      correctIndex:0
  [1] textIT:"dove"    correctIndex:1
  [2] textIT:"vieni"   correctIndex:2
  [3] textIT:"?"       correctIndex:3
```

### Ricostruzione_Giuseppe_3.asset
```
exerciseId: "giuseppe_domanda_3"
fullSentenceIT: "Quanti anni hai?"
translationEN: "How old are you?"

fragments:
  [0] textIT:"Quanti"  correctIndex:0
  [1] textIT:"anni"    correctIndex:1
  [2] textIT:"hai"     correctIndex:2
  [3] textIT:"?"       correctIndex:3
```

---

## STEP 2 — Prefab FragmentCard

Gerarchia:
```
FragmentCard (root)
├── Components: XRGrabInteractable, Rigidbody, BoxCollider
├── CardVisual (MeshRenderer — usa un quad 0.12 x 0.08 m)
│   └── Materials: Normal (bianco), Grabbed (azzurro chiaro),
│                  Correct (verde), Wrong (arancione)
└── FragmentText (TextMeshPro 3D)
    └── Font size: 0.04, allineamento centro, colore scuro
```

**Rigidbody settings:**
- Use Gravity: ✗ (le carte fluttuano)
- Interpolation: Interpolate
- Collision Detection: Continuous Dynamic
- Constraints: Freeze Rotation X, Z (ruotano solo su Y)

**XRGrabInteractable settings:**
- Movement Type: Velocity Tracking
- Throw On Detach: ✗ (non vogliamo che volino via)

---

## STEP 3 — Prefab SnapSlot

Gerarchia:
```
SnapSlot (root)
├── Components: BoxCollider (Is Trigger: ✓), SnapSlot script
├── SlotVisual (quad 0.13 x 0.09 m, materiale traslucido grigio)
├── PlaceholderText (TMP_Text 3D — mostra "___")
├── CorrectIndicator (piccola sfera verde, SetActive false di default)
└── WrongIndicator (piccola sfera arancione, SetActive false di default)
```

**BoxCollider trigger:** dimensioni leggermente più grandi della carta
(es. 0.15 x 0.11 x 0.06 m) per facilitare il posizionamento.

---

## STEP 4 — Setup scena (scale di Giuseppe)

### Posizionamento nello spazio

```
SentenceReconstructionManager (GameObject)
├── cardSpawnArea: posiziona a ~0.8m davanti al player, altezza occhi
│   (es. davanti al corrimano delle scale)
└── slotsRow: posiziona 0.2m sotto cardSpawnArea, stessa profondità
```

Configurazione consigliata:
- `cardSpread`: 0.15 (15cm tra ogni carta in spawn)
- `slotSpacing`: 0.13 (13cm tra ogni slot)
- Le carte compaiono in alto → il player le prende e le abbassa sugli slot

### GameObject nella scena
1. Crea `SentenceReconstructionManager` GameObject
2. Aggiungi component `SentenceReconstructionManager`
3. Assegna `exercisesInOrder`: trascina i 3 asset in ordine
4. Assegna `fragmentCardPrefab` e `snapSlotPrefab`
5. Crea due Empty GameObject: `CardSpawnArea` e `SlotsRow`, posizionali
6. Assegna `audioSource`, `haptic`, `ui`

---

## STEP 5 — Canvas ReconstructionUI

```
ReconstructionCanvas (Canvas WorldSpace)
├── Posizione: 0.4m sopra la slotsRow
├── Scala: 0.001 (render space)
└── Contenuto:
    ├── FullSentencePanel (attivo solo durante l'ascolto)
    │   ├── FullSentenceText (TMP — grande, centrato)
    │   └── TranslationText (TMP — piccolo, grigio sotto)
    ├── InstructionsText (TMP — istruzioni contestuali)
    └── CompletionText (TMP — "Bravo! Corretto!" con fade out)
```

---

## STEP 6 — Collegamento a SceneDirector di Giuseppe

Nel GameObject del dialogo di Giuseppe (Cap. 1, Scena 2):
1. Nel `SceneDirector`, campo `onSceneComplete`
2. Collega → `SentenceReconstructionManager.StartNextExercise()`

Oppure, se l'esercizio parte dopo che Giuseppe ha fatto le 3 domande:
```csharp
// In un DialogueNode finale di Giuseppe:
// setFlags: ["giuseppe_ha_fatto_domande"]
// In SceneDirector.onSceneComplete → Manager.StartNextExercise()
```

---

## Materiali consigliati (URP)

Crea 4 materiali Unlit o Lit semplici:

| Materiale | Colore | Alpha |
|---|---|---|
| Card_Normal | Bianco caldo #F5F0E8 | 1.0 |
| Card_Grabbed | Azzurro #B8D4F0 | 1.0 |
| Card_Correct | Verde #7DCF7D | 1.0 |
| Card_Wrong | Arancione #F0A060 | 1.0 |
| Slot_Empty | Grigio #CCCCCC | 0.3 (traslucido) |
| Slot_Occupied | Giallo chiaro #F0E880 | 0.5 |

---

## Tip: testing senza VR

Con XR Device Simulator puoi testare afferrando le carte con il mouse.
In alternativa, aggiungi un bottone di debug nell'Inspector:

```csharp
[ContextMenu("Debug: Auto-Solve")]
void DebugAutoSolve() {
    for (int i = 0; i < _slots.Count; i++) {
        var card = FindCardByFragmentIndex(i);
        if (card != null) _slots[i].ForceAcceptCard(card);
    }
}
```

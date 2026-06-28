# MissionManager — Sistema Lista della Spesa

## File inclusi

| File | Scopo |
|---|---|
| `ShoppingMissionData.cs` | ScriptableObject con items, audio Carla, difficoltà |
| `MissionManager.cs` | Orchestra le 3 fasi: ascolto lista → mercato → ritorno |
| `VendorController.cs` | NPC ambulante: riceve richiesta, consegna item, anima oggetto |
| `VendorSystems.cs` | VendorDialogueData, VendorOptionUI, PlayerInventory, ShoppingListUI |
| `ReturnTrigger.cs` | Collider che rileva il ritorno del player da Carla |

---

## Flusso completo

```
Carla parla → [audio item 1] → [audio item 2] → (lista visiva 10s se A1)
     ↓
Player va al mercato
     ↓
Ambulante frutta: "Cosa desidera?" → player sceglie "Vorrei dei pomodori"
     ↓
VendorController → MissionManager.OnPlayerRequestsItem("ambulante_frutta", "pomodori")
     ↓
Match trovato → item vola nello zaino → checkmark sulla lista
     ↓
Tutti gli item → ReturnTrigger si attiva
     ↓
Carla reagisce (tutte giuste: +mood / qualcosa manca: neutro)
```

---

## STEP 1 — Crea il ScriptableObject missione

1. Tasto destro → **Create → Bellavalle → Shopping Mission**
2. Crea `SpesaCap1.asset` (Cap. 1), `SpesaCap3.asset` (Cap. 3), `SpesaCap4.asset` (Cap. 4)

Per `SpesaCap1.asset`:
```
missionId: "spesa_cap1"
chapterIndex: 1
visualModeAvailable: ✓
visualModeThreshold: 0.65

items[0]:
  nameIT: "pomodori"
  aliases: ["pomodoro", "rossi", "al chilo"]
  vendorId: "ambulante_frutta"

items[1]:
  nameIT: "pane"
  aliases: ["una pagnotta", "del pane", "pagnotta"]
  vendorId: "ambulante_pane"
```

---

## STEP 2 — Crea i VendorDialogueData

Tasto destro → **Create → Bellavalle → Vendor Dialogue**
Crea un asset per tipo: `Vendor_Frutta.asset`, `Vendor_Pane.asset`, ecc.

Esempio `Vendor_Frutta.asset`:
```
greetingIT: "Buongiorno! Cosa vuole?"

requestOptions[0]:
  labelIT: "Vorrei dei pomodori."
  keywordIT: "pomodori"
  confirmTextIT: "Eccoli! Freschi di stamattina!"
  itemPrefab: [prefab pomodori 3D]

requestOptions[1]:
  labelIT: "Ha delle mele?"
  keywordIT: "mele"
  confirmTextIT: "Certo! Quante ne vuole?"
  itemPrefab: [prefab mele 3D]

requestOptions[2]:
  labelIT: "Niente, grazie."
  isFarewell: ✓
```

---

## STEP 3 — Setup scena Mercato

### GameObject MissionManager
1. Crea un GameObject vuoto → **MissionManager**
2. Aggiungi component `MissionManager`
3. Assegna: `missionData` (SpesaCap1.asset), `carlaTransform`, `playerInventory`, `worldFeedback`, `haptic`
4. Opzionale: assegna `listUI` per la lista visiva

### Chiamata da SceneDirector
In `SceneDirector.onSceneComplete` del dialogo di Carla, collega:
```
MissionManager → StartMission()
```

### Struttura Carla (quando legge la lista)
Il `MissionManager` usa `carlaAudio` per riprodurre i clip.
Assegna l'AudioSource di Carla al campo `carlaAudio`.

---

## STEP 4 — Setup ambulanti

Per ogni banco del mercato:

1. Aggiungi `VendorController` all'NPC ambulante
2. Assegna: `vendorId` (deve corrispondere a `ShoppingItem.vendorId`), `dialogueData`
3. Aggiungi `XRSimpleInteractable` + BoxCollider
4. Crea un prefab `VendorOptionsPanel` (Canvas WorldSpace con `VendorOptionUI`)
5. Assegna il prefab a `optionsPanelPrefab`

### Prefab VendorOptionsPanel
```
VendorOptionsPanel (Canvas WorldSpace)
└── ButtonsContainer (VerticalLayoutGroup)
    └── [bottoni istanziati runtime da VendorOptionUI]
```
Assegna `VendorOptionUI` component al root del prefab.

---

## STEP 5 — Zaino del player

1. Nel tuo XR Rig, crea un GameObject figlio: **PlayerBag**
2. Posizionalo sul dorso (es. `localPosition: 0, 0, -0.2`)
3. Aggiungi component `PlayerInventory`
4. Crea 4-5 `ItemSlot` come GameObject figli, posizionati dentro il "volume" dello zaino
5. Assegna gli slot all'array `itemSlots`
6. Aggiungi tag **"PlayerBag"** all'oggetto

---

## STEP 6 — ReturnTrigger

1. Crea un GameObject nell'area di Carla (vicino alla sua porta)
2. Aggiungi `SphereCollider` → Is Trigger: ✓, Radius: 1.5
3. Aggiungi component `ReturnTrigger`
4. Assegna `minimumItemsRequired: 1`

---

## STEP 7 — Lista visiva (ShoppingListUI)

Se vuoi la lista che svanisce dopo 10s (modalità visuale):

1. Crea un Canvas WorldSpace agganciato alla camera (o flottante nel mondo)
2. Dentro: `ItemsContainer` (VerticalLayoutGroup)
3. Prefab `ItemRow`: Image (per lo sprite) + TMP_Text (per il nome) + GameObject "Checkmark"
4. Aggiungi component `ShoppingListUI`
5. Assegna al campo `listUI` del MissionManager

Il `MissionManager` chiama `listUI.Show()` e `listUI.FadeOut()` automaticamente.

---

## Difficoltà adattiva — come funziona

| SuccessRate player | Modalità |
|---|---|
| < 65% | Visuale: Carla mostra ogni item + audio |
| ≥ 65% | Solo audio: Carla legge la lista |
| > 75% (missione successiva) | `shoppingDifficulty = 1` → lista più lunga |

La soglia è configurabile su ogni `ShoppingMissionData.visualModeThreshold`.

---

## Items per capitolo (suggeriti)

| Capitolo | Items | Difficoltà |
|---|---|---|
| Cap. 1 | pomodori, pane | 2 item, visuale attiva se A1 |
| Cap. 3 | latte, uova, formaggio | 3 item, solo audio |
| Cap. 4 (sagra) | farina, olio, sale, zucchero, basilico | 5 item, veloce |

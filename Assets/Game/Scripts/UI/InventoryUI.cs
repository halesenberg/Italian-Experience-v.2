using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Bellavalle.Core;

namespace Bellavalle.UI
{
    /// <summary>
    /// Canvas World Space per l'inventario delle frasi apprese (lo "zaino").
    /// Si apre/chiude con il tasto menu del controller.
    ///
    /// Gerarchia consigliata nel Canvas:
    ///   InventoryCanvas
    ///   ├── TabButtons (Domande / Risposte / Vocabolario)
    ///   ├── ScrollView
    ///   │    └── Content  <- qui vengono instanziate le righe (PhraseRowPrefab)
    ///   └── EmptyStateText ("Non hai ancora imparato nulla qui")
    ///
    /// Setup:
    ///  1. Metti questo script sul GameObject InventoryCanvas
    ///  2. Assegna content, phraseRowPrefab, emptyStateText
    ///  3. Collega i 3 bottoni delle tab a ShowCategory(PhraseCategory)
    ///  4. Collega ToggleInventory() al tasto menu del controller (Input System)
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] GameObject canvasRoot;     // il GameObject da attivare/disattivare

        [Header("Contenuto")]
        [SerializeField] Transform content;        // dentro la ScrollView
        [SerializeField] GameObject phraseRowPrefab; // prefab di una riga (vedi PhraseRowUI)
        [SerializeField] GameObject emptyStateText;  // testo "vuoto" se nessuna frase

        [Header("Tab attiva (per evidenziare il bottone)")]
        [SerializeField] PhraseCategory defaultCategory = PhraseCategory.Vocabolario;

        readonly List<GameObject> _spawnedRows = new();
        bool _isOpen;

        public bool IsOpen => _isOpen;
        public PhraseCategory CurrentCategory { get; private set; }

        void Start()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
            _isOpen = false;
        }

        // ── API pubblica — collega al tasto menu del controller ──────────
        public void ToggleInventory()
        {
            if (_isOpen) CloseInventory();
            else OpenInventory();
        }

        public void OpenInventory()
        {
            _isOpen = true;
            if (canvasRoot != null) canvasRoot.SetActive(true);
            ShowCategory(defaultCategory);
        }

        public void CloseInventory()
        {
            _isOpen = false;
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // ── Cambio categoria — collega ai 3 bottoni tab ─────────────────
        public void ShowCategory(PhraseCategory category)
        {
            CurrentCategory = category;
            ClearRows();

            if (GameManager.Instance == null) return;
            var phrases = GameManager.Instance.State.GetPhrasesByCategory(category);

            if (emptyStateText != null)
                emptyStateText.SetActive(phrases.Count == 0);

            foreach (var phrase in phrases)
                SpawnRow(phrase);
        }

        // Overload comodo per collegare i bottoni Unity Event con un int (0,1,2)
        public void ShowCategoryByIndex(int index)
        {
            ShowCategory((PhraseCategory)index);
        }

        // ── Spawn riga ────────────────────────────────────────────────────
        void SpawnRow(LearnedPhrase phrase)
        {
            if (phraseRowPrefab == null || content == null) return;

            var go = Instantiate(phraseRowPrefab, content);
            _spawnedRows.Add(go);

            var row = go.GetComponent<PhraseRowUI>();
            if (row != null)
                row.Setup(phrase);
        }

        void ClearRows()
        {
            foreach (var row in _spawnedRows)
                if (row != null) Destroy(row);
            _spawnedRows.Clear();
        }
    }

    /// <summary>
    /// Singola riga dell'inventario: testo IT, traduzione EN, bottone audio opzionale.
    ///
    /// Gerarchia prefab consigliata:
    ///   PhraseRow
    ///   ├── TextIT (TMP_Text)
    ///   ├── TextEN (TMP_Text, piu' piccolo/grigio)
    ///   └── PlayAudioButton (Button, opzionale - nascosto se non c'e' clip)
    /// </summary>

}
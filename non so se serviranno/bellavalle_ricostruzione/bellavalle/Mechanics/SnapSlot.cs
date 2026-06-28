using UnityEngine;
using TMPro;

namespace Bellavalle.Mechanics
{
    /// <summary>
    /// Slot fisico nella riga di ricostruzione.
    /// Rileva quando una FragmentCard entra nel suo trigger e la "agancia".
    ///
    /// Gerarchia prefab:
    ///   SnapSlot (BoxCollider trigger + questo component)
    ///   ├── SlotVisual (quad traslucido — mostra "_ _ _")
    ///   ├── IndexLabel (TMP_Text — numero slot, opzionale)
    ///   └── CorrectIndicator (GameObject — pallino verde, inizialmente disattivo)
    /// </summary>
    public class SnapSlot : MonoBehaviour
    {
        // ── Proprietà pubbliche ────────────────────────────────────────
        public int  SlotIndex  { get; private set; }
        public bool IsOccupied => _currentCard != null;
        public bool IsCorrect  { get; private set; }

        // ── Riferimenti ────────────────────────────────────────────────
        [SerializeField] Renderer      slotRenderer;
        [SerializeField] GameObject    correctIndicator;    // pallino verde
        [SerializeField] GameObject    wrongIndicator;      // pallino rosso/arancione
        [SerializeField] TMP_Text      placeholderText;     // "___" quando vuoto

        [Header("Materiali")]
        [SerializeField] Material      emptyMaterial;
        [SerializeField] Material      occupiedMaterial;
        [SerializeField] Material      correctMaterial;
        [SerializeField] Material      wrongMaterial;

        // ── Stato ──────────────────────────────────────────────────────
        FragmentCard                       _currentCard;
        SentenceReconstructionManager      _manager;

        // ── Init ───────────────────────────────────────────────────────
        public void Init(int index, SentenceReconstructionManager manager)
        {
            SlotIndex = index;
            _manager  = manager;
            SetVisualState(SlotState.Empty);
        }

        // ── Trigger Physics ────────────────────────────────────────────
        void OnTriggerEnter(Collider other)
        {
            // Accetta solo FragmentCard non già in uno slot e non grabbed
            var card = other.GetComponent<FragmentCard>();
            if (card == null)         return;
            if (card.IsInSlot)        return;
            if (card.IsGrabbed)       return;  // non snappare mentre è in mano
            if (IsOccupied)           return;  // slot già occupato

            AcceptCard(card);
        }

        void OnTriggerStay(Collider other)
        {
            // Gestisce il caso in cui la carta viene rilasciata dentro il trigger
            var card = other.GetComponent<FragmentCard>();
            if (card == null)    return;
            if (card.IsInSlot)   return;
            if (card.IsGrabbed)  return;
            if (IsOccupied)      return;

            AcceptCard(card);
        }

        // ── Accettazione carta ─────────────────────────────────────────
        void AcceptCard(FragmentCard card)
        {
            _currentCard = card;
            card.PlaceInSlot(this);

            // Nascondi placeholder
            if (placeholderText) placeholderText.gameObject.SetActive(false);

            SetVisualState(SlotState.Occupied);

            // Notifica il manager
            _manager.OnCardPlacedInSlot(SlotIndex, card.FragmentIndex, this);
        }

        public void SetCorrectState(bool correct)
        {
            IsCorrect = correct;
            SetVisualState(correct ? SlotState.Correct : SlotState.Wrong);
            _currentCard?.SetCorrectVisual(correct);

            if (correctIndicator) correctIndicator.SetActive(correct);
            if (wrongIndicator)   wrongIndicator.SetActive(!correct);
        }

        // ── Rimozione carta ────────────────────────────────────────────
        public void OnCardRemoved()
        {
            _currentCard = null;
            IsCorrect    = false;

            if (placeholderText) placeholderText.gameObject.SetActive(true);
            if (correctIndicator) correctIndicator.SetActive(false);
            if (wrongIndicator)   wrongIndicator.SetActive(false);

            SetVisualState(SlotState.Empty);
        }

        /// Forza accettazione carta (usato da ShowSolution nel manager)
        public void ForceAcceptCard(FragmentCard card)
        {
            if (IsOccupied && _currentCard != null)
            {
                _currentCard.RemoveFromSlot();
                _currentCard = null;
            }
            AcceptCard(card);
            SetCorrectState(true);
        }

        // ── Visual state ───────────────────────────────────────────────
        enum SlotState { Empty, Occupied, Correct, Wrong }

        void SetVisualState(SlotState state)
        {
            if (slotRenderer == null) return;
            slotRenderer.material = state switch
            {
                SlotState.Empty    => emptyMaterial,
                SlotState.Occupied => occupiedMaterial,
                SlotState.Correct  => correctMaterial,
                SlotState.Wrong    => wrongMaterial,
                _                  => emptyMaterial
            };
        }

        // ── Gizmo ──────────────────────────────────────────────────────
        void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied
                ? (IsCorrect ? Color.green : Color.red)
                : new Color(0.5f, 0.5f, 1f, 0.4f);

            var col = GetComponent<BoxCollider>();
            if (col != null)
                Gizmos.DrawWireCube(transform.position + col.center,
                                    Vector3.Scale(col.size, transform.lossyScale));
        }
    }
}

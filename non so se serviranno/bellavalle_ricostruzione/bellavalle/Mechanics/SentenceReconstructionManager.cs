using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Systems;

namespace Bellavalle.Mechanics
{
    /// <summary>
    /// Gestisce un singolo esercizio di ricostruzione frase.
    ///
    /// Flusso:
    ///  1. Giuseppe legge la frase ad alta voce (player ascolta)
    ///  2. Le carte compaiono sparse nello spazio davanti al player
    ///  3. Player le afferra (XRGrabInteractable) e le posiziona sugli slot
    ///  4. Ogni slot valida in real-time la carta posizionata
    ///  5. Quando tutti gli slot sono corretti → congratulazioni + avanza
    ///
    /// Attacca a un GameObject nella scena di Giuseppe (scale del palazzo).
    /// </summary>
    public class SentenceReconstructionManager : MonoBehaviour
    {
        public static SentenceReconstructionManager Instance { get; private set; }

        // ── Riferimenti ────────────────────────────────────────────────
        [Header("Dati esercizio")]
        [SerializeField] SentenceReconstructionData[] exercisesInOrder;  // sequenza per Giuseppe

        [Header("Prefab")]
        [SerializeField] GameObject fragmentCardPrefab;   // carta con testo + XRGrabInteractable
        [SerializeField] GameObject snapSlotPrefab;       // slot dove posizionare la carta

        [Header("Posizionamento")]
        [SerializeField] Transform  cardSpawnArea;        // area dove le carte appaiono sparse
        [SerializeField] Transform  slotsRow;             // riga degli slot davanti al player
        [SerializeField] float      cardSpread   = 0.15f; // distanza tra carte in spawn
        [SerializeField] float      slotSpacing  = 0.13f; // distanza tra slot

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip   allCorrectClip;
        [SerializeField] AudioClip   wrongClip;
        [SerializeField] AudioClip   cardSnapClip;

        [Header("VFX / UI")]
        [SerializeField] ParticleSystem successParticles;
        [SerializeField] HapticFeedback haptic;
        [SerializeField] ReconstructionUI ui;

        // ── Stato runtime ──────────────────────────────────────────────
        int                             _currentExerciseIndex = 0;
        SentenceReconstructionData      _currentData;
        List<FragmentCard>              _spawnedCards  = new();
        List<SnapSlot>                  _slots         = new();
        int                             _correctCount  = 0;
        int                             _attempts      = 0;
        bool                            _exerciseActive;

        // ── Lifecycle ──────────────────────────────────────────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── API pubblica ───────────────────────────────────────────────
        /// Chiamato da SceneDirector o NPCController di Giuseppe
        public void StartNextExercise()
        {
            if (_currentExerciseIndex >= exercisesInOrder.Length) return;
            StartCoroutine(RunExercise(exercisesInOrder[_currentExerciseIndex]));
        }

        // ══════════════════════════════════════════════════════════════
        // CORE — esecuzione esercizio
        // ══════════════════════════════════════════════════════════════

        IEnumerator RunExercise(SentenceReconstructionData data)
        {
            _currentData    = data;
            _exerciseActive = true;
            _correctCount   = 0;
            _attempts       = 0;

            // 1. UI: mostra hint contestuale (opzionale)
            if (!string.IsNullOrEmpty(data.contextHint))
                ui?.ShowHint(data.contextHint);

            // 2. Giuseppe legge la frase ad alta voce
            yield return StartCoroutine(PlayGiuseppeReads(data));

            // 3. Spawna slot (posizioni corrette, vuote)
            SpawnSlots(data);

            // 4. Spawna carte in ordine casuale
            SpawnCards(data);

            // 5. Attendi completamento (valutazione real-time nei callback)
            ui?.ShowInstructions("Metti le parole nell'ordine giusto!");
        }

        // ── Giuseppe legge la frase ────────────────────────────────────
        IEnumerator PlayGiuseppeReads(SentenceReconstructionData data)
        {
            ui?.ShowFullSentence(data.fullSentenceIT, data.translationEN, visible: true);

            if (audioSource != null && data.giuseppeReadsClip != null)
            {
                audioSource.clip = data.giuseppeReadsClip;
                audioSource.Play();
                yield return new WaitForSeconds(data.giuseppeReadsClip.length + 0.5f);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            // La frase scompare — il player deve ricordare l'ordine
            ui?.ShowFullSentence("", "", visible: false);
            ui?.ShowInstructions("Ora ricostruiscila!");
        }

        // ══════════════════════════════════════════════════════════════
        // SPAWN carte e slot
        // ══════════════════════════════════════════════════════════════

        void SpawnSlots(SentenceReconstructionData data)
        {
            foreach (var s in _slots)
                if (s != null) Destroy(s.gameObject);
            _slots.Clear();

            int count = data.fragments.Length;
            float totalWidth = (count - 1) * slotSpacing;
            Vector3 startPos = slotsRow.position - Vector3.right * (totalWidth / 2f);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = startPos + Vector3.right * (i * slotSpacing);
                var go      = Instantiate(snapSlotPrefab, pos, slotsRow.rotation, slotsRow);
                var slot    = go.GetComponent<SnapSlot>();
                if (slot == null) slot = go.AddComponent<SnapSlot>();

                slot.Init(i, this);
                _slots.Add(slot);
            }
        }

        void SpawnCards(SentenceReconstructionData data)
        {
            foreach (var c in _spawnedCards)
                if (c != null) Destroy(c.gameObject);
            _spawnedCards.Clear();

            // Crea lista indici in ordine casuale
            var indices = new List<int>();
            for (int i = 0; i < data.fragments.Length; i++) indices.Add(i);
            Shuffle(indices);

            for (int i = 0; i < data.fragments.Length; i++)
            {
                int fragIdx = indices[i];
                var frag    = data.fragments[fragIdx];

                // Posizione sparsa nell'area spawn
                Vector3 offset = new Vector3(
                    (i - data.fragments.Length / 2f) * cardSpread,
                    UnityEngine.Random.Range(-0.05f, 0.05f),
                    UnityEngine.Random.Range(-0.03f, 0.03f));

                Vector3 spawnPos  = cardSpawnArea.position + offset;
                var go            = Instantiate(fragmentCardPrefab, spawnPos,
                                                cardSpawnArea.rotation, cardSpawnArea);

                var card = go.GetComponent<FragmentCard>();
                if (card == null) card = go.AddComponent<FragmentCard>();

                card.Init(frag, fragIdx, this);
                _spawnedCards.Add(card);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // VALIDAZIONE — chiamata da SnapSlot quando una carta viene posata
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Chiamato da SnapSlot.OnCardPlaced().
        /// slotIndex: posizione dello slot | cardFragmentIndex: frammento della carta
        /// </summary>
        public void OnCardPlacedInSlot(int slotIndex, int cardFragmentIndex, SnapSlot slot)
        {
            if (!_exerciseActive) return;

            bool correct = cardFragmentIndex == slotIndex;
            slot.SetCorrectState(correct);

            if (correct)
            {
                haptic?.Give(true);
                if (cardSnapClip) audioSource?.PlayOneShot(cardSnapClip, 0.6f);
                _correctCount++;
                CheckAllCorrect();
            }
            else
            {
                haptic?.Give(false);
                if (wrongClip) audioSource?.PlayOneShot(wrongClip, 0.5f);
                _attempts++;
                CheckMaxAttempts();
            }
        }

        /// Chiamato da SnapSlot quando una carta viene rimossa dallo slot
        public void OnCardRemovedFromSlot(int slotIndex, bool wasCorrect)
        {
            if (wasCorrect) _correctCount = Mathf.Max(0, _correctCount - 1);
        }

        // ── Controlli fine esercizio ───────────────────────────────────
        void CheckAllCorrect()
        {
            if (_correctCount < _currentData.fragments.Length) return;

            // Tutto in ordine!
            StartCoroutine(ExerciseSuccess());
        }

        void CheckMaxAttempts()
        {
            if (_currentData.maxAttempts <= 0) return;
            if (_attempts < _currentData.maxAttempts) return;

            // Troppi errori — mostra la soluzione
            StartCoroutine(ShowSolutionAndContinue());
        }

        // ══════════════════════════════════════════════════════════════
        // FEEDBACK finale
        // ══════════════════════════════════════════════════════════════

        IEnumerator ExerciseSuccess()
        {
            _exerciseActive = false;

            // Audio + particelle
            if (allCorrectClip) audioSource?.PlayOneShot(allCorrectClip);
            successParticles?.Play();
            haptic?.Give(true);

            // Mostra la frase completa con traduzione
            ui?.ShowFullSentence(_currentData.fullSentenceIT,
                                 _currentData.translationEN, visible: true);

            // Registra risposta corretta
            GameManager.Instance.RegisterAnswer(true);
            if (!string.IsNullOrEmpty(_currentData.exerciseId))
                GameManager.Instance.LearnWord(_currentData.fullSentenceIT);

            yield return new WaitForSeconds(2.5f);

            Cleanup();
            _currentExerciseIndex++;

            // Avanza al prossimo esercizio o chiudi
            if (_currentExerciseIndex < exercisesInOrder.Length)
            {
                yield return new WaitForSeconds(1f);
                StartNextExercise();
            }
            else
            {
                EventBus.Emit(GameEvent.MissionCompleted, "ricostruzione_giuseppe");
            }
        }

        IEnumerator ShowSolutionAndContinue()
        {
            _exerciseActive = false;
            GameManager.Instance.RegisterAnswer(false);

            // Posiziona automaticamente le carte nella posizione corretta
            for (int i = 0; i < _slots.Count; i++)
            {
                var correctCard = FindCardByFragmentIndex(i);
                if (correctCard != null)
                    _slots[i].ForceAcceptCard(correctCard);
            }

            ui?.ShowFullSentence(_currentData.fullSentenceIT,
                                 _currentData.translationEN, visible: true);

            yield return new WaitForSeconds(3f);
            Cleanup();
            _currentExerciseIndex++;

            if (_currentExerciseIndex < exercisesInOrder.Length)
            {
                yield return new WaitForSeconds(1f);
                StartNextExercise();
            }
            else
            {
                EventBus.Emit(GameEvent.MissionCompleted, "ricostruzione_giuseppe");
            }
        }

        // ── Helper ─────────────────────────────────────────────────────
        FragmentCard FindCardByFragmentIndex(int fragIdx)
        {
            foreach (var card in _spawnedCards)
                if (card != null && card.FragmentIndex == fragIdx) return card;
            return null;
        }

        void Cleanup()
        {
            foreach (var c in _spawnedCards) if (c != null) Destroy(c.gameObject);
            foreach (var s in _slots)        if (s != null) Destroy(s.gameObject);
            _spawnedCards.Clear();
            _slots.Clear();
            ui?.HideAll();
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

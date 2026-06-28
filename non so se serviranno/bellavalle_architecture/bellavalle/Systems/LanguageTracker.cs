using UnityEngine;
using Bellavalle.Core;

namespace Bellavalle.Systems
{
    /// <summary>
    /// Singleton MonoBehaviour. Legge da GameState e aggiorna il post-processing
    /// (saturazione colore) in base al progresso linguistico del player.
    /// Va su un GameObject persistente nella scena principale.
    /// </summary>
    public class LanguageTracker : MonoBehaviour
    {
        public static LanguageTracker Instance { get; private set; }

        [Header("Post-processing")]
        [SerializeField] UnityEngine.Rendering.Volume postProcessVolume;
        [SerializeField] float saturationMin = -35f;  // inizio: colori spenti
        [SerializeField] float saturationMax =   0f;  // fine: colori pieni

        [Header("Feedback UI — flash breve a schermo")]
        [SerializeField] WordFlashUI wordFlashUI;  // component UI separato

        // ── Soglie livello ─────────────────────────────────────────────
        // progress 0.0-0.45 = A1, 0.45-1.0 = A2
        public bool IsA2 => GetProgress() >= 0.45f;
        public float GetProgress() =>
            GameManager.Instance != null ? GameManager.Instance.State.languageProgress : 0f;

        // ── Unity lifecycle ────────────────────────────────────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnEnable()  => EventBus.On(GameEvent.LanguageProgressChanged, OnProgressChanged);
        void OnDisable() => EventBus.Off(GameEvent.LanguageProgressChanged, OnProgressChanged);

        void Start() => ApplySaturation(GetProgress());

        // ── Event handler ──────────────────────────────────────────────
        void OnProgressChanged(object data)
        {
            float progress = (float)data;
            ApplySaturation(progress);
        }

        // ── Post-processing ────────────────────────────────────────────
        void ApplySaturation(float progress)
        {
            if (postProcessVolume == null) return;
            if (!postProcessVolume.profile.TryGet(
                out UnityEngine.Rendering.Universal.ColorAdjustments ca)) return;

            float target = Mathf.Lerp(saturationMin, saturationMax, progress);
            // Lerp morbido per evitare salti bruschi
            ca.saturation.Override(
                Mathf.Lerp(ca.saturation.value, target, Time.deltaTime * 2f));
        }

        // ── Flash parola appresa ───────────────────────────────────────
        /// Chiamato da DialogueManager dopo ogni nodo completato con un vocabularyTag.
        public void ShowWordFlash(string wordIT)
        {
            GameManager.Instance.LearnWord(wordIT);
            wordFlashUI?.Show(wordIT);
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Bellavalle.Core;

namespace Bellavalle.Systems
{
    /// <summary>
    /// Attacca a un GameObject con un Volume URP globale.
    /// Lerpa la saturazione da -35 (inizio) a 0 (fine) in base al progresso linguistico.
    /// La transizione è continua e quasi impercettibile momento per momento —
    /// il player la nota solo guardando indietro.
    ///
    /// Setup:
    ///  1. Crea un Volume GameObject globale nella scena
    ///  2. Assegna un Profile con ColorAdjustments
    ///  3. Attacca questo script e assegna il Volume
    /// </summary>
    public class ColorProgressionController : MonoBehaviour
    {
        [SerializeField] Volume postProcessVolume;

        [Header("Range saturazione")]
        [SerializeField] float satMin = -35f;   // inizio: leggermente spento
        [SerializeField] float satMax =   0f;   // fine: colori naturali

        [Header("Velocità transizione")]
        [SerializeField] float lerpSpeed = 0.8f;  // unità/sec — lento e impercettibile

        ColorAdjustments _ca;
        float _targetSaturation;

        void Start()
        {
            if (postProcessVolume == null) return;
            postProcessVolume.profile.TryGet(out _ca);

            // Inizia dal progresso salvato (non ricomincia da 0 ad ogni scena)
            float progress    = GameManager.Instance.State.languageProgress;
            _targetSaturation = Mathf.Lerp(satMin, satMax, progress);
            if (_ca != null) _ca.saturation.Override(_targetSaturation);
        }

        void OnEnable()  => EventBus.On(GameEvent.LanguageProgressChanged, OnProgress);
        void OnDisable() => EventBus.Off(GameEvent.LanguageProgressChanged, OnProgress);

        void OnProgress(object data)
        {
            _targetSaturation = Mathf.Lerp(satMin, satMax, (float)data);
        }

        void Update()
        {
            if (_ca == null) return;
            float current = _ca.saturation.value;
            if (Mathf.Abs(current - _targetSaturation) < 0.01f) return;
            _ca.saturation.Override(
                Mathf.MoveTowards(current, _targetSaturation, lerpSpeed * Time.deltaTime));
        }
    }
}

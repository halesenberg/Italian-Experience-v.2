using System.Collections;
using UnityEngine;
using TMPro;
using Bellavalle.AI;

namespace Bellavalle.UI
{
    /// <summary>
    /// Mostra il risultato della EvaluationResult in modo discreto:
    /// - Se comprensibile: piccolo flash verde con l'"encouragement" dell'AI
    /// - Se non comprensibile: mostra la frase corretta in modo gentile
    /// - NON mostra errori grammaticali in modo tecnico — solo incoraggiamento
    ///
    /// Posiziona questo Canvas World Space sul polso sinistro del player
    /// (agganciato al LeftHandController) — appare solo quando serve,
    /// svanisce dopo pochi secondi.
    /// </summary>
    public class EvaluationFeedbackUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TMP_Text    mainText;
        [SerializeField] TMP_Text    correctedText;   // mostra versione corretta se necessario

        [Header("Colori")]
        [SerializeField] Color colorOk      = new Color(0.2f, 0.75f, 0.3f);
        [SerializeField] Color colorCorrect = new Color(0.9f, 0.55f, 0.15f);  // arancione morbido

        [Header("Timing")]
        [SerializeField] float showDuration = 3.5f;

        Coroutine _current;

        void Awake()
        {
            if (canvasGroup) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show(EvaluationResult result)
        {
            if (result == null) return;
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(ShowRoutine(result));
        }

        IEnumerator ShowRoutine(EvaluationResult result)
        {
            gameObject.SetActive(true);

            // Testo principale: sempre l'incoraggiamento dell'AI
            if (mainText != null)
            {
                mainText.text  = result.encouragement ?? (result.understandable ? "Bene!" : "Riprova!");
                mainText.color = result.understandable ? colorOk : colorCorrect;
            }

            // Versione corretta (solo se necessario e in modo gentile)
            if (correctedText != null)
            {
                bool showCorrection = !result.understandable && !string.IsNullOrEmpty(result.corrected);
                correctedText.gameObject.SetActive(showCorrection);
                if (showCorrection)
                    correctedText.text = $"→ {result.corrected}";
            }

            // Fade in
            yield return Fade(0f, 1f, 0.3f);

            // Mantieni visibile
            yield return new WaitForSeconds(showDuration);

            // Fade out
            yield return Fade(1f, 0f, 0.5f);

            gameObject.SetActive(false);
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
        }
    }
}

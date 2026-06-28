using System.Collections;
using UnityEngine;
using TMPro;

namespace Bellavalle.Mechanics
{
    /// <summary>
    /// Canvas World Space posizionato sopra la riga degli slot.
    /// Mostra:
    ///  - La frase completa (durante l'ascolto, poi nascosta)
    ///  - Istruzioni ("Metti le parole nell'ordine giusto!")
    ///  - Traduzione EN (contestuale alla frase mostrata)
    ///  - Feedback di completamento
    ///
    /// Attacca a un Canvas WorldSpace nella scena di Giuseppe.
    /// </summary>
    public class ReconstructionUI : MonoBehaviour
    {
        [Header("Elementi UI")]
        [SerializeField] GameObject  fullSentencePanel;
        [SerializeField] TMP_Text    fullSentenceText;
        [SerializeField] TMP_Text    translationText;
        [SerializeField] TMP_Text    instructionsText;
        [SerializeField] TMP_Text    completionText;
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Colori")]
        [SerializeField] Color colorNormal      = Color.white;
        [SerializeField] Color colorSuccess     = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] Color colorInstruction = new Color(0.9f, 0.9f, 0.7f);

        Coroutine _hideRoutine;

        // ── API pubblica ───────────────────────────────────────────────

        /// Mostra/nasconde la frase completa con traduzione
        public void ShowFullSentence(string sentenceIT, string translationEN, bool visible)
        {
            if (fullSentencePanel)
                fullSentencePanel.SetActive(visible);

            if (fullSentenceText)
            {
                fullSentenceText.text  = sentenceIT;
                fullSentenceText.color = colorNormal;
            }

            if (translationText)
                translationText.text = string.IsNullOrEmpty(translationEN)
                    ? "" : $"({translationEN})";
        }

        /// Mostra istruzioni al player
        public void ShowInstructions(string text)
        {
            if (instructionsText == null) return;
            instructionsText.text  = text;
            instructionsText.color = colorInstruction;
            instructionsText.gameObject.SetActive(true);
        }

        /// Mostra un hint contestuale (es. "Giuseppe ti sta chiedendo il tuo nome")
        public void ShowHint(string hint)
        {
            if (instructionsText == null) return;
            instructionsText.text  = hint;
            instructionsText.color = new Color(0.7f, 0.85f, 1f);
            instructionsText.gameObject.SetActive(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideInstructions(4f));
        }

        /// Mostra messaggio di completamento (Bravo! / Corretto!)
        public void ShowCompletion(string message)
        {
            if (completionText == null) return;
            completionText.text  = message;
            completionText.color = colorSuccess;
            completionText.gameObject.SetActive(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideCompletion(2.5f));
        }

        public void HideAll()
        {
            if (fullSentencePanel) fullSentencePanel.SetActive(false);
            if (instructionsText)  instructionsText.gameObject.SetActive(false);
            if (completionText)    completionText.gameObject.SetActive(false);
        }

        // ── Coroutine ──────────────────────────────────────────────────
        IEnumerator HideInstructions(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (instructionsText) instructionsText.gameObject.SetActive(false);
        }

        IEnumerator HideCompletion(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (completionText)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / 0.5f;
                    completionText.alpha = Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
                completionText.gameObject.SetActive(false);
            }
        }
    }
}

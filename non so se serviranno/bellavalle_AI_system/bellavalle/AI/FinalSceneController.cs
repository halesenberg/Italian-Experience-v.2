using System.Collections;
using UnityEngine;
using TMPro;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Scene;
using UnityEngine.XR.Interaction.Toolkit;

namespace Bellavalle.AI
{
    /// <summary>
    /// Controller specifico per la scena finale (Ultimo Caffè).
    ///
    /// Flusso:
    ///  1. Scena carica → mostra loading discreto
    ///  2. Chiama GenerateFinalOptions() con la storia del player
    ///  3. Se AI risponde: 3 opzioni personalizzate + bottone "Dì qualcosa" (input libero bonus)
    ///  4. Se AI fallisce: 3 opzioni scriptate di fallback
    ///  5. Player sceglie → Luca risponde → fade out + credits
    /// </summary>
    public class FinalSceneController : MonoBehaviour
    {
        [Header("NPC")]
        [SerializeField] NPCData lucaData;

        [Header("UI")]
        [SerializeField] DialogueUI    dialogueUI;
        [SerializeField] TMP_Text      lucaLineText;
        [SerializeField] GameObject    loadingIndicator;  // discreto, "Luca pensa..."
        [SerializeField] TMP_Text      option1Text;
        [SerializeField] TMP_Text      option2Text;
        [SerializeField] TMP_Text      option3Text;

        [Header("Audio")]
        [SerializeField] AudioSource   npcAudio;
        [SerializeField] AudioClip     lucaQuestion;      // "Allora, torni nel tuo paese?"
        [SerializeField] AudioClip     lucaReactionWarm;  // risposta calorosa generica
        [SerializeField] AudioClip     ambienceClip;      // bar silenzioso mattina

        [Header("Transizione")]
        [SerializeField] float         fadeDelay = 3f;
        [SerializeField] CanvasGroup   sceneFade;

        // ── Opzioni fallback (se AI non disponibile) ───────────────────
        static readonly string[] FallbackOptions = new[]
        {
            "Sì, devo tornare... ma Bellavalle mi mancherà.",
            "Non lo so ancora. Mi sono trovato bene qui.",
            "Forse torno l'anno prossimo. Ho ancora molto da imparare."
        };

        // ── Lifecycle ──────────────────────────────────────────────────
        void Start()
        {
            StartCoroutine(RunFinalScene());
        }

        IEnumerator RunFinalScene()
        {
            // Ambiente
            if (ambienceClip) npcAudio.PlayOneShot(ambienceClip, 0.3f);

            // Pausa cinematica: Luca prepara il caffè in silenzio (3 secondi)
            yield return new WaitForSeconds(3f);

            // Luca fa la domanda
            if (lucaLineText) lucaLineText.text = "Allora… torni nel tuo paese?";
            if (lucaQuestion) npcAudio.PlayOneShot(lucaQuestion);

            // Mostra loading discreto mentre generiamo le opzioni
            SetLoading(true);

            // Genera opzioni personalizzate
            FinalOptions aiOptions = null;
            bool         aiDone    = false;
            bool         aiError   = false;

            AIDialogueService.Instance.GenerateFinalOptions((opts, err) =>
            {
                aiOptions = opts;
                aiError   = err;
                aiDone    = true;
            });

            // Aspetta (max 10s poi fallback)
            float elapsed = 0f;
            while (!aiDone && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetLoading(false);

            // Costruisci le 3 opzioni finali
            string opt1, opt2, opt3;

            if (!aiError && aiOptions != null)
            {
                opt1 = aiOptions.nostalgica;
                opt2 = aiOptions.entusiasta;
                opt3 = aiOptions.incerta;
                Debug.Log("[FinalScene] Opzioni AI generate con successo.");
            }
            else
            {
                opt1 = FallbackOptions[0];
                opt2 = FallbackOptions[1];
                opt3 = FallbackOptions[2];
                Debug.LogWarning("[FinalScene] Fallback opzioni scriptate.");
            }

            ShowFinalOptions(opt1, opt2, opt3);
        }

        // ── Mostra le 3 opzioni come bottoni VR ────────────────────────
        void ShowFinalOptions(string opt1, string opt2, string opt3)
        {
            // Usa DialogueUI o costruisci direttamente i bottoni
            // Qui costruiamo manualmente per avere più controllo sul layout finale

            if (option1Text) option1Text.text = opt1;
            if (option2Text) option2Text.text = opt2;
            if (option3Text) option3Text.text = opt3;

            // Assegna listener ai bottoni (assegnati nell'Inspector)
            // I tre bottoni devono avere XRSimpleInteractable configurato
        }

        // ── Chiamato dai bottoni ───────────────────────────────────────
        public void OnPlayerChose(int optionIndex)
        {
            StartCoroutine(PlayReactionAndEnd(optionIndex));
        }

        IEnumerator PlayReactionAndEnd(int optionIndex)
        {
            // Rimuovi i bottoni
            if (option1Text) option1Text.transform.parent.gameObject.SetActive(false);

            // Luca ha una reazione calorosa per tutte le opzioni
            // (nella versione avanzata: 3 clip diverse per 3 toni)
            string lucaReply = optionIndex switch
            {
                0 => "Capisco… ma sai che puoi tornare quando vuoi. Bellavalle è casa tua.",
                1 => "Bene! E il tuo italiano? È migliorato tanto, sai.",
                _ => "Hai tempo per decidere. Intanto, bevi il caffè."
            };

            if (lucaLineText) lucaLineText.text = lucaReply;
            if (lucaReactionWarm) npcAudio.PlayOneShot(lucaReactionWarm);

            // Salva la scelta finale come flag narrativo
            GameManager.Instance.SetFlag($"final_choice_{optionIndex}");
            GameManager.Instance.SaveGame();

            // Pausa + fade out verso credits
            yield return new WaitForSeconds(fadeDelay);
            yield return FadeOut();

            // Carica scena credits o menu principale
            UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
        }

        // ── Helper ─────────────────────────────────────────────────────
        void SetLoading(bool active)
        {
            if (loadingIndicator) loadingIndicator.SetActive(active);
        }

        IEnumerator FadeOut()
        {
            if (sceneFade == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.5f;
                sceneFade.alpha = t;
                yield return null;
            }
        }
    }
}

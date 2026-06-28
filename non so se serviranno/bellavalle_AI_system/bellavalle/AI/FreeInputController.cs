using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;   // Dictation API — funziona su PC VR/Windows
using TMPro;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Scene;
using Bellavalle.Systems;

namespace Bellavalle.AI
{
    /// <summary>
    /// Gestisce le scene con input libero (meccanica 6):
    ///  1. Player preme il bottone "Parla" (o trigger)
    ///  2. DictationRecognizer trascrive la voce in italiano
    ///  3. Il testo viene inviato ad AIDialogueService
    ///  4. La risposta NPC appare + viene riprodotta via TTS (opzionale)
    ///  5. EvaluationResult dà feedback discreto al player
    ///
    /// Attacca questo component allo stesso GameObject del DialogueManager
    /// nelle scene A2 che usano input libero.
    /// </summary>
    public class FreeInputController : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] NPCData           npcData;
        [SerializeField] DialogueUI        dialogueUI;       // per mostrare la risposta NPC
        [SerializeField] AudioSource       npcAudioSource;
        [SerializeField] HapticFeedback    haptic;

        [Header("UI")]
        [SerializeField] TMP_Text          transcriptText;   // mostra la trascrizione live
        [SerializeField] TMP_Text          npcResponseText;
        [SerializeField] GameObject        listenIndicator;  // icona "sto ascoltando"
        [SerializeField] EvaluationFeedbackUI evalUI;

        [Header("Bottone attiva ascolto (XRI)")]
        [SerializeField] UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable talkButton;

        // ── Stato ──────────────────────────────────────────────────────
        DictationRecognizer              _dictation;
        List<ConversationTurn>           _history = new();
        bool                             _isListening;
        bool                             _waitingResponse;

        // ── Lifecycle ──────────────────────────────────────────────────
        void Start()
        {
            if (talkButton != null)
                talkButton.selectEntered.AddListener(_ => ToggleListen());

            SetListenIndicator(false);
        }

        void OnDestroy() => StopDictation();

        // ── Controllo ascolto ──────────────────────────────────────────
        public void ToggleListen()
        {
            if (_waitingResponse) return; // in attesa di risposta AI, non accettare input

            if (_isListening) StopDictation();
            else              StartDictation();
        }

        void StartDictation()
        {
            if (_dictation == null) InitDictation();
            _dictation.Start();
            _isListening = true;
            SetListenIndicator(true);
            if (transcriptText) transcriptText.text = "...";
        }

        void StopDictation()
        {
            if (_dictation != null && _dictation.Status == SpeechSystemStatus.Running)
                _dictation.Stop();
            _isListening = false;
            SetListenIndicator(false);
        }

        // ── DictationRecognizer ────────────────────────────────────────
        void InitDictation()
        {
            _dictation = new DictationRecognizer();

            // Risultato intermedio (live mentre parla)
            _dictation.DictationHypothesis += hypothesis =>
            {
                if (transcriptText) transcriptText.text = hypothesis;
            };

            // Risultato finale (quando smette di parlare)
            _dictation.DictationResult += (text, confidence) =>
            {
                if (transcriptText) transcriptText.text = text;
                StopDictation();
                if (!string.IsNullOrWhiteSpace(text))
                    SendToAI(text);
            };

            _dictation.DictationError += (error, hresult) =>
            {
                Debug.LogWarning($"[Dictation] Errore: {error}");
                StopDictation();
                ShowFallbackOptions();
            };
        }

        // ── Invio all'AI ───────────────────────────────────────────────
        void SendToAI(string playerInput)
        {
            _waitingResponse = true;
            ShowThinkingState(true);

            // Valutazione e risposta NPC in parallelo
            StartCoroutine(SendParallel(playerInput));
        }

        IEnumerator SendParallel(string input)
        {
            // Lancia entrambe le chiamate
            EvaluationResult evalResult  = null;
            string           npcResponse = null;
            bool             evalDone    = false;
            bool             npcDone     = false;
            bool             evalError   = false;
            bool             npcError    = false;

            AIDialogueService.Instance.AskNPC(input, npcData, _history,
                (resp, err) => { npcResponse = resp; npcError = err; npcDone = true; });

            AIDialogueService.Instance.EvaluatePlayerInput(input,
                $"Conversazione con {npcData.displayName} a Bellavalle",
                (eval, err) => { evalResult = eval; evalError = err; evalDone = true; });

            // Aspetta entrambe
            yield return new WaitUntil(() => npcDone && evalDone);

            ShowThinkingState(false);
            _waitingResponse = false;

            // ── Risposta NPC ───────────────────────────────────────────
            if (!npcError && npcResponse != null)
            {
                ShowNPCResponse(npcResponse);
                _history.Add(new ConversationTurn
                {
                    playerLine = input,
                    npcLine    = npcResponse
                });

                // Registra come risposta corretta se comprensibile
                if (evalResult != null)
                    GameManager.Instance.RegisterAnswer(evalResult.understandable);
            }
            else
            {
                ShowFallbackOptions();
            }

            // ── Feedback valutazione (discreto) ───────────────────────
            if (!evalError && evalResult != null)
                evalUI?.Show(evalResult);
        }

        // ── UI helpers ─────────────────────────────────────────────────
        void ShowNPCResponse(string text)
        {
            if (npcResponseText) npcResponseText.text = text;
            haptic?.Give(true);
        }

        void ShowThinkingState(bool thinking)
        {
            // Mostra "..." animato mentre aspetti la risposta AI
            if (npcResponseText) npcResponseText.text = thinking ? "..." : "";
        }

        void ShowFallbackOptions()
        {
            // Se l'AI fallisce, torna a un nodo di dialogo scriptato di fallback
            Debug.LogWarning("[FreeInput] AI non disponibile, mostro opzioni fallback.");
            // Implementa secondo la tua struttura di fallback
        }

        void SetListenIndicator(bool active)
        {
            if (listenIndicator) listenIndicator.SetActive(active);
        }
    }
}

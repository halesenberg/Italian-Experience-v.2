using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using Bellavalle.Core;
using Bellavalle.Data;

namespace Bellavalle.Scene
{
    /// <summary>
    /// Canvas World Space posizionato davanti al player.
    /// Le opzioni sono XRSimpleInteractable: il player punta con il ray e preme trigger.
    ///
    /// Gerarchia prefab consigliata:
    ///   DialogueCanvas (Canvas WorldSpace)
    ///   ├── NPCLine_TMP          ← testo NPC in italiano
    ///   ├── Translation_TMP      ← traduzione EN (opzionale, adattiva)
    ///   └── OptionsContainer     ← qui vengono istanziati i bottoni
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] Canvas    dialogueCanvas;
        [SerializeField] Transform optionsContainer;
        [SerializeField] TMP_Text  npcLineText;
        [SerializeField] TMP_Text  translationText;   // traduzione EN adattiva
        [SerializeField] GameObject optionButtonPrefab;

        [Header("Posizionamento")]
        [SerializeField] Transform playerCamera;      // Main Camera del rig
        [SerializeField] float     distanceFromPlayer = 1.5f;
        [SerializeField] float     heightOffset       = 0.1f;

        // ── Stato ──────────────────────────────────────────────────────
        readonly List<GameObject> _spawnedButtons = new();
        Action<int>  _onOptionChosen;
        Action       _onHelpChosen;

        // ── API pubblica ───────────────────────────────────────────────
        public void ShowOptions(DialogueNode node,
                                Action<int> onOption,
                                Action      onHelp)
        {
            _onOptionChosen = onOption;
            _onHelpChosen   = onHelp;

            PositionCanvas();
            ClearButtons();

            // Testo NPC
            if (npcLineText != null)
                npcLineText.text = node.npcLine_IT;

            // Traduzione adattiva EN
            bool showEN = ShouldShowTranslation();
            if (translationText != null)
                translationText.gameObject.SetActive(showEN);

            // Crea bottoni opzioni
            for (int i = 0; i < node.options.Length; i++)
            {
                var opt = node.options[i];
                SpawnOptionButton(opt, i);
            }

            dialogueCanvas.gameObject.SetActive(true);
        }

        public void HideOptions()
        {
            ClearButtons();
            dialogueCanvas.gameObject.SetActive(false);
        }

        // ── Posizionamento canvas ──────────────────────────────────────
        void PositionCanvas()
        {
            if (playerCamera == null) return;
            Vector3 forward = playerCamera.forward;
            forward.y = 0f;
            forward.Normalize();

            Transform t = dialogueCanvas.transform;
            t.position = playerCamera.position
                         + forward * distanceFromPlayer
                         + Vector3.up * heightOffset;

            // Il canvas guarda sempre verso il player
            t.LookAt(playerCamera.position);
            t.Rotate(0f, 180f, 0f);
        }

        // ── Bottoni ────────────────────────────────────────────────────
        void SpawnOptionButton(PlayerOption option, int index)
        {
            var go = Instantiate(optionButtonPrefab, optionsContainer);
            _spawnedButtons.Add(go);

            // Testo
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = option.text_IT;

            // Stile visivo per "Non ho capito"
            if (option.isHelpOption)
                StyleAsHelpButton(go);

            // Interazione XRI
            var interactable = go.GetComponent<XRSimpleInteractable>();
            if (interactable == null) interactable = go.AddComponent<XRSimpleInteractable>();

            int capturedIndex = index;
            bool capturedHelp = option.isHelpOption;

            interactable.selectEntered.AddListener(_ =>
            {
                if (capturedHelp) _onHelpChosen?.Invoke();
                else              _onOptionChosen?.Invoke(capturedIndex);
            });
        }

        void StyleAsHelpButton(GameObject go)
        {
            // Rendi il bottone help visivamente più piccolo/discreto
            var rect = go.GetComponent<RectTransform>();
            if (rect != null) rect.localScale = Vector3.one * 0.8f;

            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color    = new Color(0.5f, 0.5f, 0.5f);
                label.fontSize = label.fontSize * 0.85f;
            }
        }

        void ClearButtons()
        {
            foreach (var b in _spawnedButtons)
                if (b != null) Destroy(b);
            _spawnedButtons.Clear();
        }

        // ── Subtitling adattivo ────────────────────────────────────────
        bool ShouldShowTranslation()
        {
            var state = GameManager.Instance.State;
            // Mostra EN se confidence bassa O il player ha attivato il toggle manuale
            return state.confidenceScore < 0.5f || state.subtitlesEN;
        }
    }
}

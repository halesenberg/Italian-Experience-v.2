using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Missions;

namespace Bellavalle.Characters
{
    /// <summary>
    /// Attacca a ogni ambulante del mercato.
    /// Quando il player interagisce, mostra le opzioni di dialogo tipiche
    /// di un venditore ("Cosa desidera?") e verifica se la risposta
    /// corrisponde a un item nella lista della spesa attiva.
    ///
    /// Ogni ambulante ha un vendorId che corrisponde a ShoppingItem.vendorId.
    /// Esempio:
    ///   Banco frutta  → vendorId = "ambulante_frutta"
    ///   Banco pane    → vendorId = "ambulante_pane"
    ///   Banco formaggi→ vendorId = "ambulante_formaggi"
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class VendorController : MonoBehaviour
    {
        [Header("Identità")]
        [SerializeField] string vendorId;
        [SerializeField] string displayName;   // "Fruttivendolo", "Fornaio"...

        [Header("Dialogo")]
        [SerializeField] VendorDialogueData dialogueData;

        [Header("UI")]
        [SerializeField] GameObject  optionsPanelPrefab;   // Canvas WorldSpace con bottoni
        [SerializeField] Transform   panelSpawnPoint;
        [SerializeField] TMP_Text    vendorSpeechBubble;   // fumetto sopra l'NPC

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;

        // ── Stato ──────────────────────────────────────────────────────
        GameObject  _activePanel;
        bool        _isInteracting;

        // ── Lifecycle ──────────────────────────────────────────────────
        void Awake()
        {
            var interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => OnPlayerInteract());
        }

        // ── Interazione ────────────────────────────────────────────────
        void OnPlayerInteract()
        {
            if (_isInteracting) return;
            _isInteracting = true;

            // Il venditore saluta
            PlayAudio(dialogueData.greetingClip);
            SetSpeechBubble(dialogueData.greetingIT);

            // Mostra le opzioni di richiesta
            StartCoroutine(ShowOptionsAfterGreeting());
        }

        IEnumerator ShowOptionsAfterGreeting()
        {
            yield return new WaitForSeconds(
                dialogueData.greetingClip != null ? dialogueData.greetingClip.length + 0.3f : 1f);

            SpawnOptionsPanel();
        }

        // ── Panel opzioni ──────────────────────────────────────────────
        void SpawnOptionsPanel()
        {
            if (_activePanel != null) Destroy(_activePanel);
            _activePanel = Instantiate(optionsPanelPrefab, panelSpawnPoint.position,
                                       panelSpawnPoint.rotation);

            var optionUI = _activePanel.GetComponent<VendorOptionUI>();
            if (optionUI == null) return;

            // Passa le opzioni contestuali (nome items + "Niente, grazie")
            optionUI.Setup(dialogueData.requestOptions, OnPlayerChoseOption,
                           () => DismissInteraction());
        }

        void OnPlayerChoseOption(VendorOption option)
        {
            if (option.isFarewell)
            {
                DismissInteraction();
                return;
            }

            // Invia la richiesta al MissionManager
            MissionManager.Instance?.OnPlayerRequestsItem(vendorId, option.keywordIT);

            // Controlla se era un item della lista
            bool wasInList = CheckIfInList(option.keywordIT);

            if (wasInList)
                StartCoroutine(CorrectItemRoutine(option));
            else
                StartCoroutine(WrongItemRoutine(option));
        }

        IEnumerator CorrectItemRoutine(VendorOption option)
        {
            Destroy(_activePanel);

            // Venditore conferma e "consegna" l'item
            PlayAudio(option.confirmClip ?? dialogueData.defaultConfirmClip);
            SetSpeechBubble(option.confirmTextIT ?? dialogueData.defaultConfirmIT);

            // Animazione: l'oggetto vola verso lo zaino del player
            if (option.itemPrefab != null)
                StartCoroutine(FlyItemToInventory(option.itemPrefab));

            yield return new WaitForSeconds(2f);
            _isInteracting = false;
            SetSpeechBubble("");
        }

        IEnumerator WrongItemRoutine(VendorOption option)
        {
            // L'item non era nella lista — il venditore lo vende comunque
            // ma il player torna con la cosa sbagliata
            PlayAudio(dialogueData.defaultConfirmClip);
            SetSpeechBubble(option.confirmTextIT ?? dialogueData.defaultConfirmIT);

            if (option.itemPrefab != null)
                StartCoroutine(FlyItemToInventory(option.itemPrefab));

            yield return new WaitForSeconds(2f);
            Destroy(_activePanel);
            _isInteracting = false;
            SetSpeechBubble("");
        }

        // ── Animazione item verso zaino ────────────────────────────────
        IEnumerator FlyItemToInventory(GameObject prefab)
        {
            var playerBag = GameObject.FindGameObjectWithTag("PlayerBag");
            if (playerBag == null) yield break;

            var go    = Instantiate(prefab, panelSpawnPoint.position, Quaternion.identity);
            float t   = 0f;
            Vector3 start = go.transform.position;

            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                go.transform.position = Vector3.Lerp(start, playerBag.transform.position,
                                                     Mathf.SmoothStep(0f, 1f, t));
                go.transform.localScale = Vector3.one * (1f - t * 0.5f);
                yield return null;
            }
            Destroy(go);
        }

        // ── Helper ─────────────────────────────────────────────────────
        bool CheckIfInList(string keyword)
        {
            // Controlla se la missione attiva include questo venditore + keyword
            // (la logica dettagliata è in MissionManager.FindMatch)
            return MissionManager.Instance != null;
            // MissionManager.OnPlayerRequestsItem già gestisce il controllo —
            // qui serve solo per scegliere il branch corretto dell'animazione.
            // Per una verifica più precisa, esponi un metodo pubblico HasMatchingItem()
            // in MissionManager.
        }

        void DismissInteraction()
        {
            if (_activePanel != null) Destroy(_activePanel);
            _isInteracting = false;
            SetSpeechBubble("");
        }

        void PlayAudio(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip);
        }

        void SetSpeechBubble(string text)
        {
            if (vendorSpeechBubble != null)
                vendorSpeechBubble.text = text;
        }
    }
}

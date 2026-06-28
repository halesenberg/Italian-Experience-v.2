using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using Bellavalle.Data;

namespace Bellavalle.Missions
{
    // ══════════════════════════════════════════════════════════════════
    // VendorDialogueData — ScriptableObject per ogni tipo di ambulante
    // ══════════════════════════════════════════════════════════════════

    [CreateAssetMenu(menuName = "Bellavalle/Vendor Dialogue", fileName = "Vendor_")]
    public class VendorDialogueData : ScriptableObject
    {
        [Header("Saluto")]
        public string    greetingIT   = "Buongiorno! Cosa desidera?";
        public AudioClip greetingClip;

        [Header("Risposta generica")]
        public string    defaultConfirmIT  = "Eccolo! Prego!";
        public AudioClip defaultConfirmClip;

        [Header("Opzioni che questo venditore può vendere")]
        public VendorOption[] requestOptions;
    }

    [Serializable]
    public class VendorOption
    {
        public string    labelIT;         // testo del bottone: "Vorrei dei pomodori"
        public string    keywordIT;       // parola chiave per il match: "pomodori"
        public string    confirmTextIT;   // risposta specifica del venditore
        public AudioClip confirmClip;
        public GameObject itemPrefab;     // oggetto 3D che "vola" verso lo zaino
        public bool      isFarewell;      // se true = "Niente, grazie" → chiude
    }

    // ══════════════════════════════════════════════════════════════════
    // VendorOptionUI — panel VR con i bottoni per scegliere cosa comprare
    // ══════════════════════════════════════════════════════════════════

    public class VendorOptionUI : MonoBehaviour
    {
        [SerializeField] Transform  buttonsContainer;
        [SerializeField] GameObject buttonPrefab;

        public void Setup(VendorOption[] options,
                          Action<VendorOption> onChose,
                          Action onDismiss)
        {
            foreach (Transform child in buttonsContainer)
                Destroy(child.gameObject);

            foreach (var opt in options)
            {
                var go          = Instantiate(buttonPrefab, buttonsContainer);
                var label       = go.GetComponentInChildren<TMP_Text>();
                if (label) label.text = opt.labelIT;

                var interactable = go.GetComponent<XRSimpleInteractable>()
                                   ?? go.AddComponent<XRSimpleInteractable>();

                VendorOption captured = opt;
                interactable.selectEntered.AddListener(_ =>
                {
                    if (captured.isFarewell) onDismiss?.Invoke();
                    else                     onChose?.Invoke(captured);
                });
            }

            // Aggiungi sempre "Niente, grazie" in fondo se non presente
            bool hasFarewell = false;
            foreach (var o in options) if (o.isFarewell) { hasFarewell = true; break; }
            if (!hasFarewell) AddFarewellButton(onDismiss);
        }

        void AddFarewellButton(Action onDismiss)
        {
            var go    = Instantiate(buttonPrefab, buttonsContainer);
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label)
            {
                label.text  = "Niente, grazie.";
                label.color = new Color(0.5f, 0.5f, 0.5f);
            }
            var interactable = go.GetComponent<XRSimpleInteractable>()
                               ?? go.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => onDismiss?.Invoke());
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PlayerInventory — zaino fisico sul dorso del player
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attacca a un GameObject sul dorso del player (figlio del XR Rig).
    /// Tag: "PlayerBag"
    ///
    /// Gli item collezionati vengono spawnati come figli di questo transform,
    /// visibili fisicamente nello zaino aperto.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Spawn point degli item dentro lo zaino")]
        [SerializeField] Transform[] itemSlots;   // posizioni predefinite dentro lo zaino

        [Header("UI riepilogo (opzionale)")]
        [SerializeField] ShoppingListUI listUI;

        readonly List<ShoppingItem>   _collected = new();
        readonly List<GameObject>     _spawnedObjects = new();

        void Awake() => gameObject.tag = "PlayerBag";

        public void AddItem(ShoppingItem item)
        {
            if (_collected.Contains(item)) return;
            _collected.Add(item);

            // Spawna l'oggetto 3D in uno slot libero dello zaino
            int slot = _collected.Count - 1;
            if (item.itemPrefab != null && slot < itemSlots.Length)
            {
                var go = Instantiate(item.itemPrefab,
                                     itemSlots[slot].position,
                                     itemSlots[slot].rotation,
                                     itemSlots[slot]);
                _spawnedObjects.Add(go);
            }

            listUI?.MarkCollected(item);
        }

        public bool HasItem(string nameIT) =>
            _collected.Exists(i => i.nameIT == nameIT);

        public List<ShoppingItem> GetCollected() => _collected;

        public void Clear()
        {
            _collected.Clear();
            foreach (var go in _spawnedObjects)
                if (go != null) Destroy(go);
            _spawnedObjects.Clear();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // ShoppingListUI — lista visiva che appare per 10 secondi
    // poi svanisce (modalità visuale adattiva)
    // ══════════════════════════════════════════════════════════════════

    public class ShoppingListUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup     canvasGroup;
        [SerializeField] Transform       itemsContainer;
        [SerializeField] GameObject      itemRowPrefab;   // prefab: Sprite + TMP_Text
        [SerializeField] float           fadeOutDuration = 1f;

        readonly Dictionary<ShoppingItem, GameObject> _rows = new();

        public void Show(List<ShoppingItem> items)
        {
            foreach (Transform child in itemsContainer) Destroy(child.gameObject);
            _rows.Clear();

            foreach (var item in items)
            {
                var row   = Instantiate(itemRowPrefab, itemsContainer);
                var texts = row.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 0) texts[0].text = item.nameIT;

                var img = row.GetComponentInChildren<UnityEngine.UI.Image>();
                if (img != null && item.itemSprite != null) img.sprite = item.itemSprite;

                _rows[item] = row;
            }

            if (canvasGroup) canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
        }

        /// Mostra brevemente l'immagine di un singolo item (modalità visuale per item)
        public void ShowItemBriefly(ShoppingItem item, float duration)
        {
            StartCoroutine(BriefShow(item, duration));
        }

        IEnumerator BriefShow(ShoppingItem item, float dur)
        {
            // Crea una card temporanea nel mondo vicino a Carla
            // (implementazione specifica alla scena — qui placeholder)
            yield return new WaitForSeconds(dur);
        }

        public void MarkCollected(ShoppingItem item)
        {
            if (!_rows.TryGetValue(item, out var row)) return;
            var texts = row.GetComponentsInChildren<TMP_Text>();
            foreach (var t in texts)
            {
                t.fontStyle = FontStyles.Strikethrough;
                t.color     = new Color(0.4f, 0.4f, 0.4f);
            }
            // Aggiungi checkmark
            var checkmark = row.transform.Find("Checkmark");
            if (checkmark) checkmark.gameObject.SetActive(true);
        }

        public void FadeOut()
        {
            StartCoroutine(FadeOutRoutine());
        }

        IEnumerator FadeOutRoutine()
        {
            if (canvasGroup == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}

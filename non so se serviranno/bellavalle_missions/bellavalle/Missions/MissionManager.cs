using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Systems;

namespace Bellavalle.Missions
{
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Dati missione corrente")]
        [SerializeField] ShoppingMissionData missionData;

        [Header("Scena")]
        [SerializeField] Transform       carlaTransform;
        [SerializeField] PlayerInventory playerInventory;
        [SerializeField] WorldFeedback   worldFeedback;
        [SerializeField] HapticFeedback  haptic;

        [Header("UI lista spesa")]
        [SerializeField] ShoppingListUI  listUI;

        [Header("Audio")]
        [SerializeField] AudioSource     carlaAudio;

        List<ShoppingItem> _activeItems = new();
        bool               _missionActive;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Avvio ──────────────────────────────────────────────────────
        public void StartMission()
        {
            if (_missionActive || missionData == null) return;
            _activeItems.Clear();
            foreach (var item in missionData.items)
            {
                item.collected = false;
                _activeItems.Add(item);
            }
            _missionActive = true;
            StartCoroutine(CarlaReadsList());
        }

        // ── Fase 1: Carla legge la lista ────────────────────────────────
        IEnumerator CarlaReadsList()
        {
            bool useVisual = ShouldUseVisualMode();

            if (carlaAudio && missionData.carlaIntroClip)
            {
                carlaAudio.clip = missionData.carlaIntroClip;
                carlaAudio.Play();
                yield return new WaitForSeconds(missionData.carlaIntroClip.length + 0.4f);
            }

            for (int i = 0; i < _activeItems.Count; i++)
            {
                yield return StartCoroutine(ReadItem(_activeItems[i], i, useVisual));
                yield return new WaitForSeconds(0.6f);
            }

            if (useVisual && listUI != null)
            {
                listUI.Show(_activeItems);
                yield return new WaitForSeconds(10f);
                listUI.FadeOut();
            }

            EventBus.Emit(GameEvent.MissionCompleted, "carla_list_read");
        }

        IEnumerator ReadItem(ShoppingItem item, int index, bool visual)
        {
            if (carlaAudio && missionData.itemAudioClips != null && index < missionData.itemAudioClips.Length)
            {
                var clip = missionData.itemAudioClips[index];
                if (clip != null) { carlaAudio.PlayOneShot(clip); yield return new WaitForSeconds(clip.length); }
            }
            if (visual && item.itemSprite != null)
                listUI?.ShowItemBriefly(item, 2f);
        }

        // ── Fase 2: Verifica item al mercato ────────────────────────────
        public void OnPlayerRequestsItem(string vendorId, string requestedWord)
        {
            if (!_missionActive) return;
            var match = FindMatch(vendorId, requestedWord);
            if (match != null && !match.collected) CollectItem(match);
            else OnWrongItem(vendorId, requestedWord);
        }

        ShoppingItem FindMatch(string vendorId, string word)
        {
            string input = word.ToLower().Trim();
            foreach (var item in _activeItems)
            {
                if (item.vendorId != vendorId) continue;
                if (item.nameIT.ToLower() == input) return item;
                foreach (var a in item.aliases)
                    if (input.Contains(a.ToLower())) return item;
            }
            return null;
        }

        void CollectItem(ShoppingItem item)
        {
            item.collected = true;
            playerInventory?.AddItem(item);
            haptic?.GivePickup();
            worldFeedback?.Show("+ " + item.nameIT, carlaTransform, true, 3f);
            listUI?.MarkCollected(item);
            GameManager.Instance.LearnWord(item.nameIT);
            GameManager.Instance.RegisterAnswer(true);
            EventBus.Emit(GameEvent.PlayerAnswered, true);
            CheckMissionComplete();
        }

        void OnWrongItem(string vendorId, string word)
        {
            haptic?.Give(false);
            GameManager.Instance.RegisterAnswer(false);
            EventBus.Emit(GameEvent.PlayerAnswered, false);
        }

        void CheckMissionComplete()
        {
            foreach (var item in _activeItems)
                if (!item.collected) return;
            StartCoroutine(AllItemsCollected());
        }

        IEnumerator AllItemsCollected()
        {
            yield return new WaitForSeconds(0.5f);
            EventBus.Emit(GameEvent.MissionCompleted, missionData.missionId);
            GameManager.Instance.CompleteMission(missionData.missionId);
            UpdateShoppingDifficulty();
        }

        // ── Fase 3: Torna da Carla ──────────────────────────────────────
        public void OnPlayerReturnedToCarla()
        {
            if (!_missionActive) return;
            _missionActive = false;
            int correct = 0;
            foreach (var item in _activeItems) if (item.collected) correct++;
            StartCoroutine(CarlaReaction(correct, _activeItems.Count));
        }

        IEnumerator CarlaReaction(int correct, int total)
        {
            yield return new WaitForSeconds(0.5f);
            if (correct == total)
            {
                if (carlaAudio && missionData.carlaThankYou) carlaAudio.PlayOneShot(missionData.carlaThankYou);
                GameManager.Instance.SetNpcMood(GameManager.NPC.Carla, +0.2f);
                GameManager.Instance.SetFlag("spesa_completata_" + missionData.chapterIndex);
            }
            else
            {
                GameManager.Instance.SetNpcMood(GameManager.NPC.Carla, -0.05f);
            }
        }

        // ── Difficoltà adattiva ─────────────────────────────────────────
        bool ShouldUseVisualMode()
        {
            if (!missionData.visualModeAvailable) return false;
            return GameManager.Instance.State.GetSuccessRate() < missionData.visualModeThreshold;
        }

        void UpdateShoppingDifficulty()
        {
            GameManager.Instance.State.shoppingDifficulty =
                GameManager.Instance.State.GetSuccessRate() > 0.75f ? 1 : 0;
        }
    }
}

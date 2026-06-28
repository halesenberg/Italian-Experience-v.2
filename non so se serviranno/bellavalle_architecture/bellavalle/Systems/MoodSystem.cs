using UnityEngine;
using Bellavalle.Core;

namespace Bellavalle.Systems
{
    /// <summary>
    /// Attacca questo component al prefab di ogni NPC.
    /// Si sincronizza con GameManager.State per persistenza tra scene.
    /// Gestisce feedback visivo (blend shape, colore alone) e audio.
    /// </summary>
    public class MoodSystem : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] string npcId;

        [Header("Feedback visivo")]
        [SerializeField] Renderer  auraRenderer;      // mesh dell'alone attorno all'NPC
        [SerializeField] Color     moodColorLow  = new Color(0.8f, 0.1f, 0.1f);
        [SerializeField] Color     moodColorMid  = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] Color     moodColorHigh = new Color(0.2f, 0.8f, 0.3f);

        [Header("Blend shapes (opzionale — richiede SkinnedMeshRenderer)")]
        [SerializeField] SkinnedMeshRenderer faceRenderer;
        [SerializeField] int   frowBlendIndex  = 0;   // blend shape "cipiglio"
        [SerializeField] int   smileBlendIndex = 1;   // blend shape "sorriso"

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip   positiveSound;
        [SerializeField] AudioClip   negativeSound;

        // ── Cache ──────────────────────────────────────────────────────
        float _currentMood;
        static readonly int _colorProp = Shader.PropertyToID("_EmissionColor");

        // ── Unity lifecycle ────────────────────────────────────────────
        void OnEnable()
        {
            EventBus.On(GameEvent.NpcMoodChanged, OnMoodChanged);
            SyncFromState();
        }

        void OnDisable()
        {
            EventBus.Off(GameEvent.NpcMoodChanged, OnMoodChanged);
        }

        void SyncFromState()
        {
            _currentMood = GameManager.Instance.GetNpcMood(npcId);
            ApplyMoodVisuals(_currentMood, animated: false);
        }

        // ── Event handler ──────────────────────────────────────────────
        void OnMoodChanged(object data)
        {
            var (id, value) = ((string, float))data;
            if (id != npcId) return;
            ApplyMoodVisuals(value, animated: true);
            PlayMoodSound(value > _currentMood);
            _currentMood = value;
        }

        // ── Visuals ────────────────────────────────────────────────────
        void ApplyMoodVisuals(float mood, bool animated)
        {
            // Colore alone
            if (auraRenderer != null)
            {
                Color target = mood < 0.4f
                    ? Color.Lerp(moodColorLow, moodColorMid,  mood / 0.4f)
                    : Color.Lerp(moodColorMid, moodColorHigh, (mood - 0.4f) / 0.6f);

                if (animated)
                    StartCoroutine(LerpAuraColor(target, 0.5f));
                else
                    auraRenderer.material.SetColor(_colorProp, target);
            }

            // Blend shapes viso
            if (faceRenderer != null)
            {
                float frown = Mathf.Clamp01((0.4f - mood) / 0.4f) * 100f;
                float smile = Mathf.Clamp01((mood - 0.6f) / 0.4f) * 100f;
                faceRenderer.SetBlendShapeWeight(frowBlendIndex,  frown);
                faceRenderer.SetBlendShapeWeight(smileBlendIndex, smile);
            }
        }

        System.Collections.IEnumerator LerpAuraColor(Color target, float duration)
        {
            var mat   = auraRenderer.material;
            Color start = mat.GetColor(_colorProp);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                mat.SetColor(_colorProp, Color.Lerp(start, target, t));
                yield return null;
            }
        }

        void PlayMoodSound(bool positive)
        {
            if (audioSource == null) return;
            var clip = positive ? positiveSound : negativeSound;
            if (clip != null) audioSource.PlayOneShot(clip, 0.4f);
        }
    }
}

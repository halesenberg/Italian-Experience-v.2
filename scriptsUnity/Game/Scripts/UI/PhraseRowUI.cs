using UnityEngine;
using TMPro;
using Bellavalle.Core;

namespace Bellavalle.UI
{
    public class PhraseRowUI : MonoBehaviour
    {
        [SerializeField] TMP_Text textIT;
        [SerializeField] TMP_Text textEN;
        [SerializeField] GameObject playAudioButton;
        [SerializeField] AudioSource audioSource;

        LearnedPhrase _phrase;

        public void Setup(LearnedPhrase phrase)
        {
            _phrase = phrase;

            if (textIT != null) textIT.text = phrase.textIT;
            if (textEN != null) textEN.text = string.IsNullOrEmpty(phrase.textEN)
                ? "" : $"({phrase.textEN})";

            bool hasAudio = !string.IsNullOrEmpty(phrase.audioClipName);
            if (playAudioButton != null)
                playAudioButton.SetActive(hasAudio);
        }

        public void PlayAudio()
        {
            if (_phrase == null || string.IsNullOrEmpty(_phrase.audioClipName)) return;
            if (audioSource == null) return;

            var clip = Resources.Load<AudioClip>($"Audio/Phrases/{_phrase.audioClipName}");
            if (clip != null) audioSource.PlayOneShot(clip);
            else Debug.LogWarning($"[PhraseRowUI] Clip non trovato: {_phrase.audioClipName}");
        }
    }
}
using UnityEngine;
using TMPro;

public class RandomLineNPC : MonoBehaviour
{
    [SerializeField] string[] lines;
    [SerializeField] AudioClip[] audioClips; // stesso ordine delle lines
    [SerializeField] float displayDuration = 3f;
    [SerializeField] TextMeshPro worldText;
    [SerializeField] AudioSource audioSource;

    int _lastIndex = -1;

    public void SayRandomLine()
    {
        int index;
        do { index = Random.Range(0, lines.Length); }
        while (lines.Length > 1 && index == _lastIndex);

        _lastIndex = index;
        worldText.text = lines[index];
        worldText.gameObject.SetActive(true);

        if (audioSource != null && audioClips != null && index < audioClips.Length && audioClips[index] != null)
            audioSource.PlayOneShot(audioClips[index]);

        CancelInvoke();
        Invoke(nameof(Hide), displayDuration);
    }

    void Hide() => worldText.gameObject.SetActive(false);
}
using System;
using UnityEngine;

namespace Bellavalle.Data
{
    [CreateAssetMenu(menuName = "Bellavalle/Shopping Mission", fileName = "SpesaCap")]
    public class ShoppingMissionData : ScriptableObject
    {
        [Header("Identificazione")]
        public string missionId;
        public int    chapterIndex;

        [Header("Items da comprare")]
        public ShoppingItem[] items;

        [Header("Audio Carla")]
        public AudioClip   carlaIntroClip;
        public AudioClip[] itemAudioClips;   // stesso ordine di items[]
        public AudioClip   carlaThankYou;

        [Header("Difficoltà adattiva")]
        public bool  visualModeAvailable  = true;
        [Range(0f,1f)] public float visualModeThreshold = 0.65f;
    }

    [Serializable]
    public class ShoppingItem
    {
        [Header("Identità")]
        public string     nameIT;
        public string[]   aliases;        // parole accettate per questo item
        public Sprite     itemSprite;
        public GameObject itemPrefab;     // oggetto 3D da spawnare nello zaino

        [Header("Venditore")]
        public string    vendorId;        // "ambulante_frutta", "ambulante_pane"...
        public AudioClip vendorConfirm;
        public AudioClip vendorWrong;

        [NonSerialized] public bool collected; // solo runtime
    }
}

using System;
using UnityEngine;

namespace Bellavalle.Data
{
    /// <summary>
    /// ScriptableObject per ogni esercizio di ricostruzione frase.
    /// Crea con: tasto destro → Create → Bellavalle → Sentence Reconstruction
    ///
    /// Esempi per la scena di Giuseppe (Cap. 1 Scena 2):
    ///   Ricostruzione_Giuseppe_1.asset → "Come ti chiami?"
    ///   Ricostruzione_Giuseppe_2.asset → "Da dove vieni?"
    ///   Ricostruzione_Giuseppe_3.asset → "Quanti anni hai?"
    /// </summary>
    [CreateAssetMenu(menuName = "Bellavalle/Sentence Reconstruction", fileName = "Ricostruzione_")]
    public class SentenceReconstructionData : ScriptableObject
    {
        [Header("Identificazione")]
        public string exerciseId;          // "giuseppe_domanda_1"

        [Header("La frase da ricostruire")]
        public string fullSentenceIT;      // "Come ti chiami?"
        public string translationEN;       // "What is your name?"
        public string contextHint;         // "Giuseppe ti sta chiedendo..." (opzionale)

        [Header("Frammenti (ordine corretto)")]
        public SentenceFragment[] fragments;   // es: ["Come", "ti", "chiami", "?"]

        [Header("Audio")]
        public AudioClip giuseppeReadsClip;    // Giuseppe legge la frase ad alta voce PRIMA
        public AudioClip correctOrderClip;     // suono quando l'ordine è corretto
        public AudioClip wrongOrderClip;       // suono per ordine sbagliato

        [Header("Tempo limite (0 = nessun limite)")]
        public float timeLimitSeconds = 0f;

        [Header("Tentativi massimi (0 = infiniti)")]
        public int maxAttempts = 0;
    }

    [Serializable]
    public class SentenceFragment
    {
        public string textIT;              // testo del frammento
        public int    correctIndex;        // posizione corretta (0-based)
        public Color  cardColor = Color.white;  // colore carta (opzionale per categorie)
    }
}

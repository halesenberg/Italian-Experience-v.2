using System;
using System.Collections.Generic;

namespace Bellavalle.Core
{
    public enum PhraseCategory
    {
        Domanda,
        Risposta,
        Vocabolario
    }

    [Serializable]
    public class LearnedPhrase
    {
        public string textIT;
        public string textEN;
        public PhraseCategory category;
        public string audioClipName;   // nome del file audio, se esiste (per ora opzionale)
        public string sourceNpcId;     // chi l'ha insegnata (per contesto, opzionale)

        public LearnedPhrase() { }

        public LearnedPhrase(string textIT, string textEN, PhraseCategory category,
                              string audioClipName = null, string sourceNpcId = null)
        {
            this.textIT = textIT;
            this.textEN = textEN;
            this.category = category;
            this.audioClipName = audioClipName;
            this.sourceNpcId = sourceNpcId;
        }
    }
}
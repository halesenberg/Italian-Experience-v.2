using System;
using System.Collections.Generic;

namespace Bellavalle.Core
{
    [Serializable]
    public class GameState
    {
        // ── Progressione narrativa ──────────────────────────────────────
        public int currentChapter = 0;       // 0=Prologo, 1-4=Capitoli
        public int currentScene  = 0;
        public string playerName = "Marco";  // scelto all'inizio

        // ── Lingua ─────────────────────────────────────────────────────
        public List<string> learnedWords        = new();
        public float        languageProgress    = 0f;   // 0-1 (A1→A2)
        public int          correctAnswers      = 0;
        public int          totalAnswers        = 0;

        // ── Relazioni NPC (chiave = npcId) ─────────────────────────────
        public Dictionary<string, float>        npcMood      = new();
        public Dictionary<string, int>          npcRelLevel  = new();   // 0-3
        public Dictionary<string, List<string>> npcMemories  = new();

        // ── Missioni ───────────────────────────────────────────────────
        public List<string> completedMissions = new();
        public List<string> activeFlags       = new();  // flag narrativi generici

        // ── Preferenze player ──────────────────────────────────────────
        public bool   subtitlesEN      = true;
        public float  confidenceScore  = 0f;   // adattivo sottotitoli
        public bool   tutorialDone     = false;
        public int    shoppingDifficulty = 0;   // 0=visuale, 1=solo audio

        // ── Helper ─────────────────────────────────────────────────────
        public void SetFlag(string flag)
        {
            if (!activeFlags.Contains(flag)) activeFlags.Add(flag);
        }

        public bool HasFlag(string flag) => activeFlags.Contains(flag);

        public void InitNpc(string npcId, float startMood = 0.5f)
        {
            if (!npcMood.ContainsKey(npcId))     npcMood[npcId]     = startMood;
            if (!npcRelLevel.ContainsKey(npcId)) npcRelLevel[npcId] = 0;
            if (!npcMemories.ContainsKey(npcId)) npcMemories[npcId] = new List<string>();
        }

        public float GetSuccessRate() =>
            totalAnswers == 0 ? 0f : (float)correctAnswers / totalAnswers;
    }
}

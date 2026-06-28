using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Bellavalle.Core;
using Bellavalle.Data;

namespace Bellavalle.AI
{
    /// <summary>
    /// Singleton di scena. Gestisce tutte le chiamate all'API Anthropic.
    ///
    /// Tre modalità d'uso:
    ///  1. AskNPC()         — risposta contestuale di un NPC (input libero A2)
    ///  2. GenerateOptions()— genera 3 opzioni di risposta basate sulla storia del player
    ///  3. EvaluateInput()  — valuta una frase italiana del player (correttezza, livello)
    ///
    /// SICUREZZA: non mettere l'API key in questo file.
    /// Leggi la sezione "Setup API key" nel README.
    /// </summary>
    public class AIDialogueService : MonoBehaviour
    {
        public static AIDialogueService Instance { get; private set; }

        // ── Config ─────────────────────────────────────────────────────
        [Header("API")]
        [SerializeField] string apiEndpoint = "https://api.anthropic.com/v1/messages";
        [SerializeField] string modelId     = "claude-sonnet-4-20250514";
        [SerializeField] int    maxTokens   = 300;

        [Header("Timeout & retry")]
        [SerializeField] float  requestTimeout = 12f;
        [SerializeField] int    maxRetries     = 2;

        [Header("Debug")]
        [SerializeField] bool logRequests = true;

        // ── API key (caricata da file locale, mai hardcoded) ────────────
        string _apiKey;

        // ── Stato richieste ────────────────────────────────────────────
        bool _isBusy;

        // ── Lifecycle ──────────────────────────────────────────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadApiKey();
        }

        void LoadApiKey()
        {
            // Cerca in: StreamingAssets/api_key.txt  (non committare nel repo)
            string path = System.IO.Path.Combine(
                Application.streamingAssetsPath, "api_key.txt");

            if (System.IO.File.Exists(path))
            {
                _apiKey = System.IO.File.ReadAllText(path).Trim();
                Debug.Log("[AIDialogueService] API key caricata.");
            }
            else
            {
                Debug.LogWarning("[AIDialogueService] api_key.txt non trovato in StreamingAssets. " +
                                 "Crea il file o usa il ProxyMode.");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // MODALITÀ 1 — Risposta contestuale NPC
        // Usa nelle scene A2 con input libero (meccanica 6)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Invia il messaggio del player a un NPC e ottiene una risposta in italiano.
        /// </summary>
        /// <param name="playerInput">Frase italiana del player (da STT o testo)</param>
        /// <param name="npc">NPCData del personaggio che risponde</param>
        /// <param name="conversationHistory">Storico turni precedenti nella stessa scena</param>
        /// <param name="onResponse">Callback: (risposta IT, flag errore)</param>
        public void AskNPC(string playerInput,
                           NPCData npc,
                           List<ConversationTurn> conversationHistory,
                           Action<string, bool> onResponse)
        {
            if (_isBusy) { onResponse?.Invoke(null, true); return; }
            StartCoroutine(AskNPCRoutine(playerInput, npc, conversationHistory, onResponse, maxRetries));
        }

        IEnumerator AskNPCRoutine(string playerInput,
                                  NPCData npc,
                                  List<ConversationTurn> history,
                                  Action<string, bool> onResponse,
                                  int retriesLeft)
        {
            _isBusy = true;

            string systemPrompt = BuildNPCSystemPrompt(npc);
            var    messages     = BuildMessages(history, playerInput);
            string body         = BuildRequestBody(systemPrompt, messages);

            if (logRequests)
                Debug.Log($"[AI] Request ({npc.npcId}): {playerInput}");

            using var req = BuildRequest(body);
            req.timeout   = Mathf.RoundToInt(requestTimeout);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AI] Errore rete: {req.error}");
                if (retriesLeft > 0)
                {
                    _isBusy = false;
                    yield return new WaitForSeconds(1f);
                    yield return AskNPCRoutine(playerInput, npc, history, onResponse, retriesLeft - 1);
                    yield break;
                }
                _isBusy = false;
                onResponse?.Invoke(null, true);
                yield break;
            }

            string response = ParseTextResponse(req.downloadHandler.text);
            if (logRequests) Debug.Log($"[AI] Response: {response}");

            _isBusy = false;
            onResponse?.Invoke(response, false);
        }

        // ══════════════════════════════════════════════════════════════
        // MODALITÀ 2 — Genera 3 opzioni per la scena finale
        // Chiama prima di caricare la scena finale; le opzioni appaiono
        // come scelte normali nel DialogueUI
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera 3 risposte italiane A2 personalizzate sulla storia del player.
        /// </summary>
        public void GenerateFinalOptions(Action<FinalOptions, bool> onResult)
        {
            StartCoroutine(GenerateFinalOptionsRoutine(onResult));
        }

        IEnumerator GenerateFinalOptionsRoutine(Action<FinalOptions, bool> onResult)
        {
            _isBusy = true;

            var state        = GameManager.Instance.State;
            string eventSummary = BuildEventSummary(state);

            string system = @"Sei un generatore di dialogo per un videogioco VR di apprendimento dell'italiano.
Rispondi SOLO con un oggetto JSON valido, senza testo aggiuntivo, senza markdown.";

            string userPrompt = $@"Il player si chiama {state.playerName} e ha appena finito 3 mesi a Bellavalle.
Luca (il barista) chiede: ""Allora, torni nel tuo paese?""

Fatti rilevanti dalla storia del player:
{eventSummary}

Genera 3 risposte italiane A2 (20-30 parole ciascuna) con questi toni:
- nostalgica (pensa a cosa lascia)
- entusiasta (è felice di restare o di portarsi qualcosa)
- incerta (non sa ancora)

Rispondi con questo JSON esatto:
{{
  ""nostalgica"": ""<frase>"",
  ""entusiasta"": ""<frase>"",
  ""incerta"": ""<frase>""
}}";

            var messages = new List<object>
            {
                new { role = "user", content = userPrompt }
            };
            string body = BuildRequestBody(system, messages);

            using var req = BuildRequest(body);
            req.timeout   = Mathf.RoundToInt(requestTimeout);
            yield return req.SendWebRequest();

            _isBusy = false;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AI] GenerateFinalOptions error: {req.error}");
                onResult?.Invoke(null, true);
                yield break;
            }

            var options = ParseFinalOptions(req.downloadHandler.text);
            onResult?.Invoke(options, options == null);
        }

        // ══════════════════════════════════════════════════════════════
        // MODALITÀ 3 — Valutazione input libero
        // Verifica se la frase del player è comprensibile e a che livello
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Valuta una frase italiana del player: comprensibile? livello? errori principali?
        /// Usato per il feedback nella scena finale e nelle scene A2 avanzate.
        /// </summary>
        public void EvaluatePlayerInput(string playerInput,
                                        string expectedContext,
                                        Action<EvaluationResult, bool> onResult)
        {
            StartCoroutine(EvaluateRoutine(playerInput, expectedContext, onResult));
        }

        IEnumerator EvaluateRoutine(string input, string context, Action<EvaluationResult, bool> onResult)
        {
            _isBusy = true;

            string system = @"Sei un valutatore di italiano per studenti principianti.
Rispondi SOLO con JSON valido, senza testo aggiuntivo.";

            string userPrompt = $@"Contesto della conversazione: {context}
Frase dello studente: ""{input}""

Valuta e rispondi con questo JSON:
{{
  ""understandable"": true/false,
  ""level"": ""A1""/""A2""/""below_A1"",
  ""mainError"": ""descrizione breve in inglese o null"",
  ""corrected"": ""versione corretta italiana o null se già corretta"",
  ""encouragement"": ""frase breve incoraggiante in italiano (10-15 parole)""
}}";

            var messages = new List<object>
            {
                new { role = "user", content = userPrompt }
            };
            string body = BuildRequestBody(system, messages);

            using var req = BuildRequest(body);
            req.timeout   = Mathf.RoundToInt(requestTimeout);
            yield return req.SendWebRequest();

            _isBusy = false;

            if (req.result != UnityWebRequest.Result.Success)
            { onResult?.Invoke(null, true); yield break; }

            var result = ParseEvaluation(req.downloadHandler.text);
            onResult?.Invoke(result, result == null);
        }

        // ══════════════════════════════════════════════════════════════
        // SYSTEM PROMPT BUILDER — cuore della qualità delle risposte
        // ══════════════════════════════════════════════════════════════

        string BuildNPCSystemPrompt(NPCData npc)
        {
            var    state   = GameManager.Instance.State;
            string level   = LanguageTracker.Instance.IsA2 ? "A2" : "A1";
            float  mood    = GameManager.Instance.GetNpcMood(npc.npcId);
            string moodStr = mood > 0.65f ? "cordiale e sorridente"
                           : mood > 0.35f ? "neutro e normale"
                           :               "un po' seccato";

            // Fatti che questo NPC sa del player
            string memories = "";
            if (state.npcMemories.TryGetValue(npc.npcId, out var mem) && mem.Count > 0)
                memories = "Sai queste cose del player: " + string.Join(", ", mem) + ".";

            return $@"Sei {npc.displayName}, un personaggio di Bellavalle, una piccola città italiana immaginaria nel centro Italia.

{npc.bio}

REGOLE FONDAMENTALI:
- Rispondi SOLO in italiano. Non usare mai l'inglese, neanche se il player parla inglese.
- Il player si chiama {state.playerName} ed è al livello {level} di italiano.
- Il tuo tono attuale è: {moodStr}.
- Frasi corte (max 2 frasi). Vocabolario semplice se il player è A1.
- Se non capisci la frase del player, chiedi di ripetere in modo naturale.
- Non correggere esplicitamente gli errori grammaticali — reagisci al significato.
- Ogni risposta deve avanzare la conversazione, non solo confermare.
{memories}

Funzioni linguistiche che insegni: {string.Join(", ", npc.languageFunctions ?? Array.Empty<string>())}";
        }

        // ══════════════════════════════════════════════════════════════
        // HELPER BUILDERS
        // ══════════════════════════════════════════════════════════════

        List<object> BuildMessages(List<ConversationTurn> history, string newInput)
        {
            var messages = new List<object>();

            // Storico turni precedenti (max ultimi 6 per limitare token)
            int start = Mathf.Max(0, history.Count - 6);
            for (int i = start; i < history.Count; i++)
            {
                var turn = history[i];
                messages.Add(new { role = "user",      content = turn.playerLine });
                messages.Add(new { role = "assistant", content = turn.npcLine    });
            }

            // Nuovo input
            messages.Add(new { role = "user", content = newInput });
            return messages;
        }

        string BuildRequestBody(string system, List<object> messages)
        {
            var payload = new
            {
                model      = modelId,
                max_tokens = maxTokens,
                system,
                messages
            };
            return JsonConvert.SerializeObject(payload);
        }

        UnityWebRequest BuildRequest(string jsonBody)
        {
            var req = new UnityWebRequest(apiEndpoint, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type",       "application/json");
            req.SetRequestHeader("x-api-key",          _apiKey ?? "");
            req.SetRequestHeader("anthropic-version",  "2023-06-01");
            return req;
        }

        // ══════════════════════════════════════════════════════════════
        // PARSER
        // ══════════════════════════════════════════════════════════════

        string ParseTextResponse(string raw)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<AnthropicResponse>(raw);
                foreach (var block in obj.content)
                    if (block.type == "text") return block.text?.Trim();
            }
            catch (Exception e) { Debug.LogError($"[AI] Parse error: {e.Message}\n{raw}"); }
            return null;
        }

        FinalOptions ParseFinalOptions(string raw)
        {
            try
            {
                string text = ParseTextResponse(raw);
                if (text == null) return null;
                // Rimuovi eventuali backtick JSON residui
                text = text.Replace("```json", "").Replace("```", "").Trim();
                return JsonConvert.DeserializeObject<FinalOptions>(text);
            }
            catch (Exception e) { Debug.LogError($"[AI] ParseFinalOptions: {e.Message}"); return null; }
        }

        EvaluationResult ParseEvaluation(string raw)
        {
            try
            {
                string text = ParseTextResponse(raw);
                if (text == null) return null;
                text = text.Replace("```json", "").Replace("```", "").Trim();
                return JsonConvert.DeserializeObject<EvaluationResult>(text);
            }
            catch (Exception e) { Debug.LogError($"[AI] ParseEvaluation: {e.Message}"); return null; }
        }

        // ══════════════════════════════════════════════════════════════
        // CONTEXT BUILDER — riassume la storia del player per l'AI
        // ══════════════════════════════════════════════════════════════

        string BuildEventSummary(GameState state)
        {
            var sb = new StringBuilder();

            if (state.learnedWords.Count > 0)
                sb.AppendLine($"- Ha imparato {state.learnedWords.Count} parole italiane.");

            if (state.completedMissions.Count > 0)
                sb.AppendLine($"- Missioni completate: {string.Join(", ", state.completedMissions)}.");

            // Relazioni NPC significative
            foreach (var kv in state.npcRelLevel)
                if (kv.Value >= 2)
                    sb.AppendLine($"- Ha un buon rapporto con {kv.Key} (livello {kv.Value}).");

            if (state.npcMood.TryGetValue(GameManager.NPC.Enzo, out float enzoMood) && enzoMood > 0.6f)
                sb.AppendLine("- È riuscito a far ammorbidire anche Enzo.");

            if (state.GetSuccessRate() > 0.75f)
                sb.AppendLine("- Ha risposto correttamente alla maggior parte delle conversazioni.");

            return sb.Length > 0 ? sb.ToString() : "- Primo giorno a Bellavalle, nulla di rilevante ancora.";
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // DATA CLASSES
    // ══════════════════════════════════════════════════════════════════

    [Serializable]
    public class ConversationTurn
    {
        public string playerLine;
        public string npcLine;
    }

    [Serializable]
    public class FinalOptions
    {
        public string nostalgica;
        public string entusiasta;
        public string incerta;
    }

    [Serializable]
    public class EvaluationResult
    {
        public bool   understandable;
        public string level;          // "A1", "A2", "below_A1"
        public string mainError;      // null se nessun errore
        public string corrected;      // versione corretta, null se già ok
        public string encouragement;  // frase incoraggiante in italiano
    }

    // ── Deserializzazione risposta Anthropic ───────────────────────────
    [Serializable]
    class AnthropicResponse
    {
        public List<ContentBlock> content;
    }
    [Serializable]
    class ContentBlock
    {
        public string type;
        public string text;
    }
}

using System.IO;
using UnityEngine;
using Newtonsoft.Json;   // com.unity.nuget.newtonsoft-json (Package Manager)

namespace Bellavalle.Core
{
    public static class SaveSystem
    {
        static string Path => System.IO.Path.Combine(
            Application.persistentDataPath, "bellavalle_save.json");

        public static void Save(GameState state)
        {
            string json = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(Path, json);
            Debug.Log($"[SaveSystem] Salvato in {Path}");
        }

        public static GameState Load()
        {
            if (!File.Exists(Path))
            {
                Debug.Log("[SaveSystem] Nessun salvataggio trovato, nuovo gioco.");
                return new GameState();
            }
            string json = File.ReadAllText(Path);
            return JsonConvert.DeserializeObject<GameState>(json) ?? new GameState();
        }

        public static void Delete()
        {
            if (File.Exists(Path)) File.Delete(Path);
            Debug.Log("[SaveSystem] Salvataggio eliminato.");
        }
    }
}

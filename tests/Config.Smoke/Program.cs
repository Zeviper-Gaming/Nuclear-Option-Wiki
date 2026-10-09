using System;
using System.IO;
using BepInEx.Configuration;
using UnityEngine;

namespace NuclearOptionWiki
{
    // Remplace la classe Plugin (MonoBehaviour) : WikiConfig n'utilise que Plugin.Log.
    internal static class Plugin
    {
        internal static BepInEx.Logging.ManualLogSource Log = null;
    }

    /// <summary>
    /// Crée toutes les entrées de configuration avec le vrai code de BepInEx (bug de la v0.2.1 : une apostrophe
    /// dans un nom de clé faisait échouer le démarrage du mod), puis vérifie le refus de la touche Échap.
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            string path = Path.Combine(Path.GetTempPath(), "nuclearoptionwiki-smoke.cfg");
            if (File.Exists(path)) File.Delete(path);
            try
            {
                var file = new ConfigFile(path, true);
                var cfg = new WikiConfig(file);
                cfg.ToggleKey.Value = new KeyboardShortcut(KeyCode.Escape);
                bool escapeRejected = cfg.ToggleKey.Value.MainKey == KeyCode.F6;
                file.Save();
                int entries = file.Count;
                Console.WriteLine($"{entries} entrées créées ; Échap refusée : {escapeRejected}");
                bool ok = entries == 9 && escapeRejected;
                Console.WriteLine(ok ? "OK" : "ÉCHEC");
                return ok ? 0 : 1;
            }
            catch (Exception e)
            {
                Console.WriteLine("ÉCHEC : " + e);
                return 1;
            }
        }
    }
}

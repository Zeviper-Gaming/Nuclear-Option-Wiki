using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOptionWiki.Core;
using NuclearOptionWiki.Integration;

namespace NuclearOptionWiki
{
    [BepInPlugin(Guid, DisplayName, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "zeviper.nuclearoption.wiki";
        public const string DisplayName = "Nuclear Option Wiki";
        public const string Version = "0.3.0";
        public const string DataFileName = "weapons.json";

        internal static ManualLogSource Log;
        internal static WikiConfig Settings;
        internal static WeaponDatabase Database;

        private void Awake()
        {
            Log = Logger;
            try
            {
                Settings = new WikiConfig(Config);
                Database = LoadDatabase();
                int applied = LoadoutHooks.ApplyAll(new Harmony(Guid));
                Log.LogInfo($"[Wiki] chargé v{Version} : {Database?.Count ?? 0} armes, {applied} patch(s) appliqué(s).");
            }
            catch (Exception e)
            {
                // Une exception dans Awake n'apparaît pas toujours dans LogOutput.log : on la journalise explicitement.
                Log.LogError($"[Wiki] Échec du démarrage, mod inactif : {e}");
            }
        }

        /// <summary>
        /// Charge les données : weapons.json posé à côté de la DLL s'il existe (mise à jour sans recompiler),
        /// sinon la copie intégrée à la DLL. En cas d'échec : journal d'erreur, fiche désactivée, jeu intact.
        /// </summary>
        private WeaponDatabase LoadDatabase()
        {
            string path = Path.Combine(Path.GetDirectoryName(Info.Location) ?? Paths.PluginPath, DataFileName);
            if (File.Exists(path))
            {
                try
                {
                    var db = WeaponDatabase.FromJson(File.ReadAllText(path));
                    Log.LogInfo($"[Wiki] Données lues depuis {path}.");
                    return db;
                }
                catch (Exception e)
                {
                    Log.LogError($"[Wiki] {path} illisible ({e.Message}) : utilisation des données intégrées à la DLL.");
                }
            }
            try
            {
                using (var stream = typeof(Plugin).Assembly.GetManifestResourceStream("NuclearOptionWiki." + DataFileName))
                {
                    if (stream == null) throw new FileNotFoundException("ressource intégrée absente");
                    using (var reader = new StreamReader(stream))
                    {
                        var db = WeaponDatabase.FromJson(reader.ReadToEnd());
                        Log.LogInfo("[Wiki] Données intégrées à la DLL utilisées.");
                        return db;
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"[Wiki] Données des armes introuvables ({e.Message}). La fiche est désactivée.");
                return null;
            }
        }
    }
}

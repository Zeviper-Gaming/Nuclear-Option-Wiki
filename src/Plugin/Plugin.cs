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
        public const string Version = "0.5.0";
        public const string DataFileName = "weapons.json";
        public const string AircraftFileName = "aircraft.json";

        internal static ManualLogSource Log;
        internal static WikiConfig Settings;
        internal static WeaponDatabase Database;
        internal static AircraftDatabase Aircraft;
        internal static TagsFile Tags = new TagsFile();

        private static string _pluginDir;

        private void Awake()
        {
            Log = Logger;
            try
            {
                _pluginDir = Path.GetDirectoryName(Info.Location) ?? Paths.PluginPath;
                Settings = new WikiConfig(Config, Diagnostics.ExportButtons.Tag, TagsEditor.Tag);
                // Armes et appareils sont chargés indépendamment (A18) : l'échec de l'un ne désactive pas l'autre.
                Database = Load(DataFileName, WeaponDatabase.FromJson, "des armes");
                Aircraft = Load(AircraftFileName, AircraftDatabase.FromJson, "des appareils");
                LoadTags();
                int applied = LoadoutHooks.ApplyAll(new Harmony(Guid));
                Log.LogInfo($"[Wiki] chargé v{Version} : {Database?.Count ?? 0} armes, {Aircraft?.Count ?? 0} appareils, " +
                            $"{applied} patch(s) appliqué(s). DLL : {Info.Location}");
            }
            catch (Exception e)
            {
                // Une exception dans Awake n'apparaît pas toujours dans LogOutput.log : on la journalise explicitement.
                Log.LogError($"[Wiki] Échec du démarrage, mod inactif : {e}");
            }
        }

        /// <summary>
        /// Charge un fichier de données : celui posé à côté de la DLL s'il existe (mise à jour sans recompiler),
        /// sinon la copie intégrée à la DLL. En cas d'échec : journal d'erreur, fiche correspondante désactivée, jeu intact.
        /// </summary>
        private static T Load<T>(string fileName, Func<string, T> parse, string what) where T : class
        {
            string path = Path.Combine(_pluginDir, fileName);
            if (File.Exists(path))
            {
                try
                {
                    var db = parse(File.ReadAllText(path));
                    Log.LogInfo($"[Wiki] Données {what} lues depuis {path}.");
                    return db;
                }
                catch (Exception e)
                {
                    Log.LogError($"[Wiki] {path} illisible ({e.Message}) : utilisation des données intégrées à la DLL.");
                }
            }
            try
            {
                using (var stream = typeof(Plugin).Assembly.GetManifestResourceStream("NuclearOptionWiki." + fileName))
                {
                    if (stream == null) throw new FileNotFoundException("ressource intégrée absente");
                    using (var reader = new StreamReader(stream))
                    {
                        var db = parse(reader.ReadToEnd());
                        Log.LogInfo($"[Wiki] Données {what} intégrées à la DLL utilisées.");
                        return db;
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"[Wiki] Données {what} introuvables ({e.Message}). La fiche correspondante est désactivée.");
                return null;
            }
        }

        /// <summary>A24 : tags saisis en jeu (BepInEx/config/NuclearOptionWiki_tags.json).</summary>
        private static void LoadTags()
        {
            string path = Path.Combine(Paths.ConfigPath, TagsFile.FileName);
            try
            {
                Tags = File.Exists(path) ? TagsFile.FromJson(File.ReadAllText(path)) : new TagsFile();
                if (Tags.Count > 0) Log.LogInfo($"[Wiki] Tags saisis en jeu : {Tags.Count} appareil(s) ({path}).");
            }
            catch (Exception e)
            {
                Log.LogError($"[Wiki] {path} illisible ({e.Message}) : tags saisis en jeu ignorés.");
                Tags = new TagsFile();
            }
        }

        internal static bool SaveTags()
        {
            string path = Path.Combine(Paths.ConfigPath, TagsFile.FileName);
            try
            {
                File.WriteAllText(path, Tags.ToJson(), new System.Text.UTF8Encoding(false));
                return true;
            }
            catch (Exception e)
            {
                Log.LogError($"[Wiki] Écriture de {path} impossible : {e.Message}");
                return false;
            }
        }
    }
}

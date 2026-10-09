using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOptionWiki.Integration;

namespace NuclearOptionWiki
{
    [BepInPlugin(Guid, DisplayName, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "zeviper.nuclearoption.wiki";
        public const string DisplayName = "Nuclear Option Wiki";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;
        internal static WikiConfig Settings;

        private void Awake()
        {
            Log = Logger;
            Settings = new WikiConfig(Config);
            int applied = LoadoutHooks.ApplyAll(new Harmony(Guid));
            Log.LogInfo($"[Wiki] chargé v{Version} ({applied} patch(s) appliqué(s))");
        }
    }
}

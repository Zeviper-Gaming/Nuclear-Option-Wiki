using System;
using System.Reflection;
using HarmonyLib;
using NuclearOptionWiki.Diagnostics;

namespace NuclearOptionWiki.Integration
{
    /// <summary>
    /// Patchs Harmony (tous en Postfix). Chaque classe de patch est appliquée séparément :
    /// si une méthode du jeu a changé, seul ce patch est désactivé et le jeu continue normalement.
    /// </summary>
    internal static class LoadoutHooks
    {
        private static readonly Type[] PatchClasses =
        {
            typeof(MenuInitializePatch),
        };

        public static int ApplyAll(Harmony harmony)
        {
            int applied = 0;
            foreach (var patch in PatchClasses)
            {
                try
                {
                    harmony.CreateClassProcessor(patch).Patch();
                    applied++;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"[Wiki] Patch {patch.Name} non appliqué (version du jeu différente ?) : {e.GetBaseException().Message}");
                }
            }
            return applied;
        }

        /// <summary>Ouverture de l'écran de loadout / hangar.</summary>
        [HarmonyPatch]
        private static class MenuInitializePatch
        {
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(AircraftSelectionMenu), "Initialize");

            private static void Postfix()
            {
                try
                {
                    if (Plugin.Settings.ExportWeapons.Value)
                        WeaponDump.Export();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"[Wiki] Erreur à l'ouverture du hangar : {e}");
                }
            }
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NuclearOptionWiki.Diagnostics;

namespace NuclearOptionWiki.Integration
{
    /// <summary>
    /// Patchs Harmony (tous en Postfix, plan section 7.5). Chaque classe de patch est appliquée séparément :
    /// si une méthode du jeu a changé, seul ce patch est désactivé et le jeu continue normalement.
    /// </summary>
    internal static class LoadoutHooks
    {
        private static readonly Type[] PatchClasses =
        {
            typeof(MenuInitializePatch),
            typeof(MountHoverPatch),
            typeof(SlotSelectionPatch),
            typeof(SlotCreatedPatch),
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

        private static void Guard(string where, Action action)
        {
            try { action(); }
            catch (Exception e) { Plugin.Log.LogError($"[Wiki] Erreur ({where}) : {e}"); }
        }

        /// <summary>Ouverture de l'écran de loadout / hangar.</summary>
        [HarmonyPatch]
        private static class MenuInitializePatch
        {
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(AircraftSelectionMenu), "Initialize");

            private static void Postfix(AircraftSelectionMenu __instance) => Guard("ouverture du hangar", () =>
            {
                if (Plugin.Settings.ExportWeapons.Value) WeaponDump.Export();
                if (Plugin.Settings.ExportAircraft.Value) AircraftDump.Export();
                WikiController.Attach(__instance);
            });
        }

        /// <summary>Survol d'une option dans la liste déroulante ouverte (null pour « Empty »).</summary>
        [HarmonyPatch]
        private static class MountHoverPatch
        {
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(LoadoutSelector), "ShowMountInfo");

            private static void Postfix(WeaponMount mount) => Guard("survol", () => WikiController.ShowWeapon(mount));
        }

        /// <summary>Sélection d'une arme dans un emplacement.</summary>
        [HarmonyPatch]
        private static class SlotSelectionPatch
        {
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(WeaponSelector), "DropdownChanged");

            private static void Postfix(WeaponSelector __instance) => Guard("sélection", () => WikiController.ShowWeapon(__instance.GetValue()));
        }

        /// <summary>Création d'un emplacement dans l'écran hangar : ajout du survol de l'emplacement fermé.</summary>
        [HarmonyPatch]
        private static class SlotCreatedPatch
        {
            // WeaponSelector.Initialize a deux surcharges : on vise celle du hangar (Aircraft, HardpointSet, FactionHQ, Airbase).
            private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(WeaponSelector))
                .FirstOrDefault(m => m.Name == "Initialize" && m.GetParameters().Length == 4);

            private static void Postfix(WeaponSelector __instance) => Guard("emplacement", () => SlotHoverRelay.AttachTo(__instance));
        }
    }
}

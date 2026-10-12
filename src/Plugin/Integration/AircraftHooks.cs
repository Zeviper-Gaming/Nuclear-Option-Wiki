using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOptionWiki.Integration
{
    /// <summary>
    /// Plan avions V9 : survol d'un bouton de la grille d'appareils (grisé compris) → fiche de cet appareil.
    /// Patch Postfix de AircraftSelectionButton.Initialize (A19), qui ajoute un relais de survol sur chaque bouton.
    /// </summary>
    [HarmonyPatch]
    internal static class AircraftButtonPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(AircraftSelectionButton), "Initialize");

        private static void Postfix(AircraftSelectionButton __instance)
        {
            try { AircraftHoverRelay.AttachTo(__instance); }
            catch (System.Exception e) { Plugin.Log.LogError($"[Wiki] Erreur (bouton d'appareil) : {e}"); }
        }
    }

    internal sealed class AircraftHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private AircraftSelectionButton _button;

        public static void AttachTo(AircraftSelectionButton button)
        {
            if (button == null || button.GetComponent<AircraftHoverRelay>() != null) return;
            button.gameObject.AddComponent<AircraftHoverRelay>()._button = button;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && _button.definition != null) WikiController.ShowAircraft(_button.definition, this);
        }

        /// <summary>v3.5 : la souris quitte le bouton → retour à l'appareil sélectionné.</summary>
        public void OnPointerExit(PointerEventData eventData) => WikiController.EndHover(this);
    }
}

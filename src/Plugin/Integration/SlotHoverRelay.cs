using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOptionWiki.Integration
{
    /// <summary>
    /// Ajouté sur chaque emplacement d'arme (WeaponSelector) : survoler l'emplacement fermé affiche l'arme équipée.
    /// Liste déroulante ouverte : ce sont les options survolées qui comptent (LoadoutSelector.ShowMountInfo).
    /// </summary>
    internal sealed class SlotHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private WeaponSelector _selector;
        private TMP_Dropdown _dropdown;

        public static void AttachTo(WeaponSelector selector)
        {
            if (selector == null || selector.GetComponent<SlotHoverRelay>() != null) return;
            var relay = selector.gameObject.AddComponent<SlotHoverRelay>();
            relay._selector = selector;
            relay._dropdown = selector.GetComponentInChildren<TMP_Dropdown>(true);
            // v3.5 : les options de la liste déroulante sont clonées depuis le modèle : un relais de sortie posé
            // sur le modèle est recopié sur chaque option.
            var item = relay._dropdown != null && relay._dropdown.template != null
                ? relay._dropdown.template.GetComponentInChildren<Toggle>(true) : null;
            if (item != null && item.GetComponent<OptionExitRelay>() == null) item.gameObject.AddComponent<OptionExitRelay>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selector == null) return;
            if (_dropdown != null && _dropdown.IsExpanded) return;
            WikiController.ShowWeapon(_selector.GetValue(), this);
        }

        /// <summary>v3.5 : la souris quitte l'emplacement → retour à l'appareil sélectionné.</summary>
        public void OnPointerExit(PointerEventData eventData) => WikiController.EndHover(this);
    }

    /// <summary>Option de liste déroulante : la quitter met fin au survol de l'arme (l'option suivante prend le relais).</summary>
    internal sealed class OptionExitRelay : MonoBehaviour, IPointerExitHandler
    {
        public void OnPointerExit(PointerEventData eventData) => WikiController.EndHover(null);
    }
}

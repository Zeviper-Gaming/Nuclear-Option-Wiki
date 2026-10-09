using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOptionWiki.Integration
{
    /// <summary>
    /// Ajouté sur chaque emplacement d'arme (WeaponSelector) : survoler l'emplacement fermé affiche l'arme équipée.
    /// Liste déroulante ouverte : ce sont les options survolées qui comptent (LoadoutSelector.ShowMountInfo).
    /// </summary>
    internal sealed class SlotHoverRelay : MonoBehaviour, IPointerEnterHandler
    {
        private WeaponSelector _selector;
        private TMP_Dropdown _dropdown;

        public static void AttachTo(WeaponSelector selector)
        {
            if (selector == null || selector.GetComponent<SlotHoverRelay>() != null) return;
            var relay = selector.gameObject.AddComponent<SlotHoverRelay>();
            relay._selector = selector;
            relay._dropdown = selector.GetComponentInChildren<TMP_Dropdown>(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selector == null) return;
            if (_dropdown != null && _dropdown.IsExpanded) return;
            WikiController.ShowWeapon(_selector.GetValue());
        }
    }
}

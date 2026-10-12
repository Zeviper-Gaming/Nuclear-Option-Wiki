using NuclearOptionWiki.Core;
using TMPro;
using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI.Widgets
{
    /// <summary>Cellule de stat : petit libellé en capitales + valeur.</summary>
    internal sealed class StatCell
    {
        public const float Height = 46f;

        private readonly TextMeshProUGUI _label, _value;
        private readonly Color _valueColor;
        private readonly bool _greyNa;   // fiche appareil seulement : « N/A » en gris (la fiche arme reste inchangée)

        public StatCell(Transform parent, ThemeDef th, TMP_FontAsset font, float x, float y, float w, float h = Height)
        {
            _valueColor = th.StatValue;
            _greyNa = h < Height;
            var frame = Frame(parent, "Stat", x, y, w, h, th.StatBg, th.StatBorder, th.StatRadius, 1);
            float compact = h < Height ? 3f : 0f;   // fiche appareil : cellules de 40 px (plan avions 7.3)
            _label = Text(frame.Root, "Libellé", 7f, 5f - compact / 2f, w - 14f, 14f, font, 10.5f, th.StatLabel, TextAlignmentOptions.TopLeft, 10f, true);
            _value = Text(frame.Root, "Valeur", 7f, 21f - compact, w - 14f, 21f, font, h < Height ? 15f : 16f, th.StatValue, TextAlignmentOptions.TopLeft);
        }

        public void Apply(StatField f)
        {
            SetText(_label, f.Label);
            SetText(_value, f.Value);
            _value.color = _greyNa && f.Value == Format.NotAvailable ? Hex(NoteScale.NotAvailableHex) : _valueColor;
        }
    }
}

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

        public StatCell(Transform parent, ThemeDef th, TMP_FontAsset font, float x, float y, float w)
        {
            var frame = Frame(parent, "Stat", x, y, w, Height, th.StatBg, th.StatBorder, th.StatRadius, 1);
            _label = Text(frame.Root, "Libellé", 7f, 5f, w - 14f, 14f, font, 10.5f, th.StatLabel, TextAlignmentOptions.TopLeft, 10f, true);
            _value = Text(frame.Root, "Valeur", 7f, 21f, w - 14f, 21f, font, 16f, th.StatValue, TextAlignmentOptions.TopLeft);
        }

        public void Apply(StatField f)
        {
            SetText(_label, f.Label);
            SetText(_value, f.Value);
        }
    }
}

using TMPro;
using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI.Widgets
{
    /// <summary>Titre de zone (« STATS GLOBALES »…), avec bandeau et filet selon le thème.</summary>
    internal static class ZoneTitle
    {
        public const float Height = 20f;

        public static void Create(Transform parent, ThemeDef th, TMP_FontAsset font, string text, float y, float width)
        {
            float textX = 0f;
            if (th.ZoneTitleBg.a > 0f) Box(parent, "Bandeau", 0f, y, width, Height, th.ZoneTitleBg, null);
            if (th.ZoneTitleAccent.a > 0f)
            {
                Box(parent, "Filet", 0f, y, 3f, Height, th.ZoneTitleAccent, null);
                textX = 11f;
            }
            else if (th.ZoneTitleBg.a > 0f && !th.ZoneTitleCentered)
            {
                textX = 8f;
            }
            var align = th.ZoneTitleCentered ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineLeft;
            var t = Text(parent, "Titre", textX, y, width - textX, Height, font, 13f, th.ZoneTitle, align, 18f, true);
            SetText(t, text);
        }
    }
}

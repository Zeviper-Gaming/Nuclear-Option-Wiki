using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI.Widgets
{
    /// <summary>
    /// Ligne « PROFIL » : pastilles du profil d'attaque (plan v1.1, D26), posées de gauche à droite.
    /// Débordement : la dernière pastille visible devient « +n ». Aucune pastille : « N/A ».
    /// </summary>
    internal sealed class ProfileRow
    {
        public const float Height = 18f;
        private const int MaxChips = 6;
        private const float LabelW = 88f, Gap = 10f, ChipGap = 6f, ChipPad = 14f;

        private readonly ThemeDef _th;
        private readonly float _x0, _width;
        private readonly Frame[] _frames = new Frame[MaxChips];
        private readonly TextMeshProUGUI[] _texts = new TextMeshProUGUI[MaxChips];

        public ProfileRow(Transform parent, ThemeDef th, TMP_FontAsset font, float y, float width)
        {
            _th = th;
            var label = Text(parent, "Libellé", 0f, y, LabelW, Height, font, 13f, th.BarLabel, TextAlignmentOptions.MidlineLeft, 0f, true);
            SetText(label, "Profil");
            _x0 = LabelW + Gap;
            _width = width - _x0;
            for (int i = 0; i < MaxChips; i++)
            {
                _frames[i] = Frame(parent, "Pastille" + i, _x0, y, 10f, Height, Color.clear, th.Chip, 3, 1);
                _texts[i] = Text(_frames[i].Root, "Texte", 0f, 0f, 10f, Height, font, 10.5f, th.Chip, TextAlignmentOptions.Center, 8f, true);
                _frames[i].Root.gameObject.SetActive(false);
            }
        }

        public void Apply(List<string> chips)
        {
            var items = chips != null && chips.Count > 0 ? chips : new List<string> { "N/A" };
            bool na = chips == null || chips.Count == 0;
            float x = 0f;
            int shown = 0;
            for (int i = 0; i < items.Count && shown < MaxChips; i++)
            {
                int remaining = items.Count - i;
                string text = items[i];
                float w = Measure(shown, text);
                // Place pour cette pastille ET, s'il en reste après, pour une pastille « +n » ?
                bool last = remaining == 1;
                float plusW = last ? 0f : Measure(shown, "+" + (remaining - 1)) + ChipGap;
                if (x + w + plusW > _width || (shown == MaxChips - 1 && !last))
                {
                    text = "+" + remaining;
                    w = Measure(shown, text);
                    Place(shown++, text, x, w, na);
                    break;
                }
                Place(shown++, text, x, w, na);
                x += w + ChipGap;
            }
            for (int i = shown; i < MaxChips; i++) _frames[i].Root.gameObject.SetActive(false);
        }

        private float Measure(int index, string text) =>
            _texts[index].GetPreferredValues(text.ToUpperInvariant()).x + ChipPad;

        private void Place(int index, string text, float x, float w, bool na)
        {
            var f = _frames[index];
            f.Root.gameObject.SetActive(true);
            f.Root.anchoredPosition = new Vector2(_x0 + x, f.Root.anchoredPosition.y);
            f.Root.sizeDelta = new Vector2(w, Height);
            f.Fill.rectTransform.sizeDelta = new Vector2(w, Height);
            if (f.Border != null)
            {
                f.Border.rectTransform.sizeDelta = new Vector2(w, Height);
                f.Border.color = na ? Hex(Core.NoteScale.NotAvailableHex) : _th.Chip;
            }
            _texts[index].rectTransform.sizeDelta = new Vector2(w, Height);
            _texts[index].color = na ? Hex(Core.NoteScale.NotAvailableHex) : _th.Chip;
            SetText(_texts[index], text);
        }
    }
}

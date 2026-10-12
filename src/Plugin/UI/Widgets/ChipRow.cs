using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI.Widgets
{
    /// <summary>
    /// Ligne « RÔLES » ou « TAGS » de la fiche appareil (plan avions 7.3, v3.3) : libellé puis pastilles arrondies,
    /// de gauche à droite. Débordement : la dernière pastille visible devient « +n ». Vide : une pastille grise (« N/A » ou « — »).
    /// </summary>
    internal sealed class ChipRow
    {
        private const int MaxChips = 6;
        private const float LabelW = 52f, ChipGap = 6f, ChipPad = 16f;

        private readonly ThemeDef _th;
        private readonly float _x0, _width, _height;
        private readonly bool _dim;
        private readonly string _empty;
        private readonly Frame[] _frames = new Frame[MaxChips];
        private readonly TextMeshProUGUI[] _texts = new TextMeshProUGUI[MaxChips];

        /// <param name="dim">Tags : bordure plus discrète que les rôles.</param>
        public ChipRow(Transform parent, ThemeDef th, TMP_FontAsset font, string label, string empty, float y, float width, float height, bool dim)
        {
            _th = th;
            _height = height;
            _dim = dim;
            _empty = empty;
            var l = Text(parent, "Libellé", 0f, y, LabelW, height, font, 11f, WithAlpha(th.BarLabel, 0.7f), TextAlignmentOptions.MidlineLeft, 14f, true);
            SetText(l, label);
            _x0 = LabelW + 6f;
            _width = width - _x0;
            int radius = Mathf.Max(2, (int)(height / 2f));
            for (int i = 0; i < MaxChips; i++)
            {
                _frames[i] = Frame(parent, "Pastille" + i, _x0, y, 10f, height, Color.clear, th.Accent, radius, 1);
                _texts[i] = Text(_frames[i].Root, "Texte", 0f, 0f, 10f, height, font, height < 20f ? 10f : 11f, th.Accent, TextAlignmentOptions.Center, 4f);
                _frames[i].Root.gameObject.SetActive(false);
            }
        }

        public void Apply(List<string> chips)
        {
            bool none = chips == null || chips.Count == 0;
            var items = none ? new List<string> { _empty } : chips;
            float x = 0f;
            int shown = 0;
            for (int i = 0; i < items.Count && shown < MaxChips; i++)
            {
                int remaining = items.Count - i;
                string text = items[i];
                float w = Measure(shown, text);
                bool last = remaining == 1;
                float plusW = last ? 0f : Measure(shown, "+" + (remaining - 1)) + ChipGap;
                if (x + w + plusW > _width || (shown == MaxChips - 1 && !last))
                {
                    text = "+" + remaining;
                    Place(shown++, text, x, Measure(shown - 1, text), none);
                    break;
                }
                Place(shown++, text, x, w, none);
                x += w + ChipGap;
            }
            for (int i = shown; i < MaxChips; i++) _frames[i].Root.gameObject.SetActive(false);
        }

        private float Measure(int index, string text) => Mathf.Min(_texts[index].GetPreferredValues(text).x + ChipPad, _width);

        private void Place(int index, string text, float x, float w, bool none)
        {
            var f = _frames[index];
            var color = none ? Hex(Core.NoteScale.NotAvailableHex) : _th.Accent;
            f.Root.gameObject.SetActive(true);
            f.Root.anchoredPosition = new Vector2(_x0 + x, f.Root.anchoredPosition.y);
            f.Root.sizeDelta = new Vector2(w, _height);
            f.Fill.rectTransform.sizeDelta = new Vector2(w, _height);
            f.Fill.color = none ? Color.clear : WithAlpha(_th.Accent, _dim ? 0.06f : 0.1f);
            if (f.Border != null)
            {
                f.Border.rectTransform.sizeDelta = new Vector2(w, _height);
                f.Border.color = WithAlpha(color, _dim ? 0.45f : 0.6f);
            }
            _texts[index].rectTransform.sizeDelta = new Vector2(w, _height);
            _texts[index].color = color;
            SetText(_texts[index], text);
        }
    }
}

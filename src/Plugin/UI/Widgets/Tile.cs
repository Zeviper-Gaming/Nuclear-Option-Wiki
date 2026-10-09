using NuclearOptionWiki.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI.Widgets
{
    /// <summary>Tuile voyant MFD : lampe, note en gros, éventuelle valeur réelle, libellé.</summary>
    internal sealed class Tile
    {
        private const float NoteSize = 28f, NoteSizeWithReal = 22f, NaSize = 20f, LabelSize = 11f, RealSize = 11f;

        private readonly ThemeDef _th;
        private readonly Frame _frame;
        private readonly Image _lamp, _glow;
        private readonly TextMeshProUGUI _note, _real, _label, _inv;
        private readonly bool _hasReal;

        public Tile(Transform parent, ThemeDef th, TMP_FontAsset font, float x, float y, float w, float h, bool hasReal)
        {
            _th = th;
            _hasReal = hasReal;
            _frame = Frame(parent, "Tuile", x, y, w, h, th.TileBg, th.TileBorder, th.TileRadius, th.TileBorderWidth);
            var root = _frame.Root;

            if (th.Lamp)
            {
                float lw = w * 0.42f, lx = (w - lw) / 2f;
                if (th.Glow) _glow = Box(root, "Halo", lx - 10f, -2f, lw + 20f, 20f, Color.clear, null);
                if (_glow != null) _glow.sprite = Glow();
                _lamp = Box(root, "Lampe", lx, 8f, lw, 4f, Color.clear, Rounded(2, 0));
            }

            if (th.TileLabelStrip.a > 0f)
            {
                Box(root, "Bandeau", 0f, h - 17f, w, 17f, th.TileLabelStrip, Rounded(Mathf.Max(th.TileRadius - 1, 0), 0));
                _label = Text(root, "Libellé", 2f, h - 16f, w - 4f, 15f, font, LabelSize, th.TileLabel, TextAlignmentOptions.Center, 6f, true);
                float top = hasReal ? 8f : 12f;
                _note = Text(root, "Note", 2f, top, w - 4f, hasReal ? 24f : 30f, font, hasReal ? NoteSizeWithReal : NoteSize, Color.white, TextAlignmentOptions.Center);
                if (hasReal) _real = Text(root, "Réel", 2f, 34f, w - 4f, 13f, font, RealSize, WithAlpha(th.Body, 0.8f), TextAlignmentOptions.Center);
            }
            else
            {
                _label = Text(root, "Libellé", 4f, h - 19f, w - 8f, 13f, font, LabelSize, th.TileLabel, TextAlignmentOptions.Center, 6f, true);
                if (hasReal)
                {
                    _real = Text(root, "Réel", 2f, h - 33f, w - 4f, 13f, font, RealSize, WithAlpha(th.Body, 0.8f), TextAlignmentOptions.Center);
                    _note = Text(root, "Note", 2f, h - 58f, w - 4f, 25f, font, NoteSizeWithReal, Color.white, TextAlignmentOptions.Bottom);
                }
                else
                {
                    _note = Text(root, "Note", 2f, h - 52f, w - 4f, 31f, font, NoteSize, Color.white, TextAlignmentOptions.Bottom);
                }
            }
            _inv = Text(root, "Inv", w - 34f, 2f, 30f, 11f, font, 9f, WithAlpha(th.Body, 0.75f), TextAlignmentOptions.Right, 8f);
            SetText(_inv, "inv.");
            _inv.gameObject.SetActive(false);
        }

        public void Apply(TileField f)
        {
            bool na = f.ColorIndex == null;
            Color c = Hex(NoteScale.Hex(f.ColorIndex));

            SetText(_label, f.Label);
            _inv.gameObject.SetActive(f.Inverted);

            if (na)
            {
                _note.fontSize = NaSize;
                _note.characterSpacing = 8f;
                SetText(_note, "N/A");
            }
            else
            {
                _note.fontSize = _hasReal ? NoteSizeWithReal : NoteSize;
                _note.characterSpacing = 0f;
                SetText(_note, f.Note + "<size=12><alpha=#A6>" + f.Unit);
            }
            if (_real != null) SetText(_real, f.Real ?? "");

            if (_th.TileFilledByNote)
            {
                _frame.SetColors(na ? _th.TileNaBg : Mix(c, _th.TileBg, 0.2f), na ? _th.TileNaBorder : Mix(c, Color.black, 0.75f));
                _note.color = na ? c : Mix(c, Color.white, 0.85f);
            }
            else
            {
                _note.color = c;
            }

            if (_lamp != null) _lamp.color = na ? WithAlpha(c, 0.5f) : c;
            if (_glow != null) _glow.color = na ? Color.clear : WithAlpha(c, 0.45f);
        }
    }
}

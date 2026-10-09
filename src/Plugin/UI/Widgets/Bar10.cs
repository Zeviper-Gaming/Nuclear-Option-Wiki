using NuclearOptionWiki.Core;
using TMPro;
using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI.Widgets
{
    /// <summary>Barre horizontale à 10 niveaux : libellé | 10 segments | note /10 | valeur réelle.</summary>
    internal sealed class Bar10
    {
        // Colonnes de la maquette : 88 | segments | 64 | 96, écart 10, largeur totale 572.
        public const float Height = 20f;
        private const float LabelW = 88f, NoteW = 64f, RealW = 96f, Gap = 10f, SegGap = 3f, SegH = 16f;

        private readonly ThemeDef _th;
        private readonly Frame[] _segs = new Frame[10];
        private readonly TextMeshProUGUI _label, _note, _real;

        public Bar10(Transform parent, ThemeDef th, TMP_FontAsset font, float y, float width)
        {
            _th = th;
            float segZone = width - LabelW - NoteW - RealW - 3f * Gap;
            float segW = (segZone - 9f * SegGap) / 10f;
            float segH = th.SegOnDimmed ? 18f : SegH;
            float segY = y + (Height - segH) / 2f;

            _label = Text(parent, "Libellé", 0f, y, LabelW, Height, font, 13f, th.BarLabel, TextAlignmentOptions.MidlineLeft, 0f, true);
            float x = LabelW + Gap;
            for (int i = 0; i < 10; i++)
                _segs[i] = Frame(parent, "Segment" + i, x + i * (segW + SegGap), segY, segW, segH, th.SegOff, th.SegOffBorder, th.SegRadius, 1);
            x += segZone + Gap;
            _note = Text(parent, "Note", x, y - 3f, NoteW, Height + 6f, font, 20f, Color.white, TextAlignmentOptions.MidlineRight);
            x += NoteW + Gap;
            _real = Text(parent, "Réel", x, y, RealW, Height, font, 13f, th.BarReal, TextAlignmentOptions.MidlineRight);
        }

        public void Apply(BarField f)
        {
            Color c = Hex(NoteScale.Hex(f.ColorIndex));
            SetText(_label, f.Label);
            SetText(_note, f.ColorIndex == null ? "N/A" : f.Note + "<size=12><alpha=#B3>" + f.Unit);
            _note.color = c;
            SetText(_real, f.Real);
            for (int i = 0; i < 10; i++)
            {
                bool on = i < f.LitSegments;
                if (!on) _segs[i].SetColors(_th.SegOff, _th.SegOffBorder);
                else if (_th.SegOnDimmed) _segs[i].SetColors(Mix(c, Color.black, 0.7f), c);
                else _segs[i].SetColors(c, c);
            }
        }
    }
}

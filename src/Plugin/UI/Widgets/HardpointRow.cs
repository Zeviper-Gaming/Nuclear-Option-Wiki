using NuclearOptionWiki.Core;
using TMPro;
using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI.Widgets
{
    /// <summary>
    /// Rangée d'emplacement (plan avions 7.3, largeur 572, hauteur 20) :
    /// nom (190) · « ×2 » (30) · SOUTE/EXT (46) · classe S/M/H (22) · charge (62) · pastilles d'acronymes (reste) · « (12) » (34).
    /// </summary>
    internal sealed class HardpointRow
    {
        public const float Height = 20f;
        private const int MaxCats = 9;
        private const float NameW = 190f, PtsW = 30f, BayW = 46f, ClassW = 22f, ChargeW = 62f, CountW = 34f, Gap = 4f, CatGap = 3f, CatH = 14f;

        private readonly ThemeDef _th;
        private readonly RectTransform _root;
        private readonly TextMeshProUGUI _name, _pts, _bay, _class, _charge, _count, _more;
        private readonly Frame _bayFrame, _classFrame;
        private readonly Frame[] _cats = new Frame[MaxCats];
        private readonly TextMeshProUGUI[] _catTexts = new TextMeshProUGUI[MaxCats];
        private readonly float _catX, _catW;

        public HardpointRow(Transform parent, ThemeDef th, TMP_FontAsset font, float y, float width)
        {
            _th = th;
            _root = Place(Node("Emplacement", parent), 0f, y, width, Height);
            Color body = th.Body;
            float x = 0f;
            _name = Text(_root, "Nom", x, 0f, NameW, Height, font, 12f, body, TextAlignmentOptions.MidlineLeft);
            x += NameW + Gap;
            _pts = Text(_root, "Points", x, 0f, PtsW, Height, font, 12f, body, TextAlignmentOptions.MidlineRight);
            x += PtsW + Gap;
            _bayFrame = Frame(_root, "Soute", x, 3f, BayW, CatH, Color.clear, th.Chip, 2, 1);
            _bay = Text(_bayFrame.Root, "Texte", 0f, 0f, BayW, CatH, font, 9.5f, th.Chip, TextAlignmentOptions.Center, 4f);
            x += BayW + Gap;
            _classFrame = Frame(_root, "Classe", x, 3f, ClassW, CatH, Color.clear, th.Accent, 2, 1);
            _class = Text(_classFrame.Root, "Texte", 0f, 0f, ClassW, CatH, font, 10f, th.Accent, TextAlignmentOptions.Center);
            x += ClassW + Gap;
            _charge = Text(_root, "Charge", x, 0f, ChargeW, Height, font, 12f, body, TextAlignmentOptions.MidlineRight);
            x += ChargeW + Gap;
            _catX = x;
            _catW = width - x - CountW - Gap;
            for (int i = 0; i < MaxCats; i++)
            {
                _cats[i] = Frame(_root, "Catégorie" + i, x, 3f, 10f, CatH, WithAlpha(th.Accent, 0.16f), Color.clear, 2, 0);
                _catTexts[i] = Text(_cats[i].Root, "Texte", 0f, 0f, 10f, CatH, font, 10f, th.Accent, TextAlignmentOptions.Center, 2f);
                _cats[i].Root.gameObject.SetActive(false);
            }
            _count = Text(_root, "Nombre", width - CountW, 0f, CountW, Height, font, 12f, WithAlpha(body, 0.75f), TextAlignmentOptions.MidlineRight);
            _more = Text(_root, "Suite", 0f, 0f, width, Height, font, 11f, WithAlpha(body, 0.75f), TextAlignmentOptions.MidlineLeft, 8f);
        }

        /// <summary>null : rangée vide (rien d'affiché).</summary>
        public void Apply(HardpointRowField f)
        {
            _root.gameObject.SetActive(f != null);
            if (f == null) return;
            bool more = f.MoreText != null;
            _more.gameObject.SetActive(more);
            foreach (var t in new[] { _name, _pts, _charge, _count }) t.gameObject.SetActive(!more);
            _bayFrame.Root.gameObject.SetActive(!more);
            _classFrame.Root.gameObject.SetActive(!more);
            if (more)
            {
                SetText(_more, f.MoreText);
                for (int i = 0; i < MaxCats; i++) _cats[i].Root.gameObject.SetActive(false);
                return;
            }
            SetText(_name, (f.ExcludesOthers ? "⊘ " : "") + f.Name);
            SetText(_pts, f.Points);
            SetText(_bay, f.Bay);
            bool ext = f.Bay != "SOUTE";
            _bay.alpha = ext ? 0.6f : 1f;
            if (_bayFrame.Border != null) _bayFrame.Border.color = WithAlpha(_th.Chip, ext ? 0.6f : 1f);
            SetText(_class, f.Class);
            SetText(_charge, f.Charge);
            SetText(_count, f.Count);

            float x = 0f;
            int shown = 0;
            for (int i = 0; i < f.Categories.Count && shown < MaxCats; i++)
            {
                string text = f.Categories[i];
                float w = _catTexts[shown].GetPreferredValues(text).x + 8f;
                int remaining = f.Categories.Count - i;
                float plusW = remaining > 1 ? _catTexts[shown].GetPreferredValues("+" + (remaining - 1)).x + 8f + CatGap : 0f;
                if (x + w + plusW > _catW)
                {
                    text = "+" + remaining;
                    w = _catTexts[shown].GetPreferredValues(text).x + 8f;
                    PlaceCat(shown++, text, x, w);
                    break;
                }
                PlaceCat(shown++, text, x, w);
                x += w + CatGap;
            }
            for (int i = shown; i < MaxCats; i++) _cats[i].Root.gameObject.SetActive(false);
        }

        private void PlaceCat(int i, string text, float x, float w)
        {
            var c = _cats[i];
            c.Root.gameObject.SetActive(true);
            c.Root.anchoredPosition = new Vector2(_catX + x, c.Root.anchoredPosition.y);
            c.Root.sizeDelta = new Vector2(w, CatH);
            c.Fill.rectTransform.sizeDelta = new Vector2(w, CatH);
            _catTexts[i].rectTransform.sizeDelta = new Vector2(w, CatH);
            SetText(_catTexts[i], text);
        }
    }
}

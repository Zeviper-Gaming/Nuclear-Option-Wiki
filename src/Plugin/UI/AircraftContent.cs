using NuclearOptionWiki.Core;
using NuclearOptionWiki.UI.Widgets;
using TMPro;
using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI
{
    /// <summary>
    /// Contenu « appareil » du panneau (plan avions 7.3, amendé v3.3 ; maquette docs/maquettes/fiche-appareil-mfd.html).
    /// Coordonnées dans la zone de contenu (panneau − marge de 14 px) : y panneau = y contenu + 14. Pas de séparateur.
    /// </summary>
    internal sealed class AircraftContent
    {
        private const float W = 572f;
        private const float TitleH = 16f, ChipH = 20f, StatH = 40f, StatGap = 6f, TileH = 64f, TileGap = 6f;
        private static readonly Color PersonalColor = Hex("#FFC24A");

        public readonly RectTransform Root;
        private readonly ThemeDef _th;
        private readonly TextMeshProUGUI _title;
        private readonly Frame[] _chipFrames = new Frame[3];
        private readonly TextMeshProUGUI[] _chipTexts = new TextMeshProUGUI[3];
        private readonly ChipRow _roles, _tags;
        private readonly Bar10[] _bars = new Bar10[AircraftFicheModel.BarCount];
        private readonly StatCell[] _stats = new StatCell[AircraftFicheModel.StatCount];
        private readonly TextMeshProUGUI _summary, _personal, _description;
        private readonly HardpointRow[] _rows = new HardpointRow[AircraftFicheModel.RowCount];
        private readonly Tile[] _tiles = new Tile[AircraftFicheModel.TileCount];

        public AircraftContent(Transform parent, ThemeDef th, TMP_FontAsset font, float x, float y)
        {
            _th = th;
            Root = Place(Node("Appareil", parent), x, y, W, WikiPanel.PanelH - 2f * WikiPanel.Pad);
            var c = Root;

            ZoneTitle.Create(c, th, font, "Appareil", 0f, W, TitleH);                       // panneau 14 → 30
            _title = Text(c, "Nom", 0f, 19f, W, 36f, font, 30f, th.Title, TextAlignmentOptions.MidlineLeft, 2f);  // 30 → 72
            for (int i = 0; i < 3; i++)
            {
                _chipFrames[i] = Frame(c, "Puce" + i, 0f, 27f, 10f, ChipH, Color.clear, i == 2 ? th.Accent : th.Chip, 3, 1);
                _chipTexts[i] = Text(_chipFrames[i].Root, "Texte", 0f, 0f, 10f, ChipH, font, i == 2 ? 13f : 11f,
                                     i == 2 ? th.Accent : th.Chip, TextAlignmentOptions.Center, 10f, true);
            }
            _roles = new ChipRow(c, th, font, "Rôles", "N/A", 63f, W, 20f, false);           // 76 → 98

            for (int i = 0; i < _bars.Length; i++) _bars[i] = new Bar10(c, th, font, 92f + i * 27f, W);   // 102 → 210

            float statW = (W - 2f * StatGap) / 3f;
            for (int i = 0; i < _stats.Length; i++)                                            // 226 → 358
                _stats[i] = new StatCell(c, th, font, (i % 3) * (statW + StatGap), 212f + (i / 3) * (StatH + StatGap), statW, StatH);

            ZoneTitle.Create(c, th, font, "Emports", 356f, W, TitleH);                        // 370 → 386
            _summary = Text(c, "Synthèse", 0f, 378f, W, 17f, font, 13f, th.Sub, TextAlignmentOptions.MidlineLeft);  // 392 → 409
            for (int i = 0; i < _rows.Length; i++) _rows[i] = new HardpointRow(c, th, font, 395f + i * HardpointRow.Height, W);  // 409 → 589

            ZoneTitle.Create(c, th, font, "Signatures · défenses", 577f, W, TitleH);         // 591 → 607
            float tileW = (W - (AircraftFicheModel.TileCount - 1) * TileGap) / AircraftFicheModel.TileCount;
            for (int i = 0; i < _tiles.Length; i++)                                            // 607 → 671
                _tiles[i] = new Tile(c, th, font, i * (tileW + TileGap), 593f, tileW, TileH, i != 3 && i != 4);

            _personal = Text(c, "Texte perso", 0f, 663f, W, 28f, font, 12f, PersonalColor, TextAlignmentOptions.TopLeft);   // 677 → 705
            Wrap(_personal, 10f, 12f);
            _tags = new ChipRow(c, th, font, "Tags", "—", 695f, W, 18f, true);               // 709 → 727
            _description = Text(c, "Description", 0f, 717f, W, 54f, font, 12f, th.Sub, TextAlignmentOptions.TopLeft);      // 731 → 785
            Wrap(_description, 10f, 12f);
        }

        public void SetActive(bool active)
        {
            if (Root.gameObject.activeSelf != active) Root.gameObject.SetActive(active);
        }

        public void Show(AircraftFicheModel m)
        {
            SetText(_title, m.Title);
            LayoutChips(new[] { m.ChipType, m.ChipSource, m.ChipSize }, m.Unreferenced);
            _roles.Apply(m.Roles);
            for (int i = 0; i < _bars.Length; i++) _bars[i].Apply(m.Bars[i]);
            for (int i = 0; i < _stats.Length; i++) _stats[i].Apply(m.Stats[i]);
            SetText(_summary, m.Summary);
            for (int i = 0; i < _rows.Length; i++) _rows[i].Apply(m.Rows[i]);
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Apply(m.Tiles[i]);
            bool hasPersonal = m.PersonalText != null;
            SetText(_personal, hasPersonal ? m.PersonalText : Format.Unknown);
            _personal.color = hasPersonal ? PersonalColor : Hex(NoteScale.NotAvailableHex);
            _tags.Apply(m.Tags);
            SetText(_description, m.Description);
        }

        /// <summary>Texte sur plusieurs lignes, taille automatique entre min et max, « … » si trop long (TMP auto-size).</summary>
        private static void Wrap(TextMeshProUGUI t, float min, float max)
        {
            t.enableWordWrapping = true;
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.lineSpacing = -8f;
        }

        /// <summary>Puces alignées à droite de la ligne du nom (type, source, taille), largeur ajustée au texte.</summary>
        private void LayoutChips(string[] texts, bool unreferenced)
        {
            float right = W;
            for (int i = texts.Length - 1; i >= 0; i--)
            {
                string t = texts[i];
                bool show = !string.IsNullOrEmpty(t);
                _chipFrames[i].Root.gameObject.SetActive(show);
                if (!show) continue;
                SetText(_chipTexts[i], t);
                Color color = i == 2 ? _th.Accent : unreferenced ? _th.Unknown : _th.Chip;
                _chipTexts[i].color = color;
                if (_chipFrames[i].Border != null) _chipFrames[i].Border.color = color;
                float w = Mathf.Min(_chipTexts[i].GetPreferredValues(t.ToUpperInvariant()).x + 16f, 190f);
                right -= w;
                var rt = _chipFrames[i].Root;
                rt.anchoredPosition = new Vector2(right, rt.anchoredPosition.y);
                rt.sizeDelta = new Vector2(w, ChipH);
                _chipFrames[i].Fill.rectTransform.sizeDelta = new Vector2(w, ChipH);
                if (_chipFrames[i].Border != null) _chipFrames[i].Border.rectTransform.sizeDelta = new Vector2(w, ChipH);
                _chipTexts[i].rectTransform.sizeDelta = new Vector2(w, ChipH);
                right -= 6f;
            }
            _title.rectTransform.sizeDelta = new Vector2(Mathf.Max(right - 10f, 120f), _title.rectTransform.sizeDelta.y);
        }
    }
}

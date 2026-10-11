using NuclearOptionWiki.Core;
using NuclearOptionWiki.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI
{
    /// <summary>
    /// La fiche : panneau fixe 600 × 840 px (référence 1920×1080) en (12, 12), sur un Canvas dédié
    /// sans GraphicRaycaster (aucun clic intercepté). Construite une fois par thème ; Show() ne fait
    /// que mettre à jour textes et couleurs.
    /// </summary>
    internal sealed class WikiPanel
    {
        public const float PanelW = 600f, PanelH = 840f, Margin = 12f, Pad = 14f;
        private const float InnerW = PanelW - 2f * Pad;   // 572
        private const float Gap = 8f, ZoneGap = 10f;
        private const float TileH = 72f;   // plan v1.1 (D32) : 78 → 72 px pour tenir dans 840 px

        private static WikiPanel _instance;
        public static WikiPanel Instance => _instance ?? (_instance = new WikiPanel());

        private GameObject _canvasGo;
        private RectTransform _panel;
        private Frame _panelFrame;
        private ThemeDef _theme;
        private WikiTheme _themeId;
        private TMP_FontAsset _font;
        private bool _built;
        private float _opacity = 0.9f;
        private float _scale = 1f;

        private TextMeshProUGUI _title, _subtitle, _footerKey, _footerWarn;
        private readonly Frame[] _chipFrames = new Frame[2];
        private readonly TextMeshProUGUI[] _chipTexts = new TextMeshProUGUI[2];
        private readonly Bar10[] _bars = new Bar10[FicheModel.BarCount];
        private ProfileRow _profile;
        private readonly StatCell[] _stats = new StatCell[FicheModel.StatCount];
        private readonly Tile[] _ground = new Tile[FicheModel.AirToGroundCount];
        private readonly Tile[] _air = new Tile[FicheModel.AirToAirCount];
        private readonly Tile[] _defense = new Tile[FicheModel.DefenseCount];

        private FicheModel _current;
        private string _footerKeyText = "", _footerWarnText = "";

        private WikiPanel() { }

        public bool Visible => _panel != null && _panel.gameObject.activeSelf;

        /// <summary>Thème et police ; reconstruit la fiche si l'un des deux change.</summary>
        public void Configure(WikiTheme theme, TMP_FontAsset font)
        {
            if (_built && theme == _themeId && font == _font) return;
            bool wasVisible = Visible;
            _themeId = theme;
            _font = font;
            _theme = ThemeDef.For(theme);
            Build();
            SetVisible(wasVisible);
            if (_current != null) Show(_current);
            SetFooter(_footerKeyText, _footerWarnText);
        }

        public void SetScale(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.75f, 1.5f);
            if (_panel != null) _panel.localScale = new Vector3(_scale, _scale, 1f);
        }

        public void SetOpacity(float opacity)
        {
            _opacity = Mathf.Clamp(opacity, 0.5f, 1f);
            if (_panelFrame != null) _panelFrame.Fill.color = WithAlpha(_theme.PanelBg, _opacity);
        }

        public void SetVisible(bool visible)
        {
            if (_panel != null && _panel.gameObject.activeSelf != visible) _panel.gameObject.SetActive(visible);
        }

        /// <summary>Pied de fiche : touche active à droite, avertissement éventuel à gauche.</summary>
        public void SetFooter(string keyText, string warning)
        {
            _footerKeyText = keyText ?? "";
            _footerWarnText = warning ?? "";
            if (!_built) return;
            SetText(_footerKey, _footerKeyText);
            SetText(_footerWarn, _footerWarnText);
        }

        public void Show(FicheModel m)
        {
            _current = m;
            if (!_built || m == null) return;

            SetText(_title, m.Title);
            SetText(_subtitle, m.Subtitle);
            _subtitle.color = m.Unreferenced ? _theme.Unknown : _theme.Sub;
            LayoutChips(m.Chip1, m.Chip2);

            _profile.Apply(m.Profile);
            for (int i = 0; i < _bars.Length; i++) _bars[i].Apply(m.Bars[i]);
            for (int i = 0; i < _stats.Length; i++) _stats[i].Apply(m.Stats[i]);
            for (int i = 0; i < _ground.Length; i++) _ground[i].Apply(m.AirToGround[i]);
            for (int i = 0; i < _air.Length; i++) _air[i].Apply(m.AirToAir[i]);
            for (int i = 0; i < _defense.Length; i++) _defense[i].Apply(m.Defense[i]);
        }

        public void Destroy()
        {
            if (_canvasGo != null) Object.Destroy(_canvasGo);
            _canvasGo = null;
            _panel = null;
            _built = false;
        }

        private void EnsureCanvas()
        {
            if (_canvasGo != null) return;
            _canvasGo = new GameObject("NuclearOptionWiki.Canvas", typeof(RectTransform)) { layer = 5 };
            Object.DontDestroyOnLoad(_canvasGo);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            // Pas de GraphicRaycaster : la fiche ne bloque jamais la souris.
        }

        private void Build()
        {
            EnsureCanvas();
            if (_panel != null) Object.Destroy(_panel.gameObject);

            var th = _theme;
            var font = _font;
            _panel = Place(Node("Fiche", _canvasGo.transform), Margin, Margin, PanelW, PanelH);
            _panel.localScale = new Vector3(_scale, _scale, 1f);
            _panelFrame = Frame(_panel, "Panneau", 0f, 0f, PanelW, PanelH, WithAlpha(th.PanelBg, _opacity), th.PanelBorder, th.PanelRadius, th.PanelBorderWidth);
            var c = Place(Node("Contenu", _panel), Pad, Pad, InnerW, PanelH - 2f * Pad);

            // --- Zone 1 : stats globales ---
            float y = 0f;
            ZoneTitle.Create(c, th, font, "Stats globales", y, InnerW);
            y += ZoneTitle.Height + Gap;
            _title = Text(c, "Nom", 0f, y, InnerW, 36f, font, 30f, th.Title, TextAlignmentOptions.MidlineLeft, 2f);
            for (int i = 0; i < 2; i++)
            {
                _chipFrames[i] = Frame(c, "Puce" + i, 0f, y + 8f, 10f, 20f, Color.clear, th.Chip, 3, 1);
                _chipTexts[i] = Text(_chipFrames[i].Root, "Texte", 0f, 0f, 10f, 20f, font, 11f, th.Chip, TextAlignmentOptions.Center, 10f, true);
            }
            y += 36f + Gap;
            _subtitle = Text(c, "Sous-titre", 0f, y, InnerW, 17f, font, 13f, th.Sub, TextAlignmentOptions.MidlineLeft);
            y += 17f + Gap;
            _profile = new ProfileRow(c, th, font, y, InnerW);
            y += ProfileRow.Height + Gap;
            for (int i = 0; i < _bars.Length; i++)
            {
                _bars[i] = new Bar10(c, th, font, y, InnerW);
                y += Bar10.Height + Gap;
            }
            // Plan v1.1 (D28) : 6 cellules, 3 colonnes × 2 rangées.
            float statW = (InnerW - 2f * 6f) / 3f;
            for (int i = 0; i < _stats.Length; i++)
            {
                int row = i / 3, col = i % 3;
                _stats[i] = new StatCell(c, th, font, col * (statW + 6f), y + row * (StatCell.Height + 6f), statW);
            }
            y += 2f * StatCell.Height + 6f;

            // --- Zone 2 : air-sol et air-air ---
            y = Divider(c, th, y);
            ZoneTitle.Create(c, th, font, "Capacité air-sol (CAS)", y, InnerW);
            y += ZoneTitle.Height + Gap;
            y = TileGrid(c, th, font, _ground, 4, y, TileH, false);
            y += Gap;
            ZoneTitle.Create(c, th, font, "Capacité air-air (AA)", y, InnerW);
            y += ZoneTitle.Height + Gap;
            y = TileGrid(c, th, font, _air, 3, y, TileH, false);

            // --- Zone 3 : interception et défenses ---
            y = Divider(c, th, y);
            ZoneTitle.Create(c, th, font, "Interception · défenses", y, InnerW);
            y += ZoneTitle.Height + Gap;
            TileGrid(c, th, font, _defense, 5, y, TileH, true);

            // --- Pied : touche active et avertissement ---
            float footY = PanelH - 2f * Pad - 14f;
            _footerWarn = Text(c, "Avertissement", 0f, footY, InnerW * 0.72f, 14f, font, 11f, th.Warning, TextAlignmentOptions.MidlineLeft, 10f, true);
            _footerKey = Text(c, "Touche", InnerW * 0.72f, footY, InnerW * 0.28f, 14f, font, 11f, WithAlpha(th.Body, 0.75f), TextAlignmentOptions.MidlineRight, 10f, true);

            _built = true;
        }

        /// <summary>Séparateur entre zones (thème A) ; renvoie le y de départ de la zone suivante.</summary>
        private static float Divider(Transform parent, ThemeDef th, float y)
        {
            y += ZoneGap;
            if (th.Divider.a > 0f)
            {
                Box(parent, "Séparateur", 0f, y, InnerW, 1f, th.Divider, null);
                y += 1f + ZoneGap;
            }
            return y;
        }

        /// <summary>Grille de tuiles sur une ou plusieurs rangées ; renvoie le y sous la grille.</summary>
        private static float TileGrid(Transform parent, ThemeDef th, TMP_FontAsset font, Tile[] tiles, int columns, float y, float h, bool withReal)
        {
            float w = (InnerW - (columns - 1) * Gap) / columns;
            int rows = (tiles.Length + columns - 1) / columns;
            for (int i = 0; i < tiles.Length; i++)
            {
                int row = i / columns, col = i % columns;
                tiles[i] = new Tile(parent, th, font, col * (w + Gap), y + row * (h + Gap), w, h, withReal);
            }
            return y + rows * h + (rows - 1) * Gap;
        }

        /// <summary>Puces alignées à droite de la ligne du nom, largeur ajustée au texte.</summary>
        private void LayoutChips(string chip1, string chip2)
        {
            string[] texts = { chip1, chip2 };
            float right = InnerW;
            for (int i = 1; i >= 0; i--)
            {
                string t = texts[i];
                bool show = !string.IsNullOrEmpty(t);
                _chipFrames[i].Root.gameObject.SetActive(show);
                if (!show) continue;
                SetText(_chipTexts[i], t);
                float w = Mathf.Min(_chipTexts[i].GetPreferredValues(t.ToUpperInvariant()).x + 16f, 200f);
                right -= w;
                var rt = _chipFrames[i].Root;
                rt.anchoredPosition = new Vector2(right, rt.anchoredPosition.y);
                rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
                _chipFrames[i].Fill.rectTransform.sizeDelta = new Vector2(w, rt.sizeDelta.y);
                if (_chipFrames[i].Border != null) _chipFrames[i].Border.rectTransform.sizeDelta = new Vector2(w, rt.sizeDelta.y);
                _chipTexts[i].rectTransform.sizeDelta = new Vector2(w, rt.sizeDelta.y);
                right -= 6f;
            }
            float titleW = Mathf.Max(right - 10f, 120f);
            _title.rectTransform.sizeDelta = new Vector2(titleW, _title.rectTransform.sizeDelta.y);
        }
    }
}

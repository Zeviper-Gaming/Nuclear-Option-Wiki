using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOptionWiki.UI
{
    /// <summary>
    /// Outils de construction uGUI. Tout est positionné en absolu depuis le coin haut-gauche du parent,
    /// en pixels de la résolution de référence 1920×1080. Aucun élément n'intercepte la souris.
    /// </summary>
    internal static class UiKit
    {
        private const int UiLayer = 5;
        private static readonly Dictionary<long, Sprite> RoundedCache = new Dictionary<long, Sprite>();
        private static Sprite _glow;

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color, Sprite sprite = null)
        {
            var rt = Place(Node(name, parent), x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        /// <summary>Rectangle arrondi avec fond et bordure (deux Image superposées).</summary>
        public static Frame Frame(Transform parent, string name, float x, float y, float w, float h,
            Color fill, Color border, int radius, int borderWidth)
        {
            var root = Place(Node(name, parent), x, y, w, h);
            var f = new Frame
            {
                Root = root,
                Fill = Box(root, "Fond", 0, 0, w, h, fill, Rounded(radius, 0)),
                Border = borderWidth > 0 ? Box(root, "Bord", 0, 0, w, h, border, Rounded(radius, borderWidth)) : null,
            };
            return f;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, float x, float y, float w, float h,
            TMP_FontAsset font, float size, Color color, TextAlignmentOptions align,
            float spacing = 0f, bool upper = false)
        {
            var rt = Place(Node(name, parent), x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.characterSpacing = spacing;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.richText = true;
            t.raycastTarget = false;
            t.margin = Vector4.zero;
            if (upper) t.fontStyle = FontStyles.UpperCase;
            return t;
        }

        private static readonly Dictionary<char, string> AsciiFallback = new Dictionary<char, string>
        {
            { '·', "-" }, { '—', "-" }, { '⊘', "(x)" }, { '×', "x" }, { '–', "-" }, { '²', "2" }, { '°', "deg" }, { '€', "EUR" },
            { 'é', "e" }, { 'è', "e" }, { 'ê', "e" }, { 'ë', "e" }, { 'à', "a" }, { 'â', "a" }, { 'ç', "c" },
            { 'î', "i" }, { 'ï', "i" }, { 'ô', "o" }, { 'û', "u" }, { 'ù', "u" }, { 'ü', "u" },
            { 'É', "E" }, { 'È', "E" }, { 'Ê', "E" }, { 'À', "A" }, { 'Ç', "C" }, { 'Ô', "O" }, { 'Î', "I" },
        };
        private static readonly Dictionary<TMP_FontAsset, Dictionary<char, bool>> GlyphCache = new Dictionary<TMP_FontAsset, Dictionary<char, bool>>();

        /// <summary>
        /// Affecte un texte en remplaçant les caractères absents de la police (et de ses polices de secours)
        /// par un équivalent ASCII, pour éviter les carrés vides.
        /// </summary>
        public static void SetText(TMP_Text t, string s)
        {
            s = s ?? "";
            var font = t.font;
            if (font != null)
            {
                if (!GlyphCache.TryGetValue(font, out var known)) GlyphCache[font] = known = new Dictionary<char, bool>();
                System.Text.StringBuilder sb = null;
                for (int i = 0; i < s.Length; i++)
                {
                    char ch = s[i];
                    if (ch < 128 || !AsciiFallback.TryGetValue(ch, out var repl)) { sb?.Append(ch); continue; }
                    if (!known.TryGetValue(ch, out bool has)) known[ch] = has = font.HasCharacter(ch, true, true);
                    if (has) { sb?.Append(ch); continue; }
                    if (sb == null) sb = new System.Text.StringBuilder(s, 0, i, s.Length + 8);
                    sb.Append(repl);
                }
                if (sb != null) s = sb.ToString();
            }
            t.text = s;
        }

        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a *= alpha;
            return c;
        }

        public static Color Rgba(float r, float g, float b, float a) => new Color(r / 255f, g / 255f, b / 255f, a);

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>Mélange linéaire, équivalent de color-mix(in srgb, a t%, b) de la maquette.</summary>
        public static Color Mix(Color a, Color b, float tOfA) => Color.Lerp(b, a, tOfA);

        /// <summary>
        /// Sprite 9-slice d'un rectangle arrondi, généré à l'exécution et mis en cache.
        /// borderWidth = 0 : rectangle plein ; sinon : contour seul de cette épaisseur.
        /// </summary>
        public static Sprite Rounded(int radius, int borderWidth)
        {
            long key = ((long)radius << 16) | (uint)borderWidth;
            if (RoundedCache.TryGetValue(key, out var cached) && cached != null) return cached;

            int m = Mathf.Max(radius, borderWidth, 1);
            int n = 2 * m + 5;
            int slice = m + 2;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[n * n];
            float half = n / 2f;
            for (int py = 0; py < n; py++)
            {
                for (int px = 0; px < n; px++)
                {
                    float qx = Mathf.Abs(px + 0.5f - half) - (half - radius);
                    float qy = Mathf.Abs(py + 0.5f - half) - (half - radius);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float d = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (borderWidth > 0) a *= Mathf.Clamp01(0.5f + d + borderWidth);
                    pixels[py * n + px] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            RoundedCache[key] = sprite;
            return sprite;
        }

        /// <summary>Halo radial doux (lampes des voyants).</summary>
        public static Sprite Glow()
        {
            if (_glow != null) return _glow;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f - n / 2f) / (n / 2f);
                float dy = (y + 0.5f - n / 2f) / (n / 2f);
                float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(t * t * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            _glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            _glow.hideFlags = HideFlags.HideAndDontSave;
            return _glow;
        }
    }

    /// <summary>Rectangle arrondi : fond + bordure.</summary>
    internal sealed class Frame
    {
        public RectTransform Root;
        public Image Fill;
        public Image Border;

        public void SetColors(Color fill, Color border)
        {
            Fill.color = fill;
            if (Border != null) Border.color = border;
        }
    }
}

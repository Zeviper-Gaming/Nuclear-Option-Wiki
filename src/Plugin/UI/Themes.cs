using UnityEngine;
using static NuclearOptionWiki.UI.UiKit;

namespace NuclearOptionWiki.UI
{
    /// <summary>Palette et formes d'un thème. Valeurs reprises des classes .va / .vb / .vc de docs/maquettes/fiche-arme-mfd.html.</summary>
    internal sealed class ThemeDef
    {
        public string Name;

        // Panneau
        public Color PanelBg;            // l'alpha est remplacé par le réglage « Opacité du fond »
        public Color PanelBorder;
        public int PanelRadius;
        public int PanelBorderWidth;
        public Color Body;               // couleur de texte par défaut (valeurs réelles, pied de fiche)

        // Titres de zone
        public Color ZoneTitle;
        public Color ZoneTitleBg;        // alpha 0 = pas de bandeau
        public Color ZoneTitleAccent;    // alpha 0 = pas de filet à gauche
        public bool ZoneTitleCentered;
        public Color Divider;            // alpha 0 = pas de séparateur entre zones

        // En-tête
        public Color Title;
        public Color Chip;
        public Color Sub;

        // Barres
        public Color BarLabel;
        public Color BarReal;
        public Color SegOff;
        public Color SegOffBorder;
        public bool SegOnDimmed;         // B : segment allumé assombri, bord à la couleur pleine
        public int SegRadius;

        // Cellules de stats
        public Color StatBg;
        public Color StatBorder;
        public Color StatLabel;
        public Color StatValue;
        public int StatRadius;

        // Tuiles voyants
        public Color TileBg;
        public Color TileBorder;
        public Color TileLabel;
        public Color TileLabelStrip;     // alpha 0 = pas de bandeau sous le libellé (B)
        public int TileRadius;
        public int TileBorderWidth;
        public bool TileFilledByNote;    // B : toute la tuile prend la couleur de la note
        public Color TileNaBg;           // B : fond d'une tuile N/A
        public Color TileNaBorder;
        public bool Lamp;                // A, C : lampe en haut de la tuile
        public bool Glow;                // halo des lampes et segments

        public Color Accent;             // fiche appareil : compteurs, classes, pastilles (couleur neutre)
        public Color Unknown;            // mention « arme non référencée »
        public Color Warning;            // avertissement de touche

        public static ThemeDef For(WikiTheme theme)
        {
            switch (theme)
            {
                case WikiTheme.Annonciateur: return Annonciateur();
                case WikiTheme.HangarNO: return HangarNO();
                default: return MfdPhosphore();
            }
        }

        private static ThemeDef MfdPhosphore() => new ThemeDef
        {
            Name = "MfdPhosphore",
            PanelBg = Rgba(3, 7, 5, 0.9f), PanelBorder = Hex("#1F2B24"), PanelRadius = 10, PanelBorderWidth = 1,
            Body = Hex("#9EF7AA"),
            ZoneTitle = Hex("#46FF5A", 0.85f), ZoneTitleBg = Color.clear, ZoneTitleAccent = Color.clear,
            Divider = Hex("#16201A"),
            Title = Hex("#46FF5A"), Chip = Hex("#6FDC7D"), Sub = Hex("#8FCF98"),
            BarLabel = Hex("#9EF7AA"), BarReal = Hex("#8FCF98"),
            SegOff = Hex("#101A14"), SegOffBorder = Hex("#1C2A21"), SegRadius = 0,
            StatBg = Hex("#070B09"), StatBorder = Hex("#1A241E"), StatLabel = Hex("#5FA86A"), StatValue = Hex("#C9F5CF"), StatRadius = 6,
            TileBg = Hex("#060908"), TileBorder = Hex("#29322D"), TileLabel = Hex("#46FF5A"), TileLabelStrip = Color.clear,
            TileRadius = 8, TileBorderWidth = 1, Lamp = true, Glow = true,
            Accent = Hex("#46FF5A"), Unknown = Hex("#FFA31F"), Warning = Hex("#FFA31F"),
        };

        private static ThemeDef Annonciateur() => new ThemeDef
        {
            Name = "Annonciateur",
            PanelBg = Hex("#15181B"), PanelBorder = Hex("#2C3136"), PanelRadius = 4, PanelBorderWidth = 2,
            Body = Hex("#D4DAD6"),
            ZoneTitle = Hex("#AEB6B1"), ZoneTitleBg = Hex("#0E1012"), ZoneTitleAccent = Hex("#6B7470"),
            Divider = Color.clear,
            Title = Hex("#F2F5F3"), Chip = Hex("#AEB6B1"), Sub = Hex("#AEB6B1"),
            BarLabel = Hex("#D4DAD6"), BarReal = Hex("#AEB6B1"),
            SegOff = Hex("#0C0E10"), SegOffBorder = Hex("#2B3035"), SegOnDimmed = true, SegRadius = 2,
            StatBg = Hex("#0E1012"), StatBorder = Hex("#262B30"), StatLabel = Hex("#87908B"), StatValue = Hex("#EEF2EF"), StatRadius = 2,
            TileBg = Hex("#0B0D0F"), TileBorder = Hex("#30363B"), TileLabel = Hex("#E6EBE8"), TileLabelStrip = Hex("#0B0D0F"),
            TileRadius = 3, TileBorderWidth = 2, TileFilledByNote = true, TileNaBg = Hex("#0F1214"), TileNaBorder = Hex("#30363B"),
            Lamp = false, Glow = false,
            Accent = Hex("#F0C45A"), Unknown = Hex("#FFA31F"), Warning = Hex("#FFA31F"),
        };

        private static ThemeDef HangarNO() => new ThemeDef
        {
            Name = "HangarNO",
            PanelBg = Rgba(9, 27, 46, 0.86f), PanelBorder = Rgba(150, 185, 215, 0.35f), PanelRadius = 2, PanelBorderWidth = 1,
            Body = Hex("#E6EEF5"),
            ZoneTitle = Hex("#E6EEF5"), ZoneTitleBg = new Color(1f, 1f, 1f, 0.06f), ZoneTitleAccent = Color.clear, ZoneTitleCentered = true,
            Divider = Color.clear,
            Title = Color.white, Chip = Hex("#A9C3DA"), Sub = Hex("#B9CDE0"),
            BarLabel = Hex("#E6EEF5"), BarReal = Hex("#B9CDE0"),
            SegOff = new Color(0f, 0f, 0f, 0.35f), SegOffBorder = Rgba(150, 185, 215, 0.22f), SegRadius = 0,
            StatBg = new Color(0f, 0f, 0f, 0.25f), StatBorder = Rgba(150, 185, 215, 0.18f), StatLabel = Hex("#9FB6CC"), StatValue = Color.white, StatRadius = 0,
            TileBg = new Color(0f, 0f, 0f, 0.32f), TileBorder = Rgba(150, 185, 215, 0.25f), TileLabel = Hex("#CFE0EF"), TileLabelStrip = Color.clear,
            TileRadius = 4, TileBorderWidth = 1, Lamp = true, Glow = true,
            Accent = Hex("#8FD3FF"), Unknown = Hex("#FFD523"), Warning = Hex("#FFA31F"),
        };
    }
}

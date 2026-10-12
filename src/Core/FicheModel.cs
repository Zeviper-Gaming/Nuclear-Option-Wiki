using System.Collections.Generic;

namespace NuclearOptionWiki.Core
{
    public enum RangeNoteMode
    {
        Logarithmic,
        Rank
    }

    /// <summary>Valeurs lues en jeu sur le prefab de l'arme (null / vide si indisponibles).</summary>
    public struct LiveStats
    {
        public double? RcsM2;
        public double? IrIntensity;
        public double? TurnRateDegS;
        public double? GLimit;

        // Plan v1.1 (D29, D30)
        public string SeekerType;       // MissileSeeker.GetSeekerType() : « IR », « ARH », « Optical »…
        public string SeekerComponent;  // nom du composant : « OpticalSeekerBomb »…
        public bool? LaserGuided;       // WeaponInfo.laserGuided
        public bool IsMissile;          // WeaponInfo.missile
        public bool IsBomb;             // WeaponInfo.bomb || glideBomb
        public bool IsGun;
        public bool IsEnergy;
        public int? AgilityNote;        // note /5 calculée en jeu (rang du gLimit parmi les missiles)

        public static readonly LiveStats None = new LiveStats();
    }

    /// <summary>Barre à 10 niveaux : libellé, note, couleur, segments allumés, valeur réelle.</summary>
    public sealed class BarField
    {
        public string Label;
        public string Note;      // « 5,5 » ou « N/A »
        public string Unit;      // « /10 » ou vide
        public string Real;      // « 10 km »
        public int? ColorIndex;  // null = N/A
        public int LitSegments;
    }

    /// <summary>Cellule de stat : libellé + valeur texte.</summary>
    public sealed class StatField
    {
        public string Label;
        public string Value;
    }

    /// <summary>Tuile voyant MFD : lampe colorée, note en gros, libellé, éventuelle valeur réelle.</summary>
    public sealed class TileField
    {
        public string Label;
        public string Note;      // « 3 » ou « N/A »
        public string Unit;      // « /5 » ou vide
        public string Real;      // null = pas de ligne de valeur réelle
        public int? ColorIndex;  // null = N/A (gris)
        public bool Inverted;    // signature : note haute = mauvaise
        public bool Neutral;     // compteur (fiche appareil) : couleur d'accent du thème, pas de note
        public bool IsNotAvailable => ColorIndex == null;
    }

    /// <summary>
    /// Contenu d'une fiche. La structure est FIXE : mêmes champs, même ordre, pour toutes les armes
    /// (décision D6). L'interface ne fait qu'afficher ce modèle.
    /// </summary>
    public sealed class FicheModel
    {
        public const int BarCount = 5;
        public const int StatCount = 6;
        public const int AirToGroundCount = 8;
        public const int AirToAirCount = 4;
        public const int DefenseCount = 5;

        public string Title;
        public string Chip1;
        public string Chip2;     // vide si une seule puce
        public string Subtitle;
        public bool Unreferenced;
        public List<string> Profile = new List<string>();   // pastilles ; vide = « N/A »
        public BarField[] Bars = new BarField[BarCount];
        public StatField[] Stats = new StatField[StatCount];
        public TileField[] AirToGround = new TileField[AirToGroundCount];
        public TileField[] AirToAir = new TileField[AirToAirCount];
        public TileField[] Defense = new TileField[DefenseCount];

        public static readonly string[] BarLabels = { "Vitesse", "Portée", "Dégâts", "Pénétration", "Explosif/M$" };
        public static readonly string[] StatLabels = { "Coût", "Portée min", "Autodirecteur", "IR", "Tire et oublie", "Laser requis" };
        public static readonly string[] AirToGroundLabels =
            { "Véh. légers", "Chars", "Bât. légers", "Gros bât.", "Bunker", "Navire léger", "Navire moyen", "Porte-avions" };
        public static readonly string[] AirToAirLabels = { "Petit avion", "Hélico", "Moyen avion", "Gros avion" };
        public static readonly string[] DefenseLabels = { "Agilité", "Sign. radar", "Sign. IR", "Rés. laser", "Diff. toucher" };

        /// <summary>
        /// Construit la fiche. <paramref name="record"/> null = arme non référencée : même fiche, notes en N/A,
        /// titre = <paramref name="gameName"/>, valeurs lues en jeu conservées.
        /// </summary>
        public static FicheModel Build(WeaponRecord record, string gameName, LiveStats live, RangeNoteMode rangeMode)
        {
            bool known = record != null;
            var r = record ?? new WeaponRecord();
            var m = new FicheModel { Unreferenced = !known };
            var seeker = SeekerInfo.Derive(live);
            string seekerText = seeker.Display ?? Format.NotAvailable;

            if (known)
            {
                m.Title = Format.OrNotAvailable(r.Name);
                m.Chip1 = Format.OrNotAvailable(r.Type);
                m.Chip2 = Format.OrNotAvailable(r.Source);
                m.Subtitle = "Guidage : " + Format.OrNotAvailable(r.Guidance) + " · Autodirecteur : " + seekerText;
                m.Profile = AttackProfile.Chips(r.Approach);
            }
            else
            {
                m.Title = string.IsNullOrWhiteSpace(gameName) ? "Arme inconnue" : gameName.Trim();
                m.Chip1 = "Hors Excel";
                m.Chip2 = "";
                m.Subtitle = "ARME NON RÉFÉRENCÉE · nom noté dans le journal BepInEx";
            }

            m.Bars[0] = Bar(BarLabels[0], r.SpeedNote, Format.Speed(r.SpeedMs));
            m.Bars[1] = Bar(BarLabels[1], rangeMode == RangeNoteMode.Logarithmic ? r.RangeNoteLog : r.RangeNoteRank, Format.Distance(r.MaxRangeM));
            m.Bars[2] = Bar(BarLabels[2], r.ExplosiveNote, Format.Kilograms(r.ExplosiveKg));
            m.Bars[3] = Bar(BarLabels[3], r.PenetrationNote, Format.Plain(r.Penetration));
            m.Bars[4] = Bar(BarLabels[4], r.ExplosivePerMillionNote, Format.KilogramsPerMillion(r.ExplosivePerMillion));

            m.Stats[0] = Stat(0, Format.Cost(r.CostMillions));
            m.Stats[1] = Stat(1, Format.Distance(r.MinRangeM));
            m.Stats[2] = Stat(2, seekerText);
            m.Stats[3] = Stat(3, Format.YesNo(seeker.Infrared));
            m.Stats[4] = Stat(4, Format.YesNo(r.FireAndForget ?? seeker.FireAndForget));
            m.Stats[5] = Stat(5, Format.YesNo(r.LaserRequired ?? seeker.LaserRequired));

            var g = r.AirToGround ?? new AirToGround();
            int?[] ground = { g.LightVehicles, g.Tanks, g.LightBuildings, g.LargeBuildings, g.Bunker, g.LightShip, g.MediumShip, g.Carrier };
            for (int i = 0; i < AirToGroundCount; i++) m.AirToGround[i] = Tile(AirToGroundLabels[i], ground[i], false, null);

            var a = r.AirToAir ?? new AirToAir();
            int?[] air = { a.SmallAircraft, a.Helicopter, a.MediumAircraft, a.LargeAircraft };
            for (int i = 0; i < AirToAirCount; i++) m.AirToAir[i] = Tile(AirToAirLabels[i], air[i], false, null);

            var d = r.Defense ?? new Defense();
            double? rcs = live.RcsM2 ?? d.RcsM2;
            m.Defense[0] = Tile(DefenseLabels[0], d.Agility ?? live.AgilityNote, false, Format.Agility(live.TurnRateDegS, live.GLimit));
            m.Defense[1] = Tile(DefenseLabels[1], d.RcsNote, true, Format.SquareMeters(rcs));
            m.Defense[2] = Tile(DefenseLabels[2], d.IrNote, true, Format.Infrared(live.IrIntensity));
            m.Defense[3] = Tile(DefenseLabels[3], d.LaserResistance, false, null);
            m.Defense[4] = Tile(DefenseLabels[4], d.HitDifficulty, false, null);
            return m;
        }

        private static BarField Bar(string label, double? note, string real) => new BarField
        {
            Label = label,
            Note = note == null ? Format.NotAvailable : Format.OneDecimal(note.Value),
            Unit = note == null ? "" : "/10",
            Real = real,
            ColorIndex = NoteScale.ColorIndex10(note),
            LitSegments = NoteScale.LitSegments(note),
        };

        private static StatField Stat(int index, string value) => new StatField { Label = StatLabels[index], Value = value };

        private static TileField Tile(string label, int? note, bool inverted, string real) => new TileField
        {
            Label = label,
            Note = note == null ? Format.NotAvailable : note.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Unit = note == null ? "" : "/5",
            Real = real,
            ColorIndex = NoteScale.ColorIndex5(note, inverted),
            Inverted = inverted,
        };
    }
}

using System;
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

        // Repli des valeurs absentes de weapons.json (mods ajoutés) : tout ce que le jeu sait de l'arme.
        public bool HasInfo;            // un WeaponInfo a été lu
        public WeaponFlags Flags;
        public double? SpeedMs, MinRangeM, MaxRangeM, Penetration, ExplosiveKg, CostMillions, MassKg;
        public double? TargetMaxSpeed;  // TargetRequirements.maxSpeed (≤ 0 : illimitée)
        public string Approach;         // même vocabulaire que la colonne « Méthode d'approche » du classeur
        public int? IrNote;             // note /5 calculée en jeu (rang de l'IR du premier moteur parmi les missiles)

        public static readonly LiveStats None = new LiveStats();
    }

    /// <summary>Classements des valeurs de weapons.json, pour noter au rang une valeur lue en jeu (D24, D10).</summary>
    public sealed class WeaponScales
    {
        public RankScale Speed, Range, Explosive, Penetration, ExplosivePerMillion;

        public static WeaponScales From(IEnumerable<WeaponRecord> records)
        {
            var list = new List<WeaponRecord>(records);
            var s = new WeaponScales();
            s.Speed = new RankScale(list.ConvertAll(r => r.SpeedMs), true);
            s.Range = new RankScale(list.ConvertAll(r => r.MaxRangeM), true);
            s.Explosive = new RankScale(list.ConvertAll(r => r.ExplosiveKg), true);
            s.Penetration = new RankScale(list.ConvertAll(r => r.Penetration), true);
            s.ExplosivePerMillion = new RankScale(list.ConvertAll(r => r.ExplosivePerMillion), true);
            return s;
        }

        /// <summary>D10 : 1 + 9 × (log10(portée_m) − 2) / 4, bornée à [1 ; 10], 1 décimale.</summary>
        public static double? RangeLogNote(double? meters)
        {
            if (meters == null || meters.Value <= 0) return null;
            double n = 1 + 9 * (Math.Log10(meters.Value) - 2) / 4;
            return NoteScale.RoundHalfUp(Math.Min(10, Math.Max(1, n)), 1);
        }
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
        public const string Estimated = "estimé";

        /// <param name="scales">Classements de weapons.json pour noter une valeur lue en jeu ; null = pas de note estimée au rang.</param>
        public static FicheModel Build(WeaponRecord record, string gameName, LiveStats live, RangeNoteMode rangeMode, WeaponScales scales = null)
        {
            bool known = record != null;
            var r = record ?? new WeaponRecord();
            var m = new FicheModel { Unreferenced = !known };
            var seeker = SeekerInfo.Derive(live);
            string seekerText = seeker.Display ?? Format.NotAvailable;
            string approach = r.Approach ?? live.Approach;

            if (known)
            {
                m.Title = Format.OrNotAvailable(r.Name);
                m.Chip1 = Format.OrNotAvailable(r.Type ?? (live.HasInfo ? WeaponEstimates.TypeOf(live.Flags) : null));
                m.Chip2 = Format.OrNotAvailable(r.Source);
                m.Subtitle = "Guidage : " + Format.OrNotAvailable(r.Guidance ?? seeker.Display) + " · Autodirecteur : " + seekerText;
            }
            else
            {
                m.Title = string.IsNullOrWhiteSpace(gameName) ? "Arme inconnue" : gameName.Trim();
                m.Chip1 = live.HasInfo ? WeaponEstimates.TypeOf(live.Flags) : "Hors Excel";
                m.Chip2 = live.HasInfo ? "Lu en jeu" : "";
                m.Subtitle = live.HasInfo
                    ? "Autodirecteur : " + seekerText + " · hors classeur : valeurs du jeu, notes estimées"
                    : "ARME NON RÉFÉRENCÉE · nom noté dans le journal BepInEx";
            }
            m.Profile = AttackProfile.Chips(approach);

            // Valeurs : classeur d'abord, sinon lues en jeu ; notes : classeur, sinon rang parmi les armes du classeur.
            double? speed = r.SpeedMs ?? Positive(live.SpeedMs);
            double? range = r.MaxRangeM ?? Positive(live.MaxRangeM);
            double? explosive = r.ExplosiveKg ?? live.ExplosiveKg;
            double? penetration = r.Penetration ?? live.Penetration;
            double? cost = r.CostMillions ?? live.CostMillions;
            double? perMillion = r.ExplosivePerMillion;
            if (perMillion == null && explosive != null && cost != null && cost.Value > 0)
                perMillion = NoteScale.RoundHalfUp(explosive.Value / cost.Value, 1);

            double? rangeNote = rangeMode == RangeNoteMode.Logarithmic
                ? r.RangeNoteLog ?? WeaponScales.RangeLogNote(range)
                : r.RangeNoteRank ?? scales?.Range.NoteOfNew(range);
            m.Bars[0] = Bar(BarLabels[0], r.SpeedNote ?? scales?.Speed.NoteOfNew(speed), Format.Speed(speed));
            m.Bars[1] = Bar(BarLabels[1], rangeNote, Format.Distance(range));
            m.Bars[2] = Bar(BarLabels[2], r.ExplosiveNote ?? scales?.Explosive.NoteOfNew(explosive), Format.Kilograms(explosive));
            m.Bars[3] = Bar(BarLabels[3], r.PenetrationNote ?? scales?.Penetration.NoteOfNew(penetration), Format.Plain(penetration));
            m.Bars[4] = Bar(BarLabels[4], r.ExplosivePerMillionNote ?? scales?.ExplosivePerMillion.NoteOfNew(perMillion),
                            Format.KilogramsPerMillion(perMillion));

            m.Stats[0] = Stat(0, Format.Cost(cost));
            m.Stats[1] = Stat(1, Format.Distance(r.MinRangeM ?? live.MinRangeM));
            m.Stats[2] = Stat(2, seekerText);
            m.Stats[3] = Stat(3, Format.YesNo(seeker.Infrared));
            m.Stats[4] = Stat(4, Format.YesNo(r.FireAndForget ?? seeker.FireAndForget));
            m.Stats[5] = Stat(5, Format.YesNo(r.LaserRequired ?? seeker.LaserRequired));

            var d = r.Defense ?? new Defense();
            double? rcs = live.RcsM2 ?? d.RcsM2;
            var est = new WeaponEstimates.Inputs
            {
                Penetration = penetration,
                ExplosiveKg = explosive,
                MassKg = live.MassKg,
                SpeedMs = speed,
                RcsM2 = rcs,
                IrNote = d.IrNote ?? live.IrNote,
                AntiSurface = live.Flags.AntiSurface > 0,
                AntiAir = live.Flags.AntiAir > 0,
                Nuclear = live.Flags.Nuclear,
                Energy = live.Flags.Energy,
                Glide = live.Flags.GlideBomb,
                Bomb = live.Flags.Bomb || live.Flags.GlideBomb,
                Projectile = live.Flags.Missile || live.Flags.HasMissileBody || live.Flags.Bomb || live.Flags.GlideBomb,
                TargetMaxSpeed = live.TargetMaxSpeed,
                Approach = approach,
            };

            var g = r.AirToGround ?? new AirToGround();
            int?[] ground = { g.LightVehicles, g.Tanks, g.LightBuildings, g.LargeBuildings, g.Bunker, g.LightShip, g.MediumShip, g.Carrier };
            for (int i = 0; i < AirToGroundCount; i++)
                m.AirToGround[i] = ground[i] != null || !live.HasInfo
                    ? Tile(AirToGroundLabels[i], ground[i], false, null)
                    : EstimatedTile(AirToGroundLabels[i], WeaponEstimates.GroundNote(i, est), null);

            var a = r.AirToAir ?? new AirToAir();
            int?[] air = { a.SmallAircraft, a.Helicopter, a.MediumAircraft, a.LargeAircraft };
            for (int i = 0; i < AirToAirCount; i++)
                m.AirToAir[i] = air[i] != null || !live.HasInfo
                    ? Tile(AirToAirLabels[i], air[i], false, null)
                    : EstimatedTile(AirToAirLabels[i], WeaponEstimates.AirNote(i, est), null);

            m.Defense[0] = Tile(DefenseLabels[0], d.Agility ?? live.AgilityNote, false, Format.Agility(live.TurnRateDegS, live.GLimit));
            m.Defense[1] = Tile(DefenseLabels[1], d.RcsNote ?? WeaponEstimates.RcsNote(rcs), true, Format.SquareMeters(rcs));
            m.Defense[2] = Tile(DefenseLabels[2], d.IrNote ?? live.IrNote, true, Format.Infrared(live.IrIntensity));
            m.Defense[3] = d.LaserResistance != null || !live.HasInfo
                ? Tile(DefenseLabels[3], d.LaserResistance, false, null)
                : EstimatedTile(DefenseLabels[3], WeaponEstimates.NoteFromN(WeaponEstimates.LaserN(est)), WeaponEstimates.LaserN(est));
            m.Defense[4] = d.HitDifficulty != null || !live.HasInfo
                ? Tile(DefenseLabels[4], d.HitDifficulty, false, null)
                : EstimatedTile(DefenseLabels[4], WeaponEstimates.NoteFromN(WeaponEstimates.SiteN(est)), WeaponEstimates.SiteN(est));
            return m;
        }

        private static double? Positive(double? v) => v != null && v.Value > 0 ? v : null;

        /// <summary>Tuile dont la note est calculée par le modèle du classeur d'efficacité : « estimé » (ou « est. N 3,4 ») en dessous.</summary>
        private static TileField EstimatedTile(string label, int? note, double? n) =>
            Tile(label, note, false, note == null ? null : n == null ? Estimated : "est. N " + Format.Number(n.Value, 1));

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

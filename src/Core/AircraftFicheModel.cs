using System;
using System.Collections.Generic;
using System.Globalization;

namespace NuclearOptionWiki.Core
{
    /// <summary>Emplacement lu en jeu (déjà résumé par <see cref="Summarize"/>).</summary>
    public sealed class LiveHardpoint
    {
        public string GameName;
        public int Index;                                            // indice dans WeaponManager.hardpointSets
        public List<int> Excludes = new List<int>();                 // precludingHardpointSets (indices)
        public int Points;
        public bool Bay;
        public bool ExcludesOthers;
        public double? ChargeKg;                                     // A9 : option d'arme la plus lourde (hors cargo)
        public List<WeaponCategory> Categories = new List<WeaponCategory>();
        public int WeaponCount;                                      // options ayant une catégorie

        /// <summary>Résume les options d'un emplacement (plan A9 amendé v3.2, A23).</summary>
        public static LiveHardpoint Summarize(string gameName, int points, bool bay, bool excludesOthers,
                                              IEnumerable<KeyValuePair<double, List<WeaponCategory>>> options)
        {
            var h = new LiveHardpoint { GameName = gameName, Points = points, Bay = bay, ExcludesOthers = excludesOthers };
            var lists = new List<List<WeaponCategory>>();
            foreach (var o in options)
            {
                if (o.Value == null || o.Value.Count == 0) continue;
                h.WeaponCount++;
                lists.Add(o.Value);
                if (!o.Value.Contains(WeaponCategory.Cargo) && o.Key > 0 && (h.ChargeKg == null || o.Key > h.ChargeKg.Value))
                    h.ChargeKg = o.Key;
            }
            h.Categories = WeaponCategories.Union(lists);
            return h;
        }

        /// <summary>
        /// Amendement v3.6 : charge utile = masse d'armement maximale sur l'ensemble des emplacements
        /// (points × charge max par point, cargo exclu), meilleure combinaison d'emplacements compatibles. Null si rien de pesé.
        /// Même calcul que tools/avions_commun.py (charge_utile_emports).
        /// </summary>
        public static double? MaxPayload(IList<LiveHardpoint> hardpoints)
        {
            if (hardpoints == null || hardpoints.Count == 0) return null;
            var weight = new Dictionary<int, double>();
            var excl = new Dictionary<int, HashSet<int>>();
            foreach (var h in hardpoints)
            {
                weight[h.Index] = (h.ChargeKg ?? 0) * h.Points;
                excl[h.Index] = new HashSet<int>(h.Excludes ?? new List<int>());
            }
            var free = new List<int>();
            var linked = new List<int>();
            foreach (var i in weight.Keys)
            {
                bool isLinked = excl[i].Count > 0;
                foreach (var j in weight.Keys) if (excl[j].Contains(i)) isLinked = true;
                (isLinked ? linked : free).Add(i);
            }
            double best = 0;
            if (linked.Count <= 16)
            {
                for (int mask = 0; mask < 1 << linked.Count; mask++)
                {
                    double sum = 0;
                    bool ok = true;
                    for (int a = 0; a < linked.Count && ok; a++)
                    {
                        if ((mask >> a & 1) == 0) continue;
                        for (int b = 0; b < linked.Count && ok; b++)
                            if (a != b && (mask >> b & 1) == 1 && (excl[linked[a]].Contains(linked[b]) || excl[linked[b]].Contains(linked[a]))) ok = false;
                        sum += weight[linked[a]];
                    }
                    if (ok && sum > best) best = sum;
                }
            }
            foreach (var i in free) best += weight[i];
            return best > 0 ? best : (double?)null;
        }
    }

    /// <summary>Valeurs lues en jeu sur la définition et le prefab de l'appareil (repli A4), null si indisponibles.</summary>
    public sealed class LiveAircraft
    {
        public string JsonKey, UnitName, Code, DescriptionEn;
        public double? Rank, CostMillions, SpeedKmh, StallKmh, Maneuverability, GLimit, EmptyMassKg, MaxMassKg;
        public double? LengthM, WidthM, RcsM2, Armor, TakeoffM, TakeoffMs;
        public bool? VerticalTakeoff;
        public double? IrMin, IrMaxDry, IrMax;
        /// <summary>IR plein gaz le plus fort de tous les appareils (corrections comprises), calculé une fois par session.</summary>
        public double? IrGlobalMax;
        public int? LaserTargets;
        public double? LaserRangeM;
        public List<LiveHardpoint> Hardpoints = new List<LiveHardpoint>();
        /// <summary>A24 : tags saisis en jeu (F1), prioritaires sur le classeur ; null = pas de saisie.</summary>
        public List<string> TagsOverride;
    }

    /// <summary>Rangée d'emplacement de la fiche.</summary>
    public sealed class HardpointRowField
    {
        public string Name;
        public bool ExcludesOthers;   // « ⊘ » devant le nom
        public string Points;         // « ×2 »
        public string Bay;            // « SOUTE » / « EXT »
        public string Class;          // « S » / « M » / « H » / « – »
        public string Charge;         // « 900 kg » / « N/A »
        public List<string> Categories = new List<string>();  // acronymes (v3.3)
        public string Count;          // « (12) »
        public bool Forced;           // au moins une valeur corrigée (« * »)
        public string MoreText;       // non null : rangée « +n emplacements »
    }

    /// <summary>
    /// Fiche appareil (plan avions 7.3, amendé v3.3). Structure FIXE : mêmes champs, même ordre, pour tous les appareils (A3).
    /// Ordre des sources, champ par champ (A4) : correction (déjà dans le JSON, marquée « * ») → aircraft.json → valeur lue en jeu.
    /// </summary>
    public sealed class AircraftFicheModel
    {
        public const int BarCount = 4;
        public const int StatCount = 9;
        public const int RowCount = 9;
        public const int TileCount = 6;
        public const string Star = "*";

        public static readonly string[] BarLabels = { "Vitesse", "Agilité", "Charge utile", "Coût" };
        public static readonly string[] StatLabels =
            { "Rang requis", "Masse vide / max", "Carburant", "Moteur", "Décrochage", "Décollage", "Radar", "Brouilleur", "Canons fixes" };
        public static readonly string[] TileLabels = { "Sign. radar", "IR 30 % gaz", "IR plein gaz", "Leurres IR", "Blindage", "Désignateur" };

        public string Title;
        public string ChipType;
        public string ChipSource;
        public string ChipSize;
        public bool Unreferenced;
        public List<string> Roles = new List<string>();     // vide = « N/A »
        public BarField[] Bars = new BarField[BarCount];
        public StatField[] Stats = new StatField[StatCount];
        public string Summary;
        public HardpointRowField[] Rows = new HardpointRowField[RowCount];  // null = rangée vide
        public TileField[] Tiles = new TileField[TileCount];
        public string PersonalText;                         // null = « — »
        public List<string> Tags = new List<string>();      // vide = « — »
        public string Description;

        public static AircraftFicheModel Build(AircraftDatabase db, AircraftRecord record, string key, LiveAircraft live)
        {
            live = live ?? new LiveAircraft();
            bool known = record != null;
            var r = record ?? new AircraftRecord();
            var t = db?.Thresholds ?? new AircraftThresholds();
            var m = new AircraftFicheModel { Unreferenced = !known };

            m.Title = known ? Format.OrNotAvailable(r.Name) : (string.IsNullOrWhiteSpace(live.UnitName) ? "Appareil inconnu" : live.UnitName.Trim());
            if (known)
            {
                m.ChipType = ShortType(r.Type) ?? Format.NotAvailable;
                m.ChipSource = Format.OrNotAvailable(r.Source);
                m.Roles = Clean(r.Roles);
            }
            else
            {
                m.ChipType = "Hors classeur";
                m.ChipSource = "Non référencé";
            }
            string size = known ? r.Size : null;
            if (size == null)
            {
                double? footprint = r.FootprintM2 ?? (live.LengthM * live.WidthM);
                size = LoadClass.Classify(footprint, t.Footprint);
            }
            m.ChipSize = (size ?? "–") + Mark(r, "taille");

            // Barres /10 (A14) : notes du JSON seulement ; COÛT inversé (cher = rouge).
            m.Bars[0] = Bar(BarLabels[0], r.SpeedNote, false, Value(r.SpeedKmh ?? live.SpeedKmh, Format.Kmh) + Mark(r, "vitesse_kmh"));
            m.Bars[1] = Bar(BarLabels[1], r.Maneuverability ?? live.Maneuverability, false,
                            Value(r.GLimit ?? live.GLimit, g => Format.Number(g, 1) + " G") + Mark(r, "maniabilite", "g_max"));
            double? payload = r.PayloadKg ?? LiveHardpoint.MaxPayload(live.Hardpoints);   // A14 amendé v3.6
            m.Bars[2] = Bar(BarLabels[2], r.PayloadNote, false, Value(payload, Format.Kg) + Mark(r, "charge_utile_kg"));
            m.Bars[3] = Bar(BarLabels[3], r.CostNote, true, Value(r.CostMillions ?? live.CostMillions, c => Format.Number(c, 1) + " M$") + Mark(r, "cout_m"));

            double? empty = known ? r.EmptyMassKg : live.EmptyMassKg;
            double? max = r.MaxMassKg ?? live.MaxMassKg;
            string masses = empty == null && max == null ? null
                : (empty == null ? Format.Unknown : Format.Number(empty.Value / 1000, 1)) + " / " + (max == null ? Format.Unknown : Format.Number(max.Value / 1000, 1)) + " t";
            string engine = r.ThrustKn != null ? Format.Number(r.ThrustKn.Value) + " kN" + Mark(r, "poussee_kn")
                : r.PowerKw != null ? Format.Number(r.PowerKw.Value) + " kW" + Mark(r, "puissance_kw") : null;
            bool? vertical = r.VerticalTakeoff ?? live.VerticalTakeoff;
            double? toM = r.TakeoffM ?? live.TakeoffM, toMs = r.TakeoffMs ?? live.TakeoffMs;
            string takeoff = vertical == true ? "VERTICAL"
                : toM != null && toM.Value > 0 ? Format.Number(toM.Value) + " m" + (toMs != null && toMs.Value > 0 ? " · " + Format.Number(toMs.Value) + " m/s" : "") : null;
            double? stall = r.StallKmh ?? live.StallKmh;
            m.Stats[0] = Stat(0, Value(r.Rank ?? live.Rank, v => Format.Number(v)) + Mark(r, "rang"));
            m.Stats[1] = Stat(1, masses == null ? null : masses + Mark(r, "masse_vide_kg", "masse_max_kg"));
            m.Stats[2] = Stat(2, r.FuelKg == null ? null : Format.Kg(r.FuelKg.Value) + Mark(r, "carburant_kg"));
            m.Stats[3] = Stat(3, engine);
            m.Stats[4] = Stat(4, stall == null || stall.Value <= 0 ? null : Format.Kmh(stall.Value) + Mark(r, "decrochage_kmh"));
            m.Stats[5] = Stat(5, takeoff == null ? null : takeoff + Mark(r, "decollage_vertical", "decollage_m", "decollage_ms"));
            m.Stats[6] = Stat(6, !known ? null : (r.RadarM == null || r.RadarM.Value <= 0 ? "NON" : Format.Distance(r.RadarM)) + Mark(r, "radar_m"));
            m.Stats[7] = Stat(7, r.Jammer == null ? null : Format.YesNo(r.Jammer) + Mark(r, "brouilleur"));
            m.Stats[8] = Stat(8, r.FixedGuns == null ? null : (r.FixedGuns.Value <= 0 ? "NON" : Format.Number(r.FixedGuns.Value)) + Mark(r, "canons_fixes"));

            BuildHardpoints(m, db, r, key, live, t);

            // Signatures · défenses (A11 amendé v3.3, A12, A21) — voyant Paillettes retiré (v3.3).
            double? rcs = r.RcsM2 ?? live.RcsM2;
            int? rcsNote = known && r.RcsNote != null ? r.RcsNote : LoadClass.RcsNote(rcs);
            double? ir30 = r.IrForced?.Ir30 ?? LoadClass.Ir30(live.IrMin, live.IrMaxDry);
            double? irMax = r.IrForced?.IrMax ?? live.IrMax;
            m.Tiles[0] = Tile(TileLabels[0], rcsNote, true, rcs == null ? Format.Unknown : Format.Number(rcs.Value, rcs.Value < 0.01 ? 6 : 2) + " m²" + Mark(r, "rcs_m2"));
            m.Tiles[1] = Tile(TileLabels[1], LoadClass.IrNote(ir30, live.IrGlobalMax), true,
                              ir30 == null ? Format.Unknown : Format.Number(ir30.Value, 2) + (r.IrForced?.Ir30 != null ? Star : ""));
            m.Tiles[2] = Tile(TileLabels[2], LoadClass.IrNote(irMax, live.IrGlobalMax), true,
                              irMax == null ? Format.Unknown : Format.Number(irMax.Value, 2) + (r.IrForced?.IrMax != null ? Star : ""));
            m.Tiles[3] = Counter(TileLabels[3], r.Flares, null, Mark(r, "leurres"));
            m.Tiles[4] = Counter(TileLabels[4], r.Armor ?? live.Armor, null, Mark(r, "blindage"));
            double? laser = r.LaserTargets ?? live.LaserTargets;
            m.Tiles[5] = Counter(TileLabels[5], laser,
                                 live.LaserRangeM != null && live.LaserRangeM.Value > 0 ? Format.Distance(live.LaserRangeM) : Format.Unknown,
                                 Mark(r, "laser_cibles"));

            m.PersonalText = string.IsNullOrWhiteSpace(r.PersonalText) ? null : r.PersonalText.Trim();
            m.Tags = Clean(live.TagsOverride ?? r.Tags);
            m.Description = !string.IsNullOrWhiteSpace(r.DescriptionFr) ? r.DescriptionFr.Trim()
                : string.IsNullOrWhiteSpace(live.DescriptionEn) ? Format.Unknown : live.DescriptionEn.Trim();
            return m;
        }

        private static void BuildHardpoints(AircraftFicheModel m, AircraftDatabase db, AircraftRecord r, string key, LiveAircraft live, AircraftThresholds t)
        {
            var visible = new List<HardpointRowField>();
            int points = 0, bays = 0;
            double? chargeMax = null;
            foreach (var h in live.Hardpoints ?? new List<LiveHardpoint>())
            {
                HardpointOverride f = null;
                r.HardpointsForced?.TryGetValue(h.GameName ?? "", out f);
                var cats = h.Categories ?? new List<WeaponCategory>();
                if (f?.Categories != null)
                {
                    cats = new List<WeaponCategory>();
                    foreach (var label in f.Categories)
                        if (WeaponCategories.TryParse(label, out var c)) cats.Add(c);
                }
                if ((f != null && f.Hide) || cats.Count == 0) continue;   // A23 + « Masquer l'emplacement »
                int pts = f?.Points ?? h.Points;
                bool bay = f?.Bay ?? h.Bay;
                double? charge = f?.ChargeKg ?? h.ChargeKg;
                int count = f?.WeaponCount ?? h.WeaponCount;
                points += pts;
                if (bay) bays++;
                if (charge != null && (chargeMax == null || charge.Value > chargeMax.Value)) chargeMax = charge;
                var row = new HardpointRowField
                {
                    Name = db?.HardpointName(key, h.GameName) ?? Format.OrNotAvailable(h.GameName),
                    ExcludesOthers = h.ExcludesOthers,
                    Points = "×" + pts.ToString(CultureInfo.InvariantCulture),
                    Bay = bay ? "SOUTE" : "EXT",
                    Class = LoadClass.Classify(charge, t.Load) ?? "–",
                    Charge = charge == null ? Format.NotAvailable : Format.Kg(charge.Value),
                    Count = "(" + count.ToString(CultureInfo.InvariantCulture) + ")",
                    Forced = f != null,
                };
                foreach (var c in WeaponCategories.Union(new[] { cats })) row.Categories.Add(WeaponCategories.Acronym(c));
                if (row.Forced) row.Name += Star;
                visible.Add(row);
            }
            int shown = visible.Count > RowCount ? RowCount - 1 : visible.Count;
            for (int i = 0; i < shown; i++) m.Rows[i] = visible[i];
            if (visible.Count > RowCount)
                m.Rows[RowCount - 1] = new HardpointRowField { MoreText = "+" + (visible.Count - shown).ToString(CultureInfo.InvariantCulture) + " emplacements" };
            m.Summary = Plural(visible.Count, "emplacement") + " · " + Plural(points, "point") + " · " + Plural(bays, "soute")
                        + " · charge max " + (chargeMax == null ? Format.NotAvailable : Format.Kg(chargeMax.Value));
        }

        /// <summary>Plan 7.8 : puce de type.</summary>
        public static string ShortType(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return null;
            string low = type.Trim().ToLowerInvariant();
            if (low.Contains("géométrie variable")) return "GÉOM. VARIABLE";
            if (low.StartsWith("avion à hélice")) return "HÉLICE";
            if (low.StartsWith("hélicoptère")) return "HÉLICO";
            if (low.StartsWith("avion à réaction")) return "RÉACTION";
            if (low.StartsWith("avion à décollage vertical")) return "VTOL";
            if (low.StartsWith("convertible à rotors basculants")) return "CONVERTIBLE";
            string up = type.Trim().ToUpperInvariant();
            return up.Length > 14 ? up.Substring(0, 14) : up;
        }

        private static string Plural(int n, string word) => n.ToString(CultureInfo.InvariantCulture) + " " + word + (n > 1 ? "s" : "");

        private static List<string> Clean(List<string> items)
        {
            var res = new List<string>();
            if (items == null) return res;
            foreach (var s in items)
                if (!string.IsNullOrWhiteSpace(s)) res.Add(s.Trim());
            return res;
        }

        private static string Mark(AircraftRecord r, params string[] fields)
        {
            foreach (var f in fields)
                if (r.IsCorrected(f)) return Star;
            return "";
        }

        private static string Value(double? v, Func<double, string> fmt) => v == null ? Format.NotAvailable : fmt(v.Value);

        private static BarField Bar(string label, double? note, bool inverted, string real)
        {
            int? color = NoteScale.ColorIndex10(note);
            if (inverted && color != null) color = 5 - color.Value;
            return new BarField
            {
                Label = label,
                Note = note == null ? Format.NotAvailable : Format.OneDecimal(note.Value),
                Unit = note == null ? "" : "/10",
                Real = real,
                ColorIndex = color,
                LitSegments = NoteScale.LitSegments(note),
            };
        }

        private static StatField Stat(int index, string value) =>
            new StatField { Label = StatLabels[index], Value = string.IsNullOrEmpty(value) || value.StartsWith(Format.NotAvailable, StringComparison.Ordinal) ? Format.NotAvailable : value };

        private static TileField Tile(string label, int? note, bool inverted, string real) => new TileField
        {
            Label = label,
            Note = note == null ? Format.NotAvailable : note.Value.ToString(CultureInfo.InvariantCulture),
            Unit = note == null ? "" : "/5",
            Real = real,
            ColorIndex = NoteScale.ColorIndex5(note, inverted),
            Inverted = inverted,
        };

        /// <summary>Compteur (couleur neutre = accent du thème, pas de note). 0 reste « 0 » (A21).</summary>
        private static TileField Counter(string label, double? count, string real, string mark) => new TileField
        {
            Label = label,
            Note = count == null ? Format.NotAvailable : Format.Number(count.Value) + mark,
            Unit = "",
            Real = real,
            ColorIndex = count == null ? (int?)null : 5,
            Neutral = true,
        };
    }
}

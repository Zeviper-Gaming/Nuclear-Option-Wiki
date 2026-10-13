using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NuclearOptionWiki.Core;

namespace NuclearOptionWiki.Tests
{
    /// <summary>
    /// Tests de la fiche appareil (plan avions, étape V6). Les valeurs « lues en jeu » sont reconstituées depuis le dernier
    /// export du mod (data/dump/*_avions_v*.csv et *_emports_v*.csv), comme le ferait LiveAircraftStats.
    /// </summary>
    internal static class AircraftTests
    {
        public static void Run(string root, Action<bool, string> check, Action<string, string, string> eq)
        {
            var db = AircraftDatabase.FromJson(File.ReadAllText(Path.Combine(root, "data", "aircraft.json")));
            var live = LoadLive(root);
            double irMax = db.All.Select(kv => kv.Value.IrForced?.IrMax ?? (live.TryGetValue(kv.Key, out var l) ? l.IrMax : null))
                             .Where(v => v != null).Max(v => v.Value);
            foreach (var l in live.Values) l.IrGlobalMax = irMax;

            eq("24", db.Count.ToString(), "aircraft.json : 24 appareils");

            // Toutes les fiches (24 + une inconnue) : même structure (A3).
            var sheets = new List<AircraftFicheModel>();
            foreach (var kv in db.All)
                sheets.Add(AircraftFicheModel.Build(db, kv.Value, kv.Key, live.TryGetValue(kv.Key, out var l) ? l : null));
            var unknownLive = new LiveAircraft { UnitName = "Mod Fighter X", LengthM = 15, WidthM = 11, RcsM2 = 0.3, IrMin = 1, IrMaxDry = 4, IrMax = 7, IrGlobalMax = irMax };
            unknownLive.Hardpoints.Add(LiveHardpoint.Summarize("Wing Pylons", 2, false, false,
                new[] { Opt(500, WeaponCategory.Bombe), Opt(150, WeaponCategory.MissileAA) }));
            var unknown = AircraftFicheModel.Build(db, null, null, unknownLive);
            sheets.Add(unknown);
            string reference = Signature(sheets[0]);
            int bad = sheets.Count(s => Signature(s) != reference || !AllTexts(s));
            eq("0", bad.ToString(), $"{sheets.Count} fiches appareil : structure identique et textes complets");

            // CI-22 (contrôles du plan V4 / V6).
            var ci = Sheet(db, live, "COIN");
            eq("S", ci.ChipSize, "CI-22 : taille S");
            eq("HÉLICE", ci.ChipType, "CI-22 : puce de type");
            eq("4", ci.Tiles[0].Note, "CI-22 : note RCS 4");
            eq("6,5", ci.Bars[1].Note, "CI-22 : agilité 6,5");
            eq("510 km/h", ci.Bars[0].Real, "CI-22 : vitesse");
            eq("3,9 / 4,8 t", ci.Stats[1].Value, "CI-22 : masses");
            eq("1 500 kW", ci.Stats[3].Value, "CI-22 : moteur");
            eq("100 m · 50 m/s", ci.Stats[5].Value, "CI-22 : décollage");
            eq("NON", ci.Stats[6].Value, "CI-22 : radar");
            eq("2", ci.Stats[8].Value, "CI-22 : canons fixes");
            eq("2", ci.Tiles[5].Note, "CI-22 : désignateur laser = 2 (A21)");
            eq("15 km", ci.Tiles[5].Real, "CI-22 : portée du désignateur");
            check(ci.Tiles[5].Neutral && ci.Tiles[3].Neutral && ci.Tiles[4].Neutral, "compteurs neutres");
            eq("6 emplacements · 9 points · 0 soute · charge max 413 kg", ci.Summary, "CI-22 : synthèse des emports");
            eq("Pylônes d'aile intérieurs", ci.Rows[3].Name, "CI-22 : nom FR d'emplacement");
            eq("CN,AAM,ASM,RK,BMB", string.Join(",", ci.Rows[3].Categories), "CI-22 : pastilles en acronymes (v3.3)");
            check(ci.Rows[6] == null && ci.Rows[8] == null, "CI-22 : rangées non utilisées vides");
            eq("Contre-insurrection", ci.Roles.FirstOrDefault(), "CI-22 : rôles");

            // Hélicoptère : décrochage sans objet.
            var sah = Sheet(db, live, "AttackHelo1");
            eq("N/A", sah.Stats[4].Value, "SAH-46 : décrochage N/A");
            eq("VERTICAL", sah.Stats[5].Value, "SAH-46 : décollage vertical");

            // MC-260 : taille H, crochet « - » masqué (A23) ; F-22E : « +n emplacements ».
            var mc = Sheet(db, live, "Aryx_CargoPlane1");
            eq("H", mc.ChipSize, "MC-260 : taille H");
            eq("8", mc.Rows.Count(r => r != null).ToString(), "MC-260 : 8 rangées (crochet masqué)");
            check(mc.Rows.All(r => r == null || r.Name != "-"), "A23 : emplacement « - » absent");
            var f22 = Sheet(db, live, "Aryx_F22E_StrikeRaptor");
            eq("+2 emplacements", f22.Rows[8].MoreText, "F-22E : 9ᵉ rangée « +2 emplacements »");
            eq("9 200 kg", f22.Bars[2].Real, "F-22E : charge utile = emports (v3.6)");

            // F-117A : RCS 0 ; FS-41 : vitesse 10,0.
            eq("0", Sheet(db, live, "blacknight2u_F117A_Nighthawk").Tiles[0].Note, "F-117A : note RCS 0");
            eq("10,0", Sheet(db, live, "Aryx_Interceptor1").Bars[0].Note, "FS-41 : note vitesse 10,0");

            // IR (A11 amendé v3.3) : même échelle ; jamais plus fort à 30 % qu'à plein gaz.
            var kr = Sheet(db, live, "1509_palafighter1");
            eq("2", kr.Tiles[1].Note, "KR-33 : IR 30 % = 2/5");
            eq("4", kr.Tiles[2].Note, "KR-33 : IR plein gaz = 4/5");
            check(sheets.Take(24).All(s => s.Tiles[1].Note == "N/A" || int.Parse(s.Tiles[1].Note) <= int.Parse(s.Tiles[2].Note)),
                  "IR : note 30 % ≤ note plein gaz pour tous");
            eq("5", LoadClass.IrNote(11, 11).ToString(), "IR max → 5");
            eq("0", LoadClass.IrNote(0.5, 11).ToString(), "IR faible → 0");

            // Coût inversé : cher = rouge.
            var ab4 = Sheet(db, live, "FastBomber1");
            eq("10,0", ab4.Bars[3].Note, "AB-4 : note coût 10,0");
            eq("0", ab4.Bars[3].ColorIndex.ToString(), "coût inversé : le plus cher est rouge");

            // Priorité A4 : correction (dans le JSON, « * ») > JSON > jeu.
            var rec = db.Find("COIN", null).Value;
            var corrected = Clone(rec);
            corrected.SpeedKmh = 600;
            corrected.Corrected = new List<string> { "vitesse_kmh" };
            var cs = AircraftFicheModel.Build(db, corrected, "COIN", live["COIN"]);
            eq("600 km/h*", cs.Bars[0].Real, "A4 : valeur corrigée avec astérisque");
            var noSpeed = Clone(rec);
            noSpeed.SpeedKmh = null;
            var liveSpeed = live["COIN"];
            eq("510 km/h", AircraftFicheModel.Build(db, noSpeed, "COIN", liveSpeed).Bars[0].Real, "A4 : repli sur la valeur lue en jeu");

            // Emport forcé : charge, classe, astérisque ; masquage.
            var forced = Clone(rec);
            forced.HardpointsForced = new Dictionary<string, HardpointOverride>
            {
                ["Inner Wing Pylons"] = new HardpointOverride { ChargeKg = 5000, Categories = new List<string> { "CN" } },
                ["Ventral Hardpoint"] = new HardpointOverride { Hide = true },
            };
            var fs = AircraftFicheModel.Build(db, forced, "COIN", live["COIN"]);
            var row = fs.Rows.First(r => r != null && r.Name.StartsWith("Pylônes d'aile intérieurs"));
            eq("5 000 kg", row.Charge, "emport forcé : charge");
            eq("H", row.Class, "emport forcé : classe recalculée");
            eq("CN", string.Join(",", row.Categories), "emport forcé : catégories (acronyme accepté)");
            check(row.Name.EndsWith("*"), "emport forcé : astérisque");
            check(fs.Rows.All(r => r == null || !r.Name.StartsWith("Point d'emport ventral")), "emport masqué");
            eq("5 emplacements", fs.Summary.Substring(0, 14), "synthèse sans l'emplacement masqué");

            // IR forcé.
            var irf = Clone(rec);
            irf.IrForced = new IrOverride { IrMax = 11 };
            eq("11*", AircraftFicheModel.Build(db, irf, "COIN", live["COIN"]).Tiles[2].Real, "IR forcé avec astérisque");

            // Tags (v3.3) et tags saisis en jeu (A24).
            var tagged = Clone(rec);
            tagged.Tags = new List<string> { "STOL", " Pas cher " };
            eq("STOL,Pas cher", string.Join(",", AircraftFicheModel.Build(db, tagged, "COIN", live["COIN"]).Tags), "tags du classeur");
            var liveTags = LoadLive(root)["COIN"];
            liveTags.TagsOverride = new List<string> { "Mon tag" };
            eq("Mon tag", string.Join(",", AircraftFicheModel.Build(db, tagged, "COIN", liveTags).Tags), "A24 : tags en jeu prioritaires");
            eq("STOL;Terrain sommaire", string.Join(";", TagsFile.Parse(" STOL ; ;Terrain sommaire ")), "A24 : saisie « ; »");

            // Appareil non référencé (A5).
            check(unknown.Unreferenced, "non référencé");
            eq("Mod Fighter X", unknown.Title, "non référencé : nom du jeu");
            eq("N/A", unknown.Bars[0].Note, "non référencé : notes N/A");
            eq("S", unknown.ChipSize, "non référencé : taille calculée en jeu (165 m²)");
            eq("3", unknown.Tiles[0].Note, "non référencé : note RCS calculée (0,3 m²)");
            eq("1 emplacement · 2 points · 0 soute · charge max 500 kg", unknown.Summary, "non référencé : emports lus en jeu");
            eq("1 000 kg", unknown.Bars[2].Real, "non référencé : charge utile calculée en jeu (2 × 500 kg)");
            // C# = Python pour tous les appareils du classeur (même règle v3.6).
            int diff = db.All.Count(kv => live.ContainsKey(kv.Key) && kv.Value.PayloadKg != LiveHardpoint.MaxPayload(live[kv.Key].Hardpoints));
            eq("0", diff.ToString(), "v3.6 : charge utile C# identique au script");
            eq("M", unknown.Rows[0].Class, "non référencé : classe de charge (500 kg)");
            check(db.Find(null, "CI-22 Cricket").Key == "COIN", "A5 : recherche par unitName");
            check(db.Find("inconnu", "inconnu").Value == null, "A5 : non trouvé");
            check(AircraftDatabase.IsExcluded("UFO", false) && AircraftDatabase.IsExcluded("x", true), "A6 : OVNI exclu");

            // Seuils (A7, A9) recalculés en C# = JSON.
            var t = LoadClass.FromValues(db.All.Select(kv => kv.Value.FootprintM2));
            eq(db.Thresholds.Footprint.SMax.ToString("0.0", CultureInfo.InvariantCulture), t.SMax.ToString("0.0", CultureInfo.InvariantCulture), "A7 : seuil S identique au script");
            eq("4", LoadClass.RcsNote(0.5).ToString(), "A12 : 0,5 m² → 4");
            eq("0", LoadClass.RcsNote(5e-7).ToString(), "A12 : 5·10⁻⁷ m² → 0");
            eq("5", LoadClass.RcsNote(5).ToString(), "A12 : 5 m² → 5");
            eq("RÉACTION", AircraftFicheModel.ShortType("Avion à réaction"), "7.8 : type court");
            eq("GÉOM. VARIABLE", AircraftFicheModel.ShortType("Avion à réaction, ailes à géométrie variable"), "7.8 : géométrie variable");
            eq("1 060 km/h", Format.Kmh(1060), "format km/h");
            check(WeaponCategories.TryParse("EW", out var ew) && ew == WeaponCategory.Brouilleur, "acronyme EW = brouilleur");
        }

        private static KeyValuePair<double, List<WeaponCategory>> Opt(double mass, params WeaponCategory[] cats) =>
            new KeyValuePair<double, List<WeaponCategory>>(mass, cats.ToList());

        private static AircraftFicheModel Sheet(AircraftDatabase db, Dictionary<string, LiveAircraft> live, string key) =>
            AircraftFicheModel.Build(db, db.Find(key, null).Value, key, live[key]);

        private static AircraftRecord Clone(AircraftRecord r) =>
            Newtonsoft.Json.JsonConvert.DeserializeObject<AircraftRecord>(Newtonsoft.Json.JsonConvert.SerializeObject(r));

        private static string Signature(AircraftFicheModel f) => string.Join("|", new[]
        {
            string.Join(",", f.Bars.Select(b => b.Label)),
            string.Join(",", f.Stats.Select(s => s.Label)),
            f.Rows.Length.ToString(),
            string.Join(",", f.Tiles.Select(t => t.Label + (t.Inverted ? "~" : "") + (t.Neutral ? "#" : ""))),
        });

        private static bool AllTexts(AircraftFicheModel f)
        {
            var texts = new List<string> { f.Title, f.ChipType, f.ChipSource, f.ChipSize, f.Summary, f.Description };
            texts.AddRange(f.Bars.SelectMany(b => new[] { b.Label, b.Note, b.Real }));
            texts.AddRange(f.Stats.Select(s => s.Value));
            // Leurres et blindage : compteurs sans ligne de valeur réelle (Real null, comme la fiche arme).
            texts.AddRange(f.Tiles.SelectMany((t, i) => i == 3 || i == 4 ? new[] { t.Label, t.Note } : new[] { t.Label, t.Note, t.Real }));
            texts.AddRange(f.Rows.Where(r => r != null && r.MoreText == null).SelectMany(r => new[] { r.Name, r.Points, r.Bay, r.Class, r.Charge, r.Count }));
            return texts.All(s => !string.IsNullOrEmpty(s));
        }

        /// <summary>Reconstitue LiveAircraft depuis le dernier export (ce que LiveAircraftStats lit en jeu).</summary>
        private static Dictionary<string, LiveAircraft> LoadLive(string root)
        {
            string dump = Path.Combine(root, "data", "dump");
            var avions = Csv(Directory.GetFiles(dump, "*_avions_v*.csv").OrderBy(p => p, StringComparer.Ordinal).Last());
            var emports = Csv(Directory.GetFiles(dump, "*_emports_v*.csv").OrderBy(p => p, StringComparer.Ordinal).Last());
            var res = new Dictionary<string, LiveAircraft>();
            foreach (var a in avions)
            {
                res[a["json_key"]] = new LiveAircraft
                {
                    JsonKey = a["json_key"], UnitName = a["unit_name"], Code = a["code"], DescriptionEn = a["description"],
                    Rank = D(a["rang"]), CostMillions = D(a["cout"]), SpeedKmh = D(a["vmax_info"]), StallKmh = D(a["decrochage"]),
                    Maneuverability = D(a["maniabilite"]), GLimit = D(a["g_max"]), EmptyMassKg = D(a["masse_vide"]), MaxMassKg = D(a["masse_max"]),
                    LengthM = D(a["longueur"]), WidthM = D(a["largeur"]), RcsM2 = D(a["rcs"]), Armor = D(a["blindage"]),
                    VerticalTakeoff = a["vertical"] == "1", TakeoffM = D(a["decollage_m"]), TakeoffMs = D(a["decollage_ms"]),
                    IrMin = D(a["ir_min"]), IrMaxDry = D(a["ir_max_sec"]), IrMax = D(a["ir_max"]),
                    LaserTargets = (int?)D(a["laser_cibles"]), LaserRangeM = D(a["laser_portee"]),
                };
            }
            foreach (var g in emports.GroupBy(e => (e["json_key"], int.Parse(e["set_index"], CultureInfo.InvariantCulture))).OrderBy(g => g.Key.Item2))
            {
                if (!res.TryGetValue(g.Key.Item1, out var l)) continue;
                var first = g.First();
                var opts = g.Where(e => e["mount_jsonkey"] != "").Select(e => new KeyValuePair<double, List<WeaponCategory>>(
                    D(e["masse"]) ?? 0, e["categorie"].Split('|').Where(c => c != "").Select(c => { WeaponCategories.TryParse(c, out var wc); return wc; }).ToList()));
                var hp = LiveHardpoint.Summarize(first["set_name"], int.Parse(first["points"]), first["soute"] == "1", first["exclut"] != "", opts);
                hp.Index = g.Key.Item2;
                hp.Excludes = first["exclut"].Split('|').Where(x => x != "").Select(int.Parse).ToList();
                l.Hardpoints.Add(hp);
            }
            return res;
        }

        private static double? D(string s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (double?)null;

        private static List<Dictionary<string, string>> Csv(string path)
        {
            var lines = File.ReadAllLines(path);
            var head = lines[0].Split(';');
            return lines.Skip(1).Where(l => l.Length > 0).Select(l =>
            {
                var cells = l.Split(';');
                var d = new Dictionary<string, string>();
                for (int i = 0; i < head.Length; i++) d[head[i]] = i < cells.Length ? cells[i] : "";
                return d;
            }).ToList();
        }
    }
}

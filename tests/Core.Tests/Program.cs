using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuclearOptionWiki.Core;

namespace NuclearOptionWiki.Tests
{
    /// <summary>Tests du code Core : chargement de weapons.json, recherche, notes, formats et structure fixe des fiches.</summary>
    internal static class Program
    {
        private static int _failures;
        private static int _checks;

        private static int Main(string[] args)
        {
            string root = FindRepoRoot();
            string json = File.ReadAllText(Path.Combine(root, "data", "weapons.json"));
            var db = WeaponDatabase.FromJson(json);

            FormatTests();
            NoteScaleTests();
            DatabaseTests(db);
            Agm48Tests(db);
            SeekerTests();
            ProfileTests();
            UnknownWeaponTests();
            AllSheetsTests(db);
            ConfigKeyTests(root);

            Console.WriteLine(_failures == 0 ? $"OK ({_checks} vérifications)" : $"ÉCHEC : {_failures} / {_checks} vérifications");
            return _failures == 0 ? 0 : 1;
        }

        private static void FormatTests()
        {
            Eq("800 m", Format.Distance(800), "distance < 1 km");
            Eq("1 km", Format.Distance(1000), "distance 1 km");
            Eq("7,5 km", Format.Distance(7500), "distance avec décimale");
            Eq("10 km", Format.Distance(10000), "distance 10 km");
            Eq("1 000 km", Format.Distance(1_000_000), "distance 1 000 km");
            Eq("N/A", Format.Distance(null), "distance absente");
            Eq("358 m/s", Format.Speed(357.5), "vitesse arrondie au demi supérieur");
            Eq("0,09 M$", Format.Cost(0.09), "coût");
            Eq("11 000 kg", Format.Kilograms(11000), "explosif groupé");
            Eq("0,02 m²", Format.SquareMeters(0.02), "RCS");
            Eq("0,001 m²", Format.SquareMeters(0.001), "RCS petite");
            Eq("—", Format.SquareMeters(null), "RCS inconnue");
            Eq("OUI", Format.YesNo(true), "oui");
            Eq("N/A", Format.YesNo(null), "oui/non absent");
            Eq("35 °/s · 20 G", Format.Agility(35, 20), "agilité complète");
            Eq("20 G", Format.Agility(null, 20), "agilité G seul");
            Eq("—", Format.Agility(null, null), "agilité inconnue");
            Eq("6,0", Format.OneDecimal(6), "une décimale");
        }

        private static void NoteScaleTests()
        {
            Eq(3.0, NoteScale.RoundHalfUp(2.5), "arrondi au demi supérieur");
            Eq(6, NoteScale.LitSegments(5.5), "segments 5,5 → 6");
            Eq(1, NoteScale.LitSegments(0.2), "au moins un segment");
            Eq(0, NoteScale.LitSegments(null), "aucun segment si N/A");
            Eq(10, NoteScale.LitSegments(12), "au plus 10 segments");
            Eq(1, NoteScale.ColorIndex10(2.1), "couleur /10 : 2,1 → 1");
            Eq(5, NoteScale.ColorIndex10(9.5), "couleur /10 : 9,5 → 5");
            Eq(0, NoteScale.ColorIndex5(5, inverted: true), "couleur inversée 5 → rouge");
            Eq(5, NoteScale.ColorIndex5(0, inverted: true), "couleur inversée 0 → vert");
            Eq<int?>(null, NoteScale.ColorIndex5(null), "couleur N/A");
            Eq("#5D6670", NoteScale.Hex(null), "gris N/A");
        }

        private static void DatabaseTests(WeaponDatabase db)
        {
            Eq(139, db.Count, "139 armes dans weapons.json (133 + 6 de NO-stats)");
            True(db.Find("SARAAM1_info", null, null)?.Name == "AAM-21 Glaive", "AAM-21 Glaive ajouté depuis NO-stats");
            string agmKey = KeyOf(db, "AGM-48|Jeu de base");
            True(agmKey != null && db.Find(agmKey, null, null) != null, "recherche par clé du jeu");
            True(db.Find("kar_info_SAM_IR1", null, null)?.ExcelKey == "IRM-S1|Jeu de base", "recherche par alias d'asset (version KAR)");
            True(db.Find("Aryx_Wep_ARM2_Info", null, "ARAD-45")?.ExcelKey == "ARAD-45|Aryx Weaponry Pack", "ARAD-45 Aryx distinguée du jeu de base");
            True(db.Find("inconnu", null, "AGM-48") != null, "recherche par nom unique");
            True(db.Find(null, null, "30mm Autocannon") == null, "nom ambigu (×5) non résolu par le nom");
            True(db.Find("inconnu", "inconnu", "inconnu") == null, "arme absente");

            var withAlias = WeaponDatabase.FromJson(
                "{\"schema\":1,\"armes\":{\"InfoA\":{\"nom\":\"Arme A\"}},\"alias\":{\"MountA_x3\":\"InfoA\"}}");
            True(withAlias.Find("autre", "MountA_x3", null)?.Name == "Arme A", "recherche par alias d'emport");

            bool threw = false;
            try { WeaponDatabase.FromJson("{\"schema\":2,\"armes\":{}}"); }
            catch (FormatException) { threw = true; }
            True(threw, "schéma inconnu refusé");
        }

        private static void Agm48Tests(WeaponDatabase db)
        {
            var record = db.Find(KeyOf(db, "AGM-48|Jeu de base"), null, null);
            var f = FicheModel.Build(record, "AGM-48", LiveStats.None, RangeNoteMode.Logarithmic);
            Eq("AGM-48", f.Title, "titre");
            Eq("Missile", f.Chip1, "puce type");
            Eq("Jeu de base", f.Chip2, "puce source");
            Eq("2,1", f.Bars[0].Note, "note vitesse");
            Eq(2, f.Bars[0].LitSegments, "segments vitesse");
            Eq("358 m/s", f.Bars[0].Real, "vitesse réelle");
            Eq("5,5", f.Bars[1].Note, "note portée log");
            Eq(6, f.Bars[1].LitSegments, "segments portée");
            Eq("10 km", f.Bars[1].Real, "portée réelle");
            Eq("6,7", FicheModel.Build(record, null, LiveStats.None, RangeNoteMode.Rank).Bars[1].Note, "note portée rang");
            Eq("5,5|9 kg", f.Bars[2].Note + "|" + f.Bars[2].Real, "barre dégâts");
            Eq("8,3|1 550", f.Bars[3].Note + "|" + f.Bars[3].Real, "barre pénétration");
            Eq("3,4|100 kg/M$", f.Bars[4].Note + "|" + f.Bars[4].Real, "barre explosif/M$");
            Eq("DATALINK|LOFT|ZIGZAG|SUIVI RELIEF", string.Join("|", f.Profile), "profil d'attaque");
            Eq("0,09 M$|1 km|N/A|N/A|N/A|N/A", string.Join("|", f.Stats.Select(s => s.Value)), "cellules de stats sans données de jeu");
            var agmLive = new LiveStats { SeekerType = "Optical", SeekerComponent = "OpticalSeeker", IsMissile = true, LaserGuided = false };
            var fa = FicheModel.Build(record, "AGM-48", agmLive, RangeNoteMode.Logarithmic);
            Eq("0,09 M$|1 km|Optique (opérateur)|NON|NON|NON", string.Join("|", fa.Stats.Select(s => s.Value)), "R11 : AGM-48 optique opérateur");
            True(fa.Subtitle.EndsWith("Autodirecteur : Optique (opérateur)"), "sous-titre avec autodirecteur");
            Eq("5|5|3|1|0|3|1|0", string.Join("|", f.AirToGround.Select(t => t.Note)), "notes air-sol");
            Eq("N/A|4|N/A|N/A", string.Join("|", f.AirToAir.Select(t => t.Note)), "AA : seule la note Hélico existe (efficacité)");
            Eq("2", f.Defense[1].Note, "note RCS");
            Eq(3, f.Defense[1].ColorIndex, "RCS inversée : 2 → couleur 3");
            Eq("0,02 m²", f.Defense[1].Real, "RCS réelle (Excel)");
            Eq(4, f.Defense[2].ColorIndex, "IR inversée : 1 → couleur 4");

            var live = new LiveStats { RcsM2 = 0.025, IrIntensity = 1.5, TurnRateDegS = 35, GLimit = 20 };
            var fl = FicheModel.Build(record, "AGM-48", live, RangeNoteMode.Logarithmic);
            Eq("0,025 m²", fl.Defense[1].Real, "RCS lue en jeu prioritaire");
            Eq("IR 1,5", fl.Defense[2].Real, "IR lue en jeu");
            Eq("35 °/s · 20 G", fl.Defense[0].Real, "agilité lue en jeu");
            Eq("N/A", fl.Defense[0].Note, "note d'agilité absente de l'Excel");
        }

        /// <summary>Plan v1.1, tableau 11.4 et décision Q2 (F&F réservé aux missiles).</summary>
        private static void SeekerTests()
        {
            string Row(LiveStats l) { var s = SeekerInfo.Derive(l); return $"{s.Display ?? "N/A"}|{Format.YesNo(s.Infrared)}|{Format.YesNo(s.FireAndForget)}|{Format.YesNo(s.LaserRequired)}"; }
            Eq("Infrarouge|OUI|OUI|NON", Row(new LiveStats { SeekerType = "IR", IsMissile = true }), "missile IR");
            Eq("Radar semi-actif|NON|NON|NON", Row(new LiveStats { SeekerType = "SARH", IsMissile = true }), "missile SARH");
            Eq("Laser|NON|NON|OUI", Row(new LiveStats { SeekerType = "Laser", IsMissile = false }), "roquette laser (autodirecteur sans être une bombe = missile)");
            Eq("Aucun (balistique)|NON|N/A|NON", Row(new LiveStats { SeekerType = "Optical", SeekerComponent = "OpticalSeekerBomb", IsBomb = true }), "bombe non guidée : F&F N/A");
            Eq("Laser|NON|N/A|OUI", Row(new LiveStats { SeekerType = "Laser", IsBomb = true, LaserGuided = true }), "bombe guidée laser : F&F N/A (pas un missile)");
            Eq("Aucun (canon)|NON|N/A|NON", Row(new LiveStats { IsGun = true }), "canon");
            Eq("N/A|N/A|N/A|N/A", Row(LiveStats.None), "inconnu");
        }

        private static void ProfileTests()
        {
            Eq("RASE-SOL 15 M|TERMINALE", string.Join("|", AttackProfile.Chips("rase-flots/rase-sol 15 m; phase terminale à 3000 m")), "pastilles avec valeur");
            Eq("ATTAQUE HAUTE|DATALINK", string.Join("|", AttackProfile.Chips("attaque par le haut (proba 1.0, 1000-50000 m); datalink")), "attaque haute");
            Eq("MOTIF INCONNU…", string.Join("|", AttackProfile.Chips("motif inconnu très long")), "motif inconnu tronqué");
            Eq(0, AttackProfile.Chips(null).Count, "pas d'approche");
            Eq(0, NoteScale.LitSegments(0), "Q3 : note 0 → barre éteinte");
            Eq(0, NoteScale.ColorIndex10(0), "Q3 : note 0 → rouge");
        }

        private static void UnknownWeaponTests()
        {
            var live = new LiveStats { RcsM2 = 0.05 };
            var f = FicheModel.Build(null, "XYZ-99 Prototype", live, RangeNoteMode.Logarithmic);
            True(f.Unreferenced, "fiche non référencée");
            Eq("XYZ-99 Prototype", f.Title, "titre = nom en jeu");
            Eq("Hors Excel", f.Chip1, "puce hors Excel");
            True(f.Subtitle.StartsWith("ARME NON RÉFÉRENCÉE"), "mention non référencée");
            True(f.AirToGround.Concat(f.AirToAir).Concat(f.Defense).All(t => t.IsNotAvailable), "toutes les notes en N/A");
            Eq("0,05 m²", f.Defense[1].Real, "valeur lue en jeu conservée");
            Eq("Arme inconnue", FicheModel.Build(null, null, LiveStats.None, RangeNoteMode.Rank).Title, "titre par défaut");
        }

        /// <summary>Critère E5 : toutes les fiches ont exactement les mêmes champs, dans le même ordre.</summary>
        private static void AllSheetsTests(WeaponDatabase db)
        {
            var sheets = new List<(string key, FicheModel sheet)>();
            foreach (var kv in db.All)
                foreach (var mode in new[] { RangeNoteMode.Logarithmic, RangeNoteMode.Rank })
                    sheets.Add((kv.Key, FicheModel.Build(kv.Value, kv.Key, LiveStats.None, mode)));
            sheets.Add(("<inconnue>", FicheModel.Build(null, "Inconnue", LiveStats.None, RangeNoteMode.Logarithmic)));

            string reference = Signature(sheets[0].sheet);
            int bad = 0;
            foreach (var (key, sheet) in sheets)
            {
                if (Signature(sheet) != reference) { bad++; Console.WriteLine($"  structure différente : {key}"); }
                if (!AllTextsPresent(sheet)) { bad++; Console.WriteLine($"  texte manquant : {key}"); }
            }
            Eq(0, bad, $"{sheets.Count} fiches construites, structure identique et textes complets");
        }

        /// <summary>
        /// BepInEx refuse ces caractères dans les noms de section et de clé (ConfigDefinition.CheckInvalidConfigChars) :
        /// une seule apostrophe fait échouer tout le démarrage du mod (bug de la v0.2.1).
        /// </summary>
        private static void ConfigKeyTests(string root)
        {
            string source = File.ReadAllText(Path.Combine(root, "src", "Plugin", "WikiConfig.cs"));
            var binds = System.Text.RegularExpressions.Regex.Matches(source, "\\.Bind\\(\\s*\"([^\"]*)\"\\s*,\\s*\"([^\"]*)\"");
            char[] forbidden = { '=', '\n', '\t', '\\', '"', '\'', '[', ']' };
            Eq(9, binds.Count, "9 entrées de configuration trouvées");
            foreach (System.Text.RegularExpressions.Match b in binds)
            {
                foreach (var name in new[] { b.Groups[1].Value, b.Groups[2].Value })
                {
                    True(name.IndexOfAny(forbidden) < 0 && name == name.Trim(), $"nom de réglage valide pour BepInEx : « {name} »");
                }
            }
        }

        /// <summary>Clé (nom d'asset du jeu, ou clé provisoire) de la ligne Excel donnée.</summary>
        private static string KeyOf(WeaponDatabase db, string excelKey) =>
            db.All.Where(kv => kv.Value.ExcelKey == excelKey).Select(kv => kv.Key).FirstOrDefault();

        private static string Signature(FicheModel f) => string.Join("|", new[]
        {
            string.Join(",", f.Bars.Select(b => b.Label)),
            string.Join(",", f.Stats.Select(s => s.Label)),
            string.Join(",", f.AirToGround.Select(t => t.Label)),
            string.Join(",", f.AirToAir.Select(t => t.Label)),
            string.Join(",", f.Defense.Select(t => t.Label + (t.Inverted ? "~" : "") + (t.Real != null ? "+" : ""))),
        });

        private static bool AllTextsPresent(FicheModel f)
        {
            var texts = new List<string> { f.Title, f.Chip1, f.Chip2, f.Subtitle };
            texts.AddRange(f.Bars.SelectMany(b => new[] { b.Label, b.Note, b.Unit, b.Real }));
            if (f.Profile == null || f.Profile.Any(p => string.IsNullOrEmpty(p))) return false;
            texts.AddRange(f.Stats.Select(s => s.Value));
            texts.AddRange(f.AirToGround.Concat(f.AirToAir).Concat(f.Defense).Select(t => t.Note));
            texts.AddRange(f.AirToGround.Concat(f.AirToAir).Concat(f.Defense).Select(t => t.Unit));
            return texts.All(t => t != null) && !string.IsNullOrEmpty(f.Title);
        }

        private static void Eq<T>(T expected, T actual, string what)
        {
            _checks++;
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                _failures++;
                Console.WriteLine($"  ÉCHEC {what} : attendu « {expected} », obtenu « {actual} »");
            }
        }

        private static void True(bool condition, string what) => Eq(true, condition, what);

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "data", "weapons.json"))) dir = dir.Parent;
            return dir?.FullName ?? throw new DirectoryNotFoundException("Racine du dépôt introuvable (data/weapons.json).");
        }
    }
}

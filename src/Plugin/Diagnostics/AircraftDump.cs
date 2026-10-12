using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using NuclearOptionWiki.Core;
using NuclearOptionWiki.GameData;
using UnityEngine;

namespace NuclearOptionWiki.Diagnostics
{
    /// <summary>
    /// Exporte les appareils chargés en jeu (stats de la définition, IR des moteurs, viseurs, emports),
    /// pour construire le classeur Avions_wiki.xlsx (plan avions, section 7.2).
    /// Appareils = encyclopédie + toutes les AircraftDefinition chargées (mods tardifs compris), dédoublonnés par jsonKey.
    /// Une erreur sur un appareil donne une ligne partielle, jamais un échec de l'export.
    /// </summary>
    internal static class AircraftDump
    {
        public static string AircraftFileName => $"NuclearOptionWiki_avions_v{Plugin.Version}.csv";
        public static string HardpointsFileName => $"NuclearOptionWiki_emports_v{Plugin.Version}.csv";

        private static readonly string[] AircraftColumns =
        {
            "json_key", "unit_name", "code", "evenementiel", "encyclopedie", "rang", "cout", "vmax_info", "vmax_param", "decrochage",
            "maniabilite", "g_max", "masse_vide", "masse_max", "masse_parts", "longueur", "largeur", "hauteur", "rcs", "blindage",
            "vertical", "decollage_ms", "decollage_m", "moteurs", "ir_min", "ir_max_sec", "ir_max", "viseurs", "viseur_portee",
            "viseur_grossissement", "eots", "laser_cibles", "laser_portee", "nb_emplacements", "nb_points", "nb_soutes", "canons_fixes", "role_surface", "role_air",
            "role_missile", "role_radar", "description",
        };

        private const string HardpointsHeader =
            "json_key;set_index;set_name;points;soute;symetrie;exclut;integres;mount_jsonkey;mount_name;info_name;masse;munitions;categorie";


        /// <summary>Écrit (ou met à jour) les deux fichiers ; renvoie un message court pour le bouton du menu F1.</summary>
        public static string Export()
        {
            try
            {
                var encyclopedia = new HashSet<AircraftDefinition>();
                var all = new List<AircraftDefinition>();
                try
                {
                    var list = Encyclopedia.i?.aircraft;
                    if (list != null)
                    {
                        encyclopedia.UnionWith(list.Where(d => d != null));
                        all.AddRange(list.Where(d => d != null));
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"[Wiki] Encyclopédie illisible pendant l'export des appareils : {e.Message}");
                }
                all.AddRange(Resources.FindObjectsOfTypeAll<AircraftDefinition>().Where(d => d != null));

                var aircraftRows = new SortedDictionary<string, string>(StringComparer.Ordinal);
                var hardpointRows = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
                foreach (var def in all)
                {
                    string key = !string.IsNullOrEmpty(def.jsonKey) ? def.jsonKey : "(sans clé) " + def.name;
                    if (aircraftRows.ContainsKey(key)) continue;
                    var hp = new List<string>();
                    aircraftRows[key] = AircraftRow(def, encyclopedia.Contains(def), hp);
                    hardpointRows[key] = hp;
                }

                var sb = new StringBuilder();
                sb.Append(string.Join(";", AircraftColumns)).Append('\n');
                foreach (var row in aircraftRows.Values) sb.Append(row).Append('\n');
                string aircraftPath = Path.Combine(Paths.ConfigPath, AircraftFileName);
                bool existed = File.Exists(aircraftPath);
                File.WriteAllText(aircraftPath, sb.ToString(), new UTF8Encoding(false));

                sb.Length = 0;
                sb.Append(HardpointsHeader).Append('\n');
                int hpCount = 0;
                foreach (var rows in hardpointRows.Values)
                    foreach (var row in rows)
                    {
                        sb.Append(row).Append('\n');
                        hpCount++;
                    }
                string hardpointsPath = Path.Combine(Paths.ConfigPath, HardpointsFileName);
                File.WriteAllText(hardpointsPath, sb.ToString(), new UTF8Encoding(false));

                string verb = existed ? "mise à jour" : "créée";
                Plugin.Log.LogInfo($"[Wiki] v{Plugin.Version} : liste des appareils {verb} : {aircraftRows.Count} appareils dans {aircraftPath}, " +
                                   $"{hpCount} lignes d'emport dans {hardpointsPath}");
                return $"Liste {verb} à {DateTime.Now:HH:mm:ss} : {aircraftRows.Count} appareils, {hpCount} lignes d'emport";
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Wiki] Échec de l'export des appareils : {e}");
                return "Échec : voir LogOutput.log";
            }
        }

        private static string AircraftRow(AircraftDefinition def, bool inEncyclopedia, List<string> hardpointRows)
        {
            var v = new Dictionary<string, string>();
            string key = def.jsonKey ?? "";
            Section(def, "définition", () =>
            {
                v["json_key"] = Clean(def.jsonKey);
                v["unit_name"] = Clean(def.unitName);
                v["code"] = Clean(def.code);
                v["encyclopedie"] = Flag(inEncyclopedia);
                v["evenementiel"] = Flag(AircraftReader.IsEventContent(def));
                v["cout"] = Num(def.value);
                v["longueur"] = Num(def.length);
                v["largeur"] = Num(def.width);
                v["hauteur"] = Num(def.height);
                v["rcs"] = Num(def.radarSize);
                v["blindage"] = Num(def.armorTier);
                v["role_surface"] = Num(def.roleIdentity.antiSurface);
                v["role_air"] = Num(def.roleIdentity.antiAir);
                v["role_missile"] = Num(def.roleIdentity.antiMissile);
                v["role_radar"] = Num(def.roleIdentity.antiRadar);
                v["description"] = Clean(def.description);
            });
            Section(def, "paramètres", () =>
            {
                var p = def.aircraftParameters;
                if (p == null) return;
                v["rang"] = p.rankRequired.ToString(CultureInfo.InvariantCulture);
                v["vmax_param"] = Num(p.maxSpeed);
                v["g_max"] = Num(p.aircraftGLimit);
                v["vertical"] = Flag(p.verticalLanding);
                v["decollage_ms"] = Num(p.takeoffSpeed);
                v["decollage_m"] = Num(p.takeoffDistance);
            });
            Section(def, "infos", () =>
            {
                var i = def.aircraftInfo;
                if (i == null) return;
                v["vmax_info"] = Num(i.maxSpeed);
                v["decrochage"] = Num(i.stallSpeed);
                v["maniabilite"] = Num(i.maneuverability);
                v["masse_vide"] = Num(i.emptyWeight);
                v["masse_max"] = Num(i.maxWeight);
            });

            GameObject prefab = def.unitPrefab;
            Component aircraft = null;
            Section(def, "prefab", () => aircraft = AircraftReader.FindAircraft(prefab));
            if (prefab != null)
            {
                Section(def, "masse des pièces", () =>
                    v["masse_parts"] = Num(prefab.GetComponentsInChildren<UnitPart>(true).Where(p => p != null).Sum(p => p.mass)));
                Section(def, "moteurs", () =>
                {
                    var ir = AircraftReader.ReadInfrared(prefab);
                    v["moteurs"] = ir.EngineTypes;
                    v["ir_min"] = Num(ir.Min);
                    v["ir_max_sec"] = Num(ir.MaxDry);
                    v["ir_max"] = Num(ir.Max);
                });
                Section(def, "viseurs", () =>
                {
                    var detectors = prefab.GetComponentsInChildren<TargetDetector>(true)
                        .Where(d => d != null && d.GetType() == typeof(TargetDetector)).ToList();
                    v["viseurs"] = detectors.Count.ToString(CultureInfo.InvariantCulture);
                    if (detectors.Count > 0)
                    {
                        v["viseur_portee"] = Num(detectors.Max(d => d.GetVisualRange()));
                        v["viseur_grossissement"] = Num(detectors.Max(d => d.GetVisualMagnification()));
                    }
                    if (aircraft != null) v["eots"] = Flag(AircraftReader.HasEots(aircraft));
                });
                Section(def, "désignateurs laser", () =>
                {
                    AircraftReader.ReadLaser(prefab, out int targets, out double? range);
                    v["laser_cibles"] = targets.ToString(CultureInfo.InvariantCulture);
                    v["laser_portee"] = Num(range);
                });
                Section(def, "emports", () => Hardpoints(key, prefab, aircraft, v, hardpointRows));
            }

            return string.Join(";", AircraftColumns.Select(c => v.TryGetValue(c, out var s) ? s : ""));
        }

        private static void Hardpoints(string key, GameObject prefab, Component aircraft, Dictionary<string, string> v, List<string> rows)
        {
            var sets = AircraftReader.ReadHardpointSets(prefab, aircraft);
            if (sets == null)
            {
                v["nb_emplacements"] = "0";
                v["nb_points"] = "0";
                v["nb_soutes"] = "0";
                v["canons_fixes"] = "0";
                return;
            }
            int points = 0, bays = 0, builtIn = 0;
            for (int i = 0; i < sets.Length; i++)
            {
                var set = sets[i];
                if (set == null) continue;
                var hps = set.hardpoints?.Where(h => h != null).ToList() ?? new List<Hardpoint>();
                bool bay = hps.Any(h => h.bayDoors != null && h.bayDoors.Length > 0);
                int integres = hps.Sum(h => (h.BuiltInWeapons?.Length ?? 0) + (h.BuiltInTurrets?.Length ?? 0));
                points += hps.Count;
                if (bay) bays++;
                builtIn += integres;
                string prefix = string.Join(";", new[]
                {
                    Clean(key), i.ToString(CultureInfo.InvariantCulture), Clean(set.name), hps.Count.ToString(CultureInfo.InvariantCulture),
                    Flag(bay), Clean(set.SymmetryName),
                    set.precludingHardpointSets != null ? string.Join("|", set.precludingHardpointSets) : "",
                    integres.ToString(CultureInfo.InvariantCulture),
                });
                var options = set.weaponOptions?.Where(m => m != null).ToList() ?? new List<WeaponMount>();
                if (options.Count == 0) rows.Add(prefix + ";;;;;;");
                foreach (var m in options)
                {
                    string cat = WeaponCategories.Join(AircraftReader.SafeCategories(m), "|");
                    rows.Add(prefix + ";" + string.Join(";", new[]
                    {
                        Clean(m.jsonKey), Clean(m.mountName), Clean(m.info != null ? m.info.name : null), Num(m.mass),
                        m.ammo.ToString(CultureInfo.InvariantCulture), cat,
                    }));
                }
            }
            v["nb_emplacements"] = sets.Count(s => s != null).ToString(CultureInfo.InvariantCulture);
            v["nb_points"] = points.ToString(CultureInfo.InvariantCulture);
            v["nb_soutes"] = bays.ToString(CultureInfo.InvariantCulture);
            v["canons_fixes"] = builtIn.ToString(CultureInfo.InvariantCulture);
        }

        private static void Section(AircraftDefinition def, string what, Action action)
        {
            try { action(); }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[Wiki] Export {def?.jsonKey} ({what}) incomplet : {e.GetBaseException().Message}");
            }
        }

        private static string Num(double? v) => v == null ? "" : Num((float)v.Value);

        private static string Num(float v) =>
            float.IsNaN(v) || float.IsInfinity(v) ? "" : v.ToString("0.######", CultureInfo.InvariantCulture);

        private static string Clean(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace(';', ',').Replace('\n', ' ').Replace('\r', ' ').Trim();

        private static string Flag(bool b) => b ? "1" : "0";
    }
}

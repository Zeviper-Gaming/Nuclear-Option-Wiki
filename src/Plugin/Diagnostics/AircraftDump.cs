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

        // Aircraft dérive de Mirage.NetworkBehaviour (assembly non référencé) : ses champs sont lus par réflexion.
        private static FieldInfo _isEventContent, _jetIrMax, _fanIrMin, _fanIrMax, _eots, _weaponManager, _laserRange;
        private static bool _resolved;

        /// <summary>Écrit (ou met à jour) les deux fichiers ; renvoie un message court pour le bouton du menu F1.</summary>
        public static string Export()
        {
            try
            {
                Resolve();
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

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            _isEventContent = AccessTools.Field(typeof(UnitDefinition), "isEventContent");
            _jetIrMax = AccessTools.Field(typeof(JetNozzle), "IRMax");
            _fanIrMin = AccessTools.Field(typeof(DuctedFan), "IRMin");
            _fanIrMax = AccessTools.Field(typeof(DuctedFan), "IRMax");
            _eots = AccessTools.Field(typeof(Aircraft), "EOTS");
            _weaponManager = AccessTools.Field(typeof(Aircraft), "weaponManager");
            _laserRange = AccessTools.Field(typeof(LaserDesignator), "range");
            if (_isEventContent == null) Plugin.Log.LogWarning("[Wiki] UnitDefinition.isEventContent introuvable : colonne « evenementiel » vide.");
            if (_jetIrMax == null) Plugin.Log.LogWarning("[Wiki] JetNozzle.IRMax introuvable : IR sans postcombustion incomplet.");
            if (_fanIrMin == null || _fanIrMax == null) Plugin.Log.LogWarning("[Wiki] DuctedFan.IRMin/IRMax introuvables : IR des soufflantes carénées absent.");
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
                if (_isEventContent != null) v["evenementiel"] = Flag((bool)_isEventContent.GetValue(def));
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
            Section(def, "prefab", () => aircraft = prefab != null ? prefab.GetComponentInChildren(typeof(Aircraft), true) : null);
            if (prefab != null)
            {
                Section(def, "masse des pièces", () =>
                    v["masse_parts"] = Num(prefab.GetComponentsInChildren<UnitPart>(true).Where(p => p != null).Sum(p => p.mass)));
                Section(def, "moteurs", () => Engines(prefab, v));
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
                    if (aircraft != null && _eots != null) v["eots"] = Flag(_eots.GetValue(aircraft) as TargetDetector != null);
                });
                Section(def, "désignateurs laser", () =>
                {
                    // Désignateurs intégrés à l'appareil (nacelles exclues : ce sont des emports, absents du prefab).
                    var lasers = prefab.GetComponentsInChildren<LaserDesignator>(true).Where(l => l != null).ToList();
                    v["laser_cibles"] = lasers.Sum(l => l.GetMaxTargets()).ToString(CultureInfo.InvariantCulture);
                    if (lasers.Count > 0 && _laserRange != null) v["laser_portee"] = Num(lasers.Max(l => (float)_laserRange.GetValue(l)));
                });
                Section(def, "emports", () => Hardpoints(key, prefab, aircraft, v, hardpointRows));
            }

            return string.Join(";", AircraftColumns.Select(c => v.TryGetValue(c, out var s) ? s : ""));
        }

        /// <summary>IR par moteur (plan A11) : max des moteurs pour le ralenti, le plein gaz sec et le plein gaz avec postcombustion.</summary>
        private static void Engines(GameObject prefab, Dictionary<string, string> v)
        {
            var types = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var c in prefab.GetComponentsInChildren<Component>(true))
                if (c is IEngine) types.Add(c.GetType().Name);
            float irMin = float.NaN, irSec = float.NaN, irMax = float.NaN;
            void Take(float min, float sec, float max)
            {
                irMin = Max(irMin, min);
                irSec = Max(irSec, sec);
                irMax = Max(irMax, max);
            }
            foreach (var n in prefab.GetComponentsInChildren<JetNozzle>(true))
            {
                types.Add(nameof(JetNozzle));
                float sec = _jetIrMax != null ? (float)_jetIrMax.GetValue(n) : float.NaN;
                Take(n.GetIRMin(), sec, n.GetIRMax());
            }
            foreach (var t in prefab.GetComponentsInChildren<TurbineEngine>(true))
                Take(t.GetIRMin(), t.GetIRMax(), t.GetIRMax());
            if (_fanIrMin != null && _fanIrMax != null)
                foreach (var f in prefab.GetComponentsInChildren<DuctedFan>(true))
                {
                    float max = (float)_fanIrMax.GetValue(f);
                    Take((float)_fanIrMin.GetValue(f), max, max);
                }
            v["moteurs"] = string.Join("|", types);
            v["ir_min"] = Num(irMin);
            v["ir_max_sec"] = Num(irSec);
            v["ir_max"] = Num(irMax);
        }

        private static float Max(float a, float b) => float.IsNaN(a) ? b : float.IsNaN(b) ? a : Math.Max(a, b);

        private static void Hardpoints(string key, GameObject prefab, Component aircraft, Dictionary<string, string> v, List<string> rows)
        {
            WeaponManager manager = aircraft != null && _weaponManager != null ? _weaponManager.GetValue(aircraft) as WeaponManager : null;
            if (manager == null) manager = prefab.GetComponentInChildren<WeaponManager>(true);
            var sets = manager != null ? manager.hardpointSets : null;
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
                    string cat = "";
                    try { cat = WeaponCategories.Join(WeaponCategories.Of(Flags(m)), "|"); }
                    catch (Exception e) { Plugin.Log.LogDebug($"[Wiki] Catégorie illisible pour {m.jsonKey} : {e.GetBaseException().Message}"); }
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

        internal static WeaponFlags Flags(WeaponMount m)
        {
            var f = new WeaponFlags { MountCargo = m.Cargo, MountTroops = m.Troops };
            var i = m.info;
            if (i == null) return f;
            f.HasInfo = true;
            f.Cargo = i.cargo;
            f.Troops = i.troops;
            f.Sling = i.sling;
            f.Jammer = i.jammer;
            f.Energy = i.energy;
            f.Gun = i.gun;
            f.Bomb = i.bomb;
            f.GlideBomb = i.glideBomb;
            f.Missile = i.missile;
            f.Nuclear = i.nuclear;
            f.LaserGuided = i.laserGuided;
            f.AntiAir = i.effectiveness.antiAir;
            f.AntiSurface = i.effectiveness.antiSurface;
            f.HasSeeker = i.weaponPrefab != null && i.weaponPrefab.GetComponentInChildren<MissileSeeker>(true) != null;
            // Missile dérive de Mirage.NetworkBehaviour : recherche non générique.
            f.HasMissileBody = i.weaponPrefab != null && i.weaponPrefab.GetComponentInChildren(typeof(Missile), true) != null;
            return f;
        }

        private static void Section(AircraftDefinition def, string what, Action action)
        {
            try { action(); }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[Wiki] Export {def?.jsonKey} ({what}) incomplet : {e.GetBaseException().Message}");
            }
        }

        private static string Num(float v) =>
            float.IsNaN(v) || float.IsInfinity(v) ? "" : v.ToString("0.######", CultureInfo.InvariantCulture);

        private static string Clean(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace(';', ',').Replace('\n', ' ').Replace('\r', ' ').Trim();

        private static string Flag(bool b) => b ? "1" : "0";
    }
}

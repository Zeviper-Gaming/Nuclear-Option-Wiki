using System;
using System.Collections.Generic;
using System.Linq;
using NuclearOptionWiki.Core;
using UnityEngine;

namespace NuclearOptionWiki.GameData
{
    /// <summary>
    /// Valeurs d'un appareil lues en jeu (plan avions A4 : repli ; emports, IR et désignateur toujours lus en jeu),
    /// mises en cache par définition : aucune lecture ni allocation par image. Toute erreur donne des valeurs nulles (N/A).
    /// </summary>
    internal static class LiveAircraftStats
    {
        private static readonly Dictionary<AircraftDefinition, LiveAircraft> Cache = new Dictionary<AircraftDefinition, LiveAircraft>();
        private static readonly Dictionary<AircraftDefinition, bool> AircraftKind = new Dictionary<AircraftDefinition, bool>();
        private static double? _globalIrMax;
        private static bool _globalIrDone;

        public static LiveAircraft Get(AircraftDefinition def)
        {
            if (def == null) return new LiveAircraft();
            if (Cache.TryGetValue(def, out var cached))
            {
                cached.IrGlobalMax = GlobalIrMax();
                return cached;
            }
            var l = new LiveAircraft { JsonKey = def.jsonKey, UnitName = def.unitName, Code = def.code, DescriptionEn = def.description };
            Guard(def, "définition", () =>
            {
                l.CostMillions = def.value;
                l.LengthM = Positive(def.length);
                l.WidthM = Positive(def.width);
                l.RcsM2 = Positive(def.radarSize);
                l.Armor = def.armorTier;
                var p = def.aircraftParameters;
                if (p != null)
                {
                    l.Rank = p.rankRequired;
                    l.GLimit = Positive(p.aircraftGLimit);
                    l.VerticalTakeoff = p.verticalLanding;
                    l.TakeoffMs = Positive(p.takeoffSpeed);
                    l.TakeoffM = Positive(p.takeoffDistance);
                }
                var i = def.aircraftInfo;
                if (i != null)
                {
                    l.SpeedKmh = Positive(i.maxSpeed);
                    l.StallKmh = Positive(i.stallSpeed);
                    l.Maneuverability = i.maneuverability;
                    l.EmptyMassKg = Positive(i.emptyWeight);
                    l.MaxMassKg = Positive(i.maxWeight);
                }
            });
            var prefab = def.unitPrefab;
            if (prefab != null)
            {
                Component aircraft = null;
                Guard(def, "prefab", () => aircraft = AircraftReader.FindAircraft(prefab));
                Guard(def, "IR", () =>
                {
                    var ir = AircraftReader.ReadInfrared(prefab);
                    l.IrMin = ir.Min;
                    l.IrMaxDry = ir.MaxDry;
                    l.IrMax = ir.Max;
                });
                Guard(def, "désignateur laser", () =>
                {
                    AircraftReader.ReadLaser(prefab, out int targets, out double? range);
                    l.LaserTargets = targets;
                    l.LaserRangeM = range;
                });
                Guard(def, "emports", () => l.Hardpoints = AircraftReader.ReadHardpoints(prefab, aircraft));
            }
            l.IrGlobalMax = GlobalIrMax();
            Cache[def] = l;
            return l;
        }

        /// <summary>A22 : vrai pour un avion ou un hélicoptère, faux pour un navire ou un véhicule terrestre (mis en cache).</summary>
        public static bool IsAircraft(AircraftDefinition def)
        {
            if (def == null) return false;
            if (AircraftKind.TryGetValue(def, out var kind)) return kind;
            bool res = false;
            Guard(def, "nature", () => res = AircraftReader.IsAircraft(def.unitPrefab, out _));
            AircraftKind[def] = res;
            return res;
        }

        /// <summary>
        /// A11 (amendé v3.3) : IR plein gaz le plus fort de tous les appareils de l'encyclopédie (hors contenu événementiel,
        /// hors navires et véhicules), IR corrigé quand une correction existe. Calculé une fois par session.
        /// </summary>
        public static double? GlobalIrMax()
        {
            if (_globalIrDone) return _globalIrMax;
            _globalIrDone = true;
            try
            {
                var defs = new HashSet<AircraftDefinition>();
                try { if (Encyclopedia.i?.aircraft != null) defs.UnionWith(Encyclopedia.i.aircraft.Where(d => d != null)); }
                catch (Exception e) { Plugin.Log.LogWarning($"[Wiki] Encyclopédie illisible (IR max) : {e.Message}"); }
                defs.UnionWith(Resources.FindObjectsOfTypeAll<AircraftDefinition>().Where(d => d != null));
                foreach (var def in defs)
                {
                    if (AircraftDatabase.IsExcluded(def.jsonKey, AircraftReader.IsEventContent(def)) || !IsAircraft(def)) continue;
                    double? ir = null;
                    var record = Plugin.Aircraft?.Find(def.jsonKey, def.unitName).Value;
                    if (record?.IrForced?.IrMax != null) ir = record.IrForced.IrMax;
                    else if (def.unitPrefab != null)
                    {
                        try { ir = AircraftReader.ReadInfrared(def.unitPrefab).Max; }
                        catch (Exception e) { Plugin.Log.LogDebug($"[Wiki] IR illisible pour {def.jsonKey} : {e.GetBaseException().Message}"); }
                    }
                    if (ir != null && (_globalIrMax == null || ir.Value > _globalIrMax.Value)) _globalIrMax = ir;
                }
                Plugin.Log.LogInfo($"[Wiki] IR plein gaz max (échelle des voyants IR) : {_globalIrMax?.ToString("0.##") ?? "inconnu"} sur {defs.Count} définitions.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[Wiki] Calcul de l'IR max impossible : {e.GetBaseException().Message}");
            }
            return _globalIrMax;
        }

        /// <summary>Après un rechargement des données (corrections d'IR).</summary>
        public static void ResetGlobal()
        {
            _globalIrDone = false;
            _globalIrMax = null;
        }

        private static double? Positive(float v) => float.IsNaN(v) || v <= 0 ? (double?)null : v;

        private static void Guard(AircraftDefinition def, string what, Action action)
        {
            try { action(); }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[Wiki] Lecture de {def?.jsonKey} ({what}) incomplète : {e.GetBaseException().Message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NuclearOptionWiki.Core;
using UnityEngine;

namespace NuclearOptionWiki.GameData
{
    /// <summary>
    /// Lecture du prefab d'un appareil, partagée par l'export (AircraftDump) et la fiche (LiveAircraftStats) :
    /// IR des moteurs (A11), désignateurs laser (A21), emports (A9, A23), drapeaux des options (7.5), nature de l'appareil (A22).
    /// Aircraft et Missile dérivent de types réseau non référencés : accès par réflexion ou recherche non générique.
    /// </summary>
    internal static class AircraftReader
    {
        private static bool _resolved;
        private static FieldInfo _isEventContent, _jetIrMax, _fanIrMin, _fanIrMax, _eots, _weaponManager, _laserRange;

        // A22 : composants qui font un aéronef, ou au contraire un navire / un véhicule terrestre.
        private static readonly HashSet<string> FlightParts = new HashSet<string> { "JetNozzle", "ConstantSpeedProp", "PropFan", "RotorShaft" };
        private static readonly HashSet<string> SurfaceParts = new HashSet<string> { "PlayerShipPropulsion", "WheelEngine" };

        public struct Infrared
        {
            public double? Min, MaxDry, Max;
            public string EngineTypes;
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
            if (_isEventContent == null) Plugin.Log.LogWarning("[Wiki] UnitDefinition.isEventContent introuvable : contenu événementiel non détecté.");
            if (_jetIrMax == null) Plugin.Log.LogWarning("[Wiki] JetNozzle.IRMax introuvable : IR sans postcombustion incomplet.");
            if (_fanIrMin == null || _fanIrMax == null) Plugin.Log.LogWarning("[Wiki] DuctedFan.IRMin/IRMax introuvables : IR des soufflantes carénées absent.");
        }

        public static bool IsEventContent(UnitDefinition def)
        {
            Resolve();
            return _isEventContent != null && def != null && (bool)_isEventContent.GetValue(def);
        }

        public static Component FindAircraft(GameObject prefab) =>
            prefab != null ? prefab.GetComponentInChildren(typeof(Aircraft), true) : null;

        public static bool HasEots(Component aircraft)
        {
            Resolve();
            return aircraft != null && _eots != null && _eots.GetValue(aircraft) as TargetDetector != null;
        }

        /// <summary>A22 : vrai si le prefab vole (tuyère, hélice, rotor) sans propulsion navale ni roues motrices.</summary>
        public static bool IsAircraft(GameObject prefab, out string engineTypes)
        {
            engineTypes = "";
            if (prefab == null) return false;
            var types = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var c in prefab.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (c is IEngine || FlightParts.Contains(n) || SurfaceParts.Contains(n)) types.Add(n);
            }
            engineTypes = string.Join("|", types);
            return !types.Overlaps(SurfaceParts) && types.Overlaps(FlightParts);
        }

        /// <summary>A11 : max des moteurs pour le ralenti, le plein gaz sec et le plein gaz avec postcombustion.</summary>
        public static Infrared ReadInfrared(GameObject prefab)
        {
            Resolve();
            var ir = new Infrared();
            var types = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var c in prefab.GetComponentsInChildren<Component>(true))
                if (c is IEngine) types.Add(c.GetType().Name);
            void Take(double min, double dry, double max)
            {
                ir.Min = ir.Min == null ? min : Math.Max(ir.Min.Value, min);
                ir.MaxDry = ir.MaxDry == null ? dry : Math.Max(ir.MaxDry.Value, dry);
                ir.Max = ir.Max == null ? max : Math.Max(ir.Max.Value, max);
            }
            foreach (var n in prefab.GetComponentsInChildren<JetNozzle>(true))
            {
                types.Add(nameof(JetNozzle));
                double max = n.GetIRMax();
                Take(n.GetIRMin(), _jetIrMax != null ? (float)_jetIrMax.GetValue(n) : max, max);
            }
            foreach (var t in prefab.GetComponentsInChildren<TurbineEngine>(true))
                Take(t.GetIRMin(), t.GetIRMax(), t.GetIRMax());
            if (_fanIrMin != null && _fanIrMax != null)
                foreach (var f in prefab.GetComponentsInChildren<DuctedFan>(true))
                {
                    double max = (float)_fanIrMax.GetValue(f);
                    Take((float)_fanIrMin.GetValue(f), max, max);
                }
            ir.EngineTypes = string.Join("|", types);
            return ir;
        }

        /// <summary>A21 : désignateurs laser intégrés (nacelles exclues : ce sont des emports, absents du prefab).</summary>
        public static void ReadLaser(GameObject prefab, out int targets, out double? rangeM)
        {
            Resolve();
            var lasers = prefab.GetComponentsInChildren<LaserDesignator>(true).Where(l => l != null).ToList();
            targets = lasers.Sum(l => l.GetMaxTargets());
            rangeM = lasers.Count > 0 && _laserRange != null ? lasers.Max(l => (double)(float)_laserRange.GetValue(l)) : (double?)null;
        }

        public static HardpointSet[] ReadHardpointSets(GameObject prefab, Component aircraft)
        {
            Resolve();
            WeaponManager manager = aircraft != null && _weaponManager != null ? _weaponManager.GetValue(aircraft) as WeaponManager : null;
            if (manager == null) manager = prefab.GetComponentInChildren<WeaponManager>(true);
            return manager != null ? manager.hardpointSets : null;
        }

        /// <summary>Emplacements résumés pour la fiche (A9, A23 appliqués ensuite par le Core).</summary>
        public static List<LiveHardpoint> ReadHardpoints(GameObject prefab, Component aircraft)
        {
            var res = new List<LiveHardpoint>();
            var sets = ReadHardpointSets(prefab, aircraft);
            if (sets == null) return res;
            for (int index = 0; index < sets.Length; index++)
            {
                var set = sets[index];
                if (set == null) continue;
                var hps = set.hardpoints?.Where(h => h != null).ToList() ?? new List<Hardpoint>();
                bool bay = hps.Any(h => h.bayDoors != null && h.bayDoors.Length > 0);
                var options = (set.weaponOptions ?? new List<WeaponMount>()).Where(m => m != null)
                    .Select(m => new KeyValuePair<double, List<WeaponCategory>>(m.mass, SafeCategories(m)));
                var h = LiveHardpoint.Summarize(set.name, hps.Count, bay,
                    set.precludingHardpointSets != null && set.precludingHardpointSets.Count > 0, options);
                h.Index = index;
                if (set.precludingHardpointSets != null) foreach (var b in set.precludingHardpointSets) h.Excludes.Add(b);
                res.Add(h);
            }
            return res;
        }

        public static List<WeaponCategory> SafeCategories(WeaponMount m)
        {
            try { return WeaponCategories.Of(Flags(m)); }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"[Wiki] Catégorie illisible pour {m.jsonKey} : {e.GetBaseException().Message}");
                return new List<WeaponCategory>();
            }
        }

        public static WeaponFlags Flags(WeaponMount m)
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
    }
}

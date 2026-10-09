using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NuclearOptionWiki.Core;
using UnityEngine;

namespace NuclearOptionWiki.GameData
{
    /// <summary>
    /// Valeurs réelles lues sur le prefab d'une arme propulsée (composant Missile), décision D14 :
    /// RCS (definition.radarSize), intensité IR du premier moteur (seul à créer la signature IR en jeu),
    /// taux de virage max (°/s) et limite en G. Lecture par réflexion (Missile dérive de types réseau
    /// non référencés), mise en cache par WeaponInfo. Toute erreur donne « — » sans exception.
    /// </summary>
    internal static class LiveWeaponStats
    {
        private static readonly Dictionary<WeaponInfo, LiveStats> Cache = new Dictionary<WeaponInfo, LiveStats>();
        private static bool _resolved, _available, _warned;
        private static Type _missileType;
        private static FieldInfo _definition, _motors, _gLimit, _maxTurnRate, _irIntensity;

        public static LiveStats Get(WeaponInfo info)
        {
            if (info == null || info.weaponPrefab == null) return LiveStats.None;
            if (Cache.TryGetValue(info, out var cached)) return cached;

            var stats = LiveStats.None;
            try
            {
                if (Resolve())
                {
                    var missile = info.weaponPrefab.GetComponent(_missileType);
                    if (missile != null) stats = Read(missile);
                }
            }
            catch (Exception e)
            {
                WarnOnce($"lecture de {info.name} impossible : {e.GetBaseException().Message}");
            }
            Cache[info] = stats;
            return stats;
        }

        private static LiveStats Read(Component missile)
        {
            var s = new LiveStats();
            if (_definition?.GetValue(missile) is UnitDefinition def && def.radarSize > 0f) s.RcsM2 = def.radarSize;
            if (_motors?.GetValue(missile) is Array motors && motors.Length > 0 && _irIntensity != null)
            {
                var first = motors.GetValue(0);
                if (first != null && _irIntensity.GetValue(first) is float ir && ir > 0f) s.IrIntensity = ir;
            }
            if (_maxTurnRate?.GetValue(missile) is float turn && turn > 0f) s.TurnRateDegS = turn;
            if (_gLimit?.GetValue(missile) is float g && g > 0f) s.GLimit = g;
            return s;
        }

        private static bool Resolve()
        {
            if (_resolved) return _available;
            _resolved = true;
            _missileType = AccessTools.TypeByName("Missile");
            if (_missileType == null)
            {
                WarnOnce("type Missile introuvable");
                return _available = false;
            }
            _definition = AccessTools.Field(_missileType, "definition");
            _motors = AccessTools.Field(_missileType, "motors");
            _gLimit = AccessTools.Field(_missileType, "gLimit");
            _maxTurnRate = AccessTools.Field(_missileType, "maxTurnRate");
            var motorType = _motors?.FieldType.GetElementType();
            _irIntensity = motorType != null ? AccessTools.Field(motorType, "IR_intensity") : null;

            if (_definition == null || _motors == null || _gLimit == null || _maxTurnRate == null || _irIntensity == null)
                WarnOnce("certains champs du missile sont introuvables (version du jeu différente ?) : valeurs partielles");
            return _available = true;
        }

        private static void WarnOnce(string message)
        {
            if (_warned) return;
            _warned = true;
            Plugin.Log.LogWarning("[Wiki] Valeurs en jeu : " + message);
        }
    }
}

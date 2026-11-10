using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using NuclearOptionWiki.Core;
using UnityEngine;

namespace NuclearOptionWiki.GameData
{
    /// <summary>
    /// Lecture d'une arme partagée par l'export (WeaponDump) et la fiche (LiveWeaponStats) : valeurs de base,
    /// pénétration et charge réelles des missiles, profil d'attaque reconstruit depuis les réglages de l'autodirecteur
    /// (même vocabulaire que la colonne « Méthode d'approche » du classeur). Champs privés lus par réflexion.
    /// </summary>
    internal static class WeaponReader
    {
        private static bool _resolved;
        private static Type _missileType;
        private static MethodInfo _getPierce, _getYield;
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> Fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static Type MissileType
        {
            get { Resolve(); return _missileType; }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            _missileType = AccessTools.TypeByName("Missile");
            if (_missileType == null) return;
            _getPierce = AccessTools.Method(_missileType, "GetPierce");
            _getYield = AccessTools.Method(_missileType, "GetYield");
        }

        /// <summary>Remplit les valeurs de base de <paramref name="s"/> (null si illisibles ou sans objet).</summary>
        public static void ReadBasics(WeaponInfo info, ref LiveStats s)
        {
            Resolve();
            s.HasInfo = true;
            s.Flags = AircraftReader.FlagsOf(info);
            s.CostMillions = Finite(info.costPerRound);
            s.MassKg = Positive(info.massPerRound);
            s.MaxRangeM = Positive(info.targetRequirements.maxRange);
            s.MinRangeM = Finite(info.targetRequirements.minRange);
            s.TargetMaxSpeed = Finite(info.targetRequirements.maxSpeed);
            s.Penetration = Finite(info.pierceDamage);
            s.ExplosiveKg = Finite(info.blastDamage);
            var prefab = info.weaponPrefab;
            var missile = prefab != null && _missileType != null ? prefab.GetComponent(_missileType) : null;
            if (missile != null)
            {
                if (_getPierce != null) s.Penetration = Finite(Convert.ToSingle(_getPierce.Invoke(missile, null)));
                if (_getYield != null) s.ExplosiveKg = Finite(Convert.ToSingle(_getYield.Invoke(missile, null)));
            }
            s.SpeedMs = Positive(info.GetMaxSpeed());
            var seeker = prefab != null ? prefab.GetComponentInChildren<MissileSeeker>(true) : null;
            if (seeker != null) s.Approach = Approach(seeker);
        }

        /// <summary>
        /// Profil d'attaque, dans l'ordre du classeur : datalink ; loft ; attaque par le haut ; angle d'attaque haut ;
        /// rase-flots / croisière ; évasion zigzag ; évite le relief ; accélération terminale ; phase terminale. Null si rien.
        /// </summary>
        public static string Approach(MissileSeeker seeker)
        {
            var parts = new List<string>();
            var t = seeker.GetType();
            if (Get(seeker, t, "useDatalink") is bool datalink && datalink) parts.Add("datalink");
            if (Get(seeker, t, "loftAmount") is float loft && loft > 0) parts.Add("loft " + Num(loft));
            var top = Get(seeker, t, "topAttack");
            if (top != null && Get(top, top.GetType(), "Amount") is float topAmount && topAmount > 0)
            {
                string proba = Get(top, top.GetType(), "probability") is float p ? p.ToString("0.0#", CultureInfo.InvariantCulture) : "?";
                string min = Get(top, top.GetType(), "minRange") is float a ? Num(a) : "?";
                string max = Get(top, top.GetType(), "maxRange") is float b ? Num(b) : "?";
                parts.Add($"attaque par le haut (proba {proba}, {min}-{max} m)");
            }
            if (Get(seeker, t, "topAttackAngle") is float angle && angle > 0) parts.Add("angle d'attaque haut " + angle.ToString("0.0", CultureInfo.InvariantCulture) + "°");
            if (Get(seeker, t, "altitudeTarget") is float alt && alt > 0)
                parts.Add(alt <= 200 ? "rase-flots/rase-sol " + alt.ToString("0.0", CultureInfo.InvariantCulture) + " m" : "croisière à " + Num(alt) + " m");
            var jink = Get(seeker, t, "jinkEvasion");
            if (jink != null && Get(jink, jink.GetType(), "amount") is float zig && zig > 0) parts.Add("évasion zigzag " + Num(zig));
            if (Get(seeker, t, "terrainAvoidance") is bool terrain && terrain) parts.Add("évite le relief");
            var boost = Get(seeker, t, "terminalBoost");
            if (boost != null && Get(boost, boost.GetType(), "Amount") is float boostAmount && boostAmount > 0)
            {
                string min = Get(boost, boost.GetType(), "minRange") is float a ? Num(a) : "?";
                string max = Get(boost, boost.GetType(), "maxRange") is float b ? Num(b) : "?";
                parts.Add($"accélération terminale ({min}-{max} m)");
            }
            if (Get(seeker, t, "terminalRange") is float terminal && terminal > 0) parts.Add("phase terminale à " + Num(terminal) + " m");
            return parts.Count > 0 ? string.Join("; ", parts) : null;
        }

        /// <summary>Champ (public ou privé) par nom sur le type réel ou ses parents ; null s'il n'existe pas.</summary>
        private static object Get(object target, Type type, string name)
        {
            if (!Fields.TryGetValue(type, out var byName)) Fields[type] = byName = new Dictionary<string, FieldInfo>();
            if (!byName.TryGetValue(name, out var field)) byName[name] = field = FindField(type, name);
            return field?.GetValue(target);
        }

        /// <summary>Recherche silencieuse (AccessTools.Field écrit un avertissement Harmony pour chaque champ absent).</summary>
        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(name, all);
                if (f != null) return f;
            }
            return null;
        }

        private static string Num(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static double? Finite(float v) => float.IsNaN(v) || float.IsInfinity(v) ? (double?)null : v;
        private static double? Positive(float v) => float.IsNaN(v) || float.IsInfinity(v) || v <= 0 ? (double?)null : v;
    }
}

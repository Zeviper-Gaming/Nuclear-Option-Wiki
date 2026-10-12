using System.Collections.Generic;

namespace NuclearOptionWiki.Core
{
    /// <summary>Catégories de compatibilité d'un emplacement (plan avions, section 7.5), dans l'ordre d'affichage.</summary>
    public enum WeaponCategory
    {
        Canon,
        MissileAA,
        MissileAS,
        Roquette,
        Bombe,
        Nuc,
        Laser,
        Brouilleur,
        Cargo,
    }

    /// <summary>Drapeaux d'une option d'emport, lus en jeu sur le WeaponMount et son WeaponInfo (sans dépendance à Unity).</summary>
    public struct WeaponFlags
    {
        public bool MountCargo, MountTroops;
        public bool HasInfo;
        public bool Cargo, Troops, Sling, Jammer, Energy, Gun, Bomb, GlideBomb, Missile, Nuclear;
        public bool HasSeeker, LaserGuided;
        /// <summary>Le projectile est un corps de missile (fusée) : sert à reconnaître les roquettes AGR sans drapeau d'arme.</summary>
        public bool HasMissileBody;
        public float AntiAir, AntiSurface;
    }

    public static class WeaponCategories
    {
        private static readonly string[] Labels =
            { "CANON", "MISSILE AA", "MISSILE AS", "ROQUETTE", "BOMBE", "NUC", "LASER", "BROUILLEUR", "CARGO" };

        public static string Label(WeaponCategory c) => Labels[(int)c];

        public static bool TryParse(string label, out WeaponCategory category)
        {
            string s = (label ?? "").Trim().ToUpperInvariant();
            for (int i = 0; i < Labels.Length; i++)
            {
                if (Labels[i] == s)
                {
                    category = (WeaponCategory)i;
                    return true;
                }
            }
            category = default;
            return false;
        }

        /// <summary>Catégorie principale (règle 7.5 amendée v3.2, première condition vraie), ou null (réservoir, nacelle, leurres…).</summary>
        public static WeaponCategory? Primary(WeaponFlags f)
        {
            if (f.MountCargo || f.MountTroops || f.Cargo || f.Troops || f.Sling) return WeaponCategory.Cargo;
            if (!f.HasInfo) return null;
            if (f.Jammer) return WeaponCategory.Brouilleur;
            if (f.Energy) return WeaponCategory.Laser;
            if (f.Gun) return WeaponCategory.Canon;
            if (f.Bomb || f.GlideBomb) return WeaponCategory.Bombe;
            if (f.Missile && !f.HasSeeker && !f.LaserGuided) return WeaponCategory.Roquette;
            // Roquettes AGR-18/24/37 : aucun drapeau missile/bombe/canon dans le jeu, mais un projectile autopropulsé (amendement v3.2).
            if (!f.Missile && f.HasMissileBody) return WeaponCategory.Roquette;
            if (f.Missile && f.AntiAir > f.AntiSurface) return WeaponCategory.MissileAA;
            if (f.Missile) return WeaponCategory.MissileAS;
            return null;
        }

        /// <summary>Catégories d'une option : la principale, plus NUC si l'arme est nucléaire.</summary>
        public static List<WeaponCategory> Of(WeaponFlags f)
        {
            var list = new List<WeaponCategory>(2);
            var primary = Primary(f);
            if (primary.HasValue) list.Add(primary.Value);
            if (f.HasInfo && f.Nuclear && !list.Contains(WeaponCategory.Nuc)) list.Add(WeaponCategory.Nuc);
            return list;
        }

        /// <summary>Union triée dans l'ordre d'affichage.</summary>
        public static List<WeaponCategory> Union(IEnumerable<IEnumerable<WeaponCategory>> lists)
        {
            var seen = new bool[Labels.Length];
            foreach (var l in lists)
                foreach (var c in l)
                    seen[(int)c] = true;
            var result = new List<WeaponCategory>();
            for (int i = 0; i < seen.Length; i++)
                if (seen[i]) result.Add((WeaponCategory)i);
            return result;
        }

        public static string Join(IEnumerable<WeaponCategory> cats, string separator)
        {
            var parts = new List<string>();
            foreach (var c in cats) parts.Add(Label(c));
            return string.Join(separator, parts);
        }
    }
}

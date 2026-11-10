using System;
using System.Globalization;

namespace NuclearOptionWiki.Core
{
    /// <summary>
    /// Notes estimées pour les valeurs absentes de weapons.json (armes des mods ajoutés, cases vides du classeur),
    /// calculées avec les modèles du classeur de Florian NO_efficacite_armes.xlsx (feuilles « Lisez-moi »,
    /// « Paramètres cibles », « Défenses &amp; contre-mesures »). Vérifié contre ce classeur : 95 % des notes
    /// d'efficacité identiques (le reste à ±1), règle d'applicabilité identique pour les 98 armes, N laser identique
    /// pour toutes les armes notées par régression.
    /// </summary>
    public static class WeaponEstimates
    {
        // ------------------------------------------------------------------ Efficacité (coups pour détruire)
        private struct TargetClass
        {
            public double Armor, PierceTolerance, DamageTolerance, BlastThreshold, MinTrackedSpeed;
            public TargetClass(double armor, double tol, double dt, double h, double minSpeed)
            {
                Armor = armor; PierceTolerance = tol; DamageTolerance = dt; BlastThreshold = h; MinTrackedSpeed = minSpeed;
            }
        }

        // Ordre de FicheModel.AirToGroundLabels puis AirToAirLabels. Paramètres de « Paramètres cibles »,
        // calés sur « Notes efficacité » (chars, bâtiments et bunker : représentants multiples ramenés à un seul).
        private static readonly TargetClass[] Ground =
        {
            new TargetClass(100, 5, 0.5, 0.6, 20),     // véhicules légers
            new TargetClass(450, 5, 3, 7.01, 20),      // chars
            new TargetClass(70, 400, 3, 0.6, 0),       // bâtiments légers
            new TargetClass(100, 500, 8, 0.6, 0),      // gros bâtiments
            new TargetClass(1510, 20, 3, 10.2, 0),     // bunker
            new TargetClass(125, 500, 3.2, 0.6, 20),   // navire léger
            new TargetClass(175, 500, 14, 0.6, 20),    // navire moyen
            new TargetClass(400, 1000, 15, 9, 20),     // porte-avions
        };

        private static readonly TargetClass[] Air =
        {
            new TargetClass(15, 106.1, 0.71, 0.6, 100),  // petit avion
            new TargetClass(15, 106.1, 0.71, 0.6, 50),   // hélico
            new TargetClass(15, 150, 1, 0.6, 200),       // moyen avion
            new TargetClass(15, 273.9, 2.74, 0.6, 150),  // gros avion
        };

        private const double PierceAngleFactor = 0.78, BlastK = 33.6, BlastExponent = 0.45;

        /// <summary>Données d'une arme utiles aux estimations (JSON en priorité, sinon lues en jeu).</summary>
        public struct Inputs
        {
            public double? Penetration, ExplosiveKg, MassKg, SpeedMs, RcsM2;
            public int? IrNote;
            public bool AntiSurface, AntiAir, Nuclear, Energy, Glide, Bomb, Projectile;
            public double? TargetMaxSpeed;   // null ou ≤ 0 = illimitée
            public string Approach;
        }

        /// <summary>Note /5 contre la classe air-sol <paramref name="index"/> (0..7), null si l'arme ne peut pas l'engager.</summary>
        public static int? GroundNote(int index, Inputs w) => w.AntiSurface ? Note(Ground[index], w) : null;

        /// <summary>Note /5 contre la classe air-air <paramref name="index"/> (0..3), null si l'arme ne peut pas l'engager.</summary>
        public static int? AirNote(int index, Inputs w) => w.AntiAir ? Note(Air[index], w) : null;

        private static int? Note(TargetClass c, Inputs w)
        {
            if (w.Energy) return null;                     // lasers : dégâts de feu non documentés (classeur)
            if (w.TargetMaxSpeed != null && w.TargetMaxSpeed.Value > 0 && w.TargetMaxSpeed.Value < c.MinTrackedSpeed) return null;
            if (w.Nuclear) return 5;                       // « 1 coup sur tout »
            if (w.Penetration == null && w.ExplosiveKg == null) return null;
            double pierce = Math.Max(0, (w.Penetration ?? 0) * PierceAngleFactor - c.Armor) / c.PierceTolerance;
            double blast = BlastK * Math.Max(0, Math.Pow(Math.Max(0, w.ExplosiveKg ?? 0), BlastExponent) - c.BlastThreshold) / c.DamageTolerance;
            return NoteFromDamage(pierce + blast);
        }

        /// <summary>Dégâts par coup (% des PV de la pièce clé) → note : 5 = 1 coup avec marge (≥ 143 %), 4 = 1 coup limite ou 2…</summary>
        public static int NoteFromDamage(double damagePercent)
        {
            if (damagePercent <= 0) return 0;
            double shots = Math.Ceiling(100 / damagePercent - 1e-9);
            if (shots <= 1) return damagePercent >= 143 ? 5 : 4;
            if (shots <= 2) return 4;
            if (shots <= 5) return 3;
            if (shots <= 12) return 2;
            if (shots <= 50) return 1;
            return 0;
        }

        // ------------------------------------------------------------------ Interception (vulnérabilité aux défenses)
        public const double BombSpeedKmh = 850, GlideBombSpeedKmh = 700;   // vitesse de l'avion porteur (classeur)

        /// <summary>Vrai si l'arme est une munition air-sol interceptable (missile, roquette, bombe), seul cas couvert par le classeur.</summary>
        public static bool Interceptable(Inputs w) => w.AntiSurface && w.Projectile && !w.Energy && w.MassKg != null && w.MassKg.Value > 0;

        public static double SpeedKmh(Inputs w)
        {
            if (w.SpeedMs != null && w.SpeedMs.Value > 0) return w.SpeedMs.Value * 3.6;
            return w.Glide ? GlideBombSpeedKmh : BombSpeedKmh;
        }

        /// <summary>N laser (HLT-HEL) : régression du classeur, ln N = 4,27 − 0,32·ln(masse) − 0,12·ln(vitesse km/h) + 0,27·planante.</summary>
        public static double? LaserN(Inputs w)
        {
            if (!Interceptable(w)) return null;
            return Math.Exp(4.27 - 0.32 * Math.Log(w.MassKg.Value) - 0.12 * Math.Log(SpeedKmh(w)) + (w.Glide ? 0.27 : 0));
        }

        /// <summary>N canon AA : 1 + 0,72 × temps dans la zone de 4 km × (RCS / 0,05)^0,2 × manœuvre ÷ (masse / 150)^0,25.</summary>
        public static double? CannonN(Inputs w)
        {
            if (!Interceptable(w)) return null;
            double v = SpeedKmh(w) / 3.6;
            double t = 4000 / v;
            if (w.Bomb && !w.Glide) t = Math.Min(t, 8);
            else if (w.Glide) t = Math.Min(t, 12);
            double rcs = w.RcsM2 != null && w.RcsM2.Value > 0 ? w.RcsM2.Value : 0.05;
            double man = 1;
            if (Zigzag(w.Approach)) man *= 0.5;
            if (Dive(w)) man *= 0.6;
            return 1 + 0.72 * t * Math.Pow(rcs / 0.05, 0.2) * man / Math.Pow(w.MassKg.Value / 150, 0.25);
        }

        /// <summary>N SAM courte portée : 1 + 4·p(IR) + 4·p(radar) (batterie IR et radar de 4 missiles).</summary>
        public static double? ShortSamN(Inputs w)
        {
            if (!Interceptable(w)) return null;
            double ir = Math.Max(0, w.IrNote ?? 0);
            double zig = Zigzag(w.Approach) ? 0.6 : 1;
            double pIr = Math.Min(0.85, 0.55 * Math.Sqrt(ir / 3) * zig * (Dive(w) ? 0.7 : 1));
            double rcs = w.RcsM2 != null && w.RcsM2.Value > 0 ? w.RcsM2.Value : 0.05;
            double pRad = Math.Min(0.85, 0.6 * Math.Pow(rcs / 0.05, 0.25) * (SeaSkimming(w.Approach) ? 0.4 : 1) * zig);
            return 1 + 4 * pIr + 4 * pRad;
        }

        /// <summary>N site AA complet = 1 + capacités laser + canon + SAM court.</summary>
        public static double? SiteN(Inputs w)
        {
            var l = LaserN(w);
            var c = CannonN(w);
            var s = ShortSamN(w);
            if (l == null || c == null || s == null) return null;
            return l.Value + c.Value + s.Value - 2;
        }

        /// <summary>N (munitions pour qu'une passe) → note /5 : ≤ 1,5 → 5 · ≤ 2,5 → 4 · ≤ 4,5 → 3 · ≤ 8 → 2 · ≤ 15 → 1 · sinon 0.</summary>
        public static int? NoteFromN(double? n)
        {
            if (n == null) return null;
            double v = n.Value;
            return v <= 1.5 ? 5 : v <= 2.5 ? 4 : v <= 4.5 ? 3 : v <= 8 ? 2 : v <= 15 ? 1 : 0;
        }

        public static bool Zigzag(string approach) => Contains(approach, "zigzag");
        public static bool SeaSkimming(string approach) => Contains(approach, "rase-flots");

        /// <summary>Plongeon quasi balistique : missiles très rapides (≥ 3 200 km/h : Tusko-B, Piledriver, Starfall).</summary>
        public static bool Dive(Inputs w) => !w.Bomb && w.SpeedMs != null && w.SpeedMs.Value * 3.6 >= 3200;

        private static bool Contains(string s, string what) =>
            !string.IsNullOrEmpty(s) && s.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0;

        // ------------------------------------------------------------------ Signatures
        /// <summary>Note de signature radar /5 (D13) : clamp(arrondi(5 × (log10(rcs) + 3) / 3), 0, 5).</summary>
        public static int? RcsNote(double? rcs)
        {
            if (rcs == null) return null;
            if (rcs.Value <= 0) return 0;
            double n = NoteScale.RoundHalfUp(5 * (Math.Log10(rcs.Value) + 3) / 3);
            return (int)Math.Min(5, Math.Max(0, n));
        }

        // ------------------------------------------------------------------ Type
        /// <summary>Type d'arme (vocabulaire de la colonne « Type » du classeur), déduit des drapeaux du jeu.</summary>
        public static string TypeOf(WeaponFlags f)
        {
            bool rocket = WeaponCategories.Primary(f) == WeaponCategory.Roquette;
            if (f.Nuclear && (f.Missile || f.HasMissileBody) && !f.Bomb) return "Missile nucléaire";
            if (f.Nuclear) return "Bombe nucléaire";
            if (f.Jammer) return "Brouilleur (nacelle)";
            if (f.Cargo || f.Troops || f.Sling || f.MountCargo || f.MountTroops) return "Cargaison / véhicule / élingue";
            if (f.Energy) return "Énergie / équipement";
            if (f.Gun) return "Canon / mitrailleuse / lance-grenade";
            if (f.GlideBomb) return "Bombe planante guidée";
            if (f.Bomb) return "Bombe";
            if (rocket) return f.LaserGuided ? "Roquette guidée laser" : "Roquette / obus guidé";
            if (f.Missile || f.HasMissileBody) return "Missile";
            return "Autre / équipement";
        }

        internal static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

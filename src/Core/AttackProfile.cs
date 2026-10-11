using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NuclearOptionWiki.Core
{
    /// <summary>Profil d'attaque en pastilles, à partir de la colonne « Méthode d'approche » (plan v1.1, tableau 11.3).</summary>
    public static class AttackProfile
    {
        public const int MaxRawLength = 14;

        private static readonly (Regex pattern, string label)[] Rules =
        {
            (new Regex(@"^loft", RegexOptions.IgnoreCase), "LOFT"),
            (new Regex(@"^évasion zigzag", RegexOptions.IgnoreCase), "ZIGZAG"),
            (new Regex(@"^datalink", RegexOptions.IgnoreCase), "DATALINK"),
            (new Regex(@"^phase terminale", RegexOptions.IgnoreCase), "TERMINALE"),
            (new Regex(@"^rase-flots/rase-sol\s+(\d+)\s*m", RegexOptions.IgnoreCase), "RASE-SOL {0} M"),
            (new Regex(@"^attaque par le haut", RegexOptions.IgnoreCase), "ATTAQUE HAUTE"),
            (new Regex(@"^évite le relief", RegexOptions.IgnoreCase), "SUIVI RELIEF"),
            (new Regex(@"^croisière", RegexOptions.IgnoreCase), "CROISIÈRE"),
            (new Regex(@"^accélération terminale", RegexOptions.IgnoreCase), "SPRINT FINAL"),
            (new Regex(@"^hit-to-kill", RegexOptions.IgnoreCase), "HIT-TO-KILL"),
            (new Regex(@"^angle d'attaque haut", RegexOptions.IgnoreCase), "HAUT ANGLE"),
        };

        /// <summary>Pastilles dans l'ordre de la colonne Excel ; liste vide si aucune approche.</summary>
        public static List<string> Chips(string approach)
        {
            var chips = new List<string>();
            if (string.IsNullOrWhiteSpace(approach)) return chips;
            foreach (var raw in approach.Split(';'))
            {
                string part = raw.Trim();
                if (part.Length == 0) continue;
                string chip = null;
                foreach (var (pattern, label) in Rules)
                {
                    var m = pattern.Match(part);
                    if (!m.Success) continue;
                    chip = m.Groups.Count > 1 ? string.Format(label, m.Groups[1].Value) : label;
                    break;
                }
                if (chip == null)
                {
                    chip = part.ToUpperInvariant();
                    if (chip.Length > MaxRawLength) chip = chip.Substring(0, MaxRawLength - 1).TrimEnd() + "…";
                }
                if (!chips.Contains(chip)) chips.Add(chip);
            }
            return chips;
        }
    }
}

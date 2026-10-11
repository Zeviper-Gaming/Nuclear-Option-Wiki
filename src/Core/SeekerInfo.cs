namespace NuclearOptionWiki.Core
{
    /// <summary>
    /// Autodirecteur, IR, tire et oublie, laser requis, déduits des données lues en jeu (plan v1.1, tableau 11.4).
    /// Valeurs null = N/A. Les valeurs de l'Excel (laser requis, tire et oublie) restent prioritaires (D29).
    /// </summary>
    public sealed class SeekerInfo
    {
        public string Display;       // « Infrarouge », « Radar actif »… ou null (N/A)
        public bool? Infrared;
        public bool? FireAndForget;
        public bool? LaserRequired;

        public static SeekerInfo Derive(LiveStats live)
        {
            var r = new SeekerInfo();
            string type = (live.SeekerType ?? "").Trim();
            string component = live.SeekerComponent ?? "";
            bool ballistic = component == "OpticalSeekerBomb" || component == "OpticalSeekerHighDrag";
            bool fafYes;

            switch (type)
            {
                case "IR": r.Display = "Infrarouge"; fafYes = true; break;
                case "ARH": r.Display = "Radar actif"; fafYes = true; break;
                case "SARH": r.Display = "Radar semi-actif"; fafYes = false; break;
                case "ARAD": r.Display = "Anti-radar"; fafYes = true; break;
                case "Laser": r.Display = "Laser"; fafYes = false; break;
                case "INS": r.Display = "Inertiel"; fafYes = true; break;
                case "INS / Opt.": r.Display = "Inertiel + optique"; fafYes = true; break;
                case "Optical":
                    r.Display = ballistic ? "Aucun (balistique)" : "Optique (opérateur)";
                    fafYes = false;
                    break;
                case "":
                    if (live.IsGun) r.Display = "Aucun (canon)";
                    else if (live.IsEnergy) r.Display = "Aucun (énergie)";
                    else return r; // inconnu : tout N/A
                    r.Infrared = false;
                    r.LaserRequired = live.LaserGuided == true;
                    return r;      // F&F : N/A (pas un missile)
                default:
                    r.Display = type; fafYes = false; break;
            }

            r.Infrared = type == "IR";
            r.LaserRequired = type == "Laser" || live.LaserGuided == true;
            // Q2 : le tire et oublie ne concerne que les missiles.
            bool isMissile = live.IsMissile || (!live.IsBomb && !ballistic);
            r.FireAndForget = isMissile ? fafYes : (bool?)null;
            return r;
        }
    }
}

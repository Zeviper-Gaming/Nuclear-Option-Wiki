using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuclearOptionWiki.Core
{
    /// <summary>Contenu de aircraft.json (schéma 1, plan avions 7.1). Généré par tools/avions_vers_json.py.</summary>
    public sealed class AircraftFile
    {
        [JsonProperty("schema")] public int Schema;
        [JsonProperty("genere_le")] public string GeneratedOn;
        [JsonProperty("source")] public string Source;
        [JsonProperty("seuils")] public AircraftThresholds Thresholds = new AircraftThresholds();
        [JsonProperty("appareils")] public Dictionary<string, AircraftRecord> Aircraft = new Dictionary<string, AircraftRecord>();
        [JsonProperty("emplacements_fr")] public Dictionary<string, Dictionary<string, string>> HardpointNames = new Dictionary<string, Dictionary<string, string>>();
        [JsonProperty("alias")] public Dictionary<string, string> Aliases = new Dictionary<string, string>();
    }

    public sealed class AircraftThresholds
    {
        [JsonProperty("encombrement_m2")] public LoadThresholds Footprint;
        [JsonProperty("charge_kg")] public LoadThresholds Load;
    }

    /// <summary>
    /// Un appareil. Toute valeur inconnue vaut null. Les valeurs listées dans <see cref="Corrected"/> sont déjà corrigées
    /// (feuille Corrections, A20) : la fiche leur ajoute « * ».
    /// </summary>
    public sealed class AircraftRecord
    {
        [JsonProperty("nom")] public string Name;
        [JsonProperty("code")] public string Code;
        [JsonProperty("source")] public string Source;
        [JsonProperty("type")] public string Type;
        [JsonProperty("taille")] public string Size;
        [JsonProperty("encombrement_m2")] public double? FootprintM2;
        [JsonProperty("longueur_m")] public double? LengthM;
        [JsonProperty("envergure_m")] public double? WingspanM;
        [JsonProperty("roles")] public List<string> Roles = new List<string>();
        [JsonProperty("tags")] public List<string> Tags = new List<string>();
        [JsonProperty("description_fr")] public string DescriptionFr;
        [JsonProperty("texte_perso")] public string PersonalText;
        [JsonProperty("rang")] public double? Rank;
        [JsonProperty("cout_m")] public double? CostMillions;
        [JsonProperty("vitesse_kmh")] public double? SpeedKmh;
        [JsonProperty("decrochage_kmh")] public double? StallKmh;
        [JsonProperty("maniabilite")] public double? Maneuverability;
        [JsonProperty("g_max")] public double? GLimit;
        [JsonProperty("masse_vide_kg")] public double? EmptyMassKg;
        [JsonProperty("masse_max_kg")] public double? MaxMassKg;
        [JsonProperty("charge_utile_kg")] public double? PayloadKg;
        [JsonProperty("carburant_kg")] public double? FuelKg;
        [JsonProperty("poussee_kn")] public double? ThrustKn;
        [JsonProperty("puissance_kw")] public double? PowerKw;
        [JsonProperty("decollage_vertical")] public bool? VerticalTakeoff;
        [JsonProperty("decollage_m")] public double? TakeoffM;
        [JsonProperty("decollage_ms")] public double? TakeoffMs;
        [JsonProperty("radar_m")] public double? RadarM;
        [JsonProperty("brouilleur")] public bool? Jammer;
        [JsonProperty("canons_fixes")] public double? FixedGuns;
        [JsonProperty("leurres")] public double? Flares;
        [JsonProperty("paillettes")] public double? Chaff;
        [JsonProperty("blindage")] public double? Armor;
        [JsonProperty("rcs_m2")] public double? RcsM2;
        [JsonProperty("laser_cibles")] public double? LaserTargets;
        [JsonProperty("note_vitesse")] public double? SpeedNote;
        [JsonProperty("note_charge")] public double? PayloadNote;
        [JsonProperty("note_cout")] public double? CostNote;
        [JsonProperty("note_rcs")] public int? RcsNote;
        [JsonProperty("corriges")] public List<string> Corrected = new List<string>();
        [JsonProperty("ir_force")] public IrOverride IrForced = new IrOverride();
        [JsonProperty("emports_forces")] public Dictionary<string, HardpointOverride> HardpointsForced = new Dictionary<string, HardpointOverride>();

        public bool IsCorrected(string field) => Corrected != null && Corrected.Contains(field);
    }

    public sealed class IrOverride
    {
        [JsonProperty("ir_30")] public double? Ir30;
        [JsonProperty("ir_max")] public double? IrMax;
    }

    /// <summary>Correction d'un emplacement lu en jeu (null = pas de correction pour ce sous-champ).</summary>
    public sealed class HardpointOverride
    {
        [JsonProperty("points")] public int? Points;
        [JsonProperty("soute")] public bool? Bay;
        [JsonProperty("charge_kg")] public double? ChargeKg;
        [JsonProperty("categories")] public List<string> Categories;
        [JsonProperty("nb_armes")] public int? WeaponCount;
        [JsonProperty("masquer")] public bool Hide;
    }
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuclearOptionWiki.Core
{
    /// <summary>Contenu de weapons.json (schéma 1). Généré par tools/xlsx_vers_json.py.</summary>
    public sealed class WeaponFile
    {
        [JsonProperty("schema")] public int Schema;
        [JsonProperty("source")] public string Source;
        [JsonProperty("source_sha256")] public string SourceSha256;
        [JsonProperty("armes")] public Dictionary<string, WeaponRecord> Weapons = new Dictionary<string, WeaponRecord>();
        [JsonProperty("alias")] public Dictionary<string, string> Aliases = new Dictionary<string, string>();
    }

    /// <summary>Une arme. Toute valeur inconnue vaut null.</summary>
    public sealed class WeaponRecord
    {
        [JsonProperty("excel")] public string ExcelKey;
        [JsonProperty("cle_provisoire")] public bool ProvisionalKey;
        [JsonProperty("nom")] public string Name;
        [JsonProperty("source")] public string Source;
        [JsonProperty("type")] public string Type;
        [JsonProperty("guidage")] public string Guidance;
        [JsonProperty("approche")] public string Approach;
        [JsonProperty("penetration")] public double? Penetration;
        [JsonProperty("explosif_kg")] public double? ExplosiveKg;
        [JsonProperty("cout_m")] public double? CostMillions;
        [JsonProperty("explosif_par_m")] public double? ExplosivePerMillion;
        [JsonProperty("portee_min_m")] public double? MinRangeM;
        [JsonProperty("portee_max_m")] public double? MaxRangeM;
        [JsonProperty("vitesse_ms")] public double? SpeedMs;
        [JsonProperty("note_vitesse")] public double? SpeedNote;
        [JsonProperty("note_portee_log")] public double? RangeNoteLog;
        [JsonProperty("note_portee_rang")] public double? RangeNoteRank;
        [JsonProperty("laser_requis")] public bool? LaserRequired;
        [JsonProperty("tire_et_oublie")] public bool? FireAndForget;
        [JsonProperty("air_sol")] public AirToGround AirToGround = new AirToGround();
        [JsonProperty("air_air")] public AirToAir AirToAir = new AirToAir();
        [JsonProperty("defense")] public Defense Defense = new Defense();
    }

    public sealed class AirToGround
    {
        [JsonProperty("vehicules_legers")] public int? LightVehicles;
        [JsonProperty("chars")] public int? Tanks;
        [JsonProperty("batiments_legers")] public int? LightBuildings;
        [JsonProperty("gros_batiments")] public int? LargeBuildings;
        [JsonProperty("bunker")] public int? Bunker;
        [JsonProperty("navire_leger")] public int? LightShip;
        [JsonProperty("navire_moyen")] public int? MediumShip;
        [JsonProperty("porte_avions")] public int? Carrier;
    }

    public sealed class AirToAir
    {
        [JsonProperty("petit_avion")] public int? SmallAircraft;
        [JsonProperty("moyen_avion")] public int? MediumAircraft;
        [JsonProperty("gros_avion")] public int? LargeAircraft;
    }

    public sealed class Defense
    {
        [JsonProperty("agilite")] public int? Agility;
        [JsonProperty("rcs_m2")] public double? RcsM2;
        [JsonProperty("note_rcs")] public int? RcsNote;
        [JsonProperty("note_ir")] public int? IrNote;
        [JsonProperty("resistance_laser")] public int? LaserResistance;
        [JsonProperty("difficulte_toucher")] public int? HitDifficulty;
    }
}

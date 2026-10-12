using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuclearOptionWiki.Core
{
    /// <summary>
    /// Base des appareils chargée depuis aircraft.json. Recherche (plan A5) :
    /// jsonKey → unitName (alias, unique dans le JSON) → non référencé.
    /// </summary>
    public sealed class AircraftDatabase
    {
        public const int SupportedSchema = 1;

        private readonly AircraftFile _file;
        private readonly Dictionary<string, string> _aliases;

        public int Count => _file.Aircraft.Count;
        public AircraftThresholds Thresholds => _file.Thresholds ?? new AircraftThresholds();
        public IEnumerable<KeyValuePair<string, AircraftRecord>> All => _file.Aircraft;

        private AircraftDatabase(AircraftFile file)
        {
            _file = file;
            _file.Aircraft = new Dictionary<string, AircraftRecord>(file.Aircraft ?? new Dictionary<string, AircraftRecord>(), StringComparer.Ordinal);
            _file.HardpointNames = file.HardpointNames ?? new Dictionary<string, Dictionary<string, string>>();
            _aliases = new Dictionary<string, string>(file.Aliases ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Charge le contenu JSON. Lève <see cref="FormatException"/> si le schéma n'est pas pris en charge.</summary>
        public static AircraftDatabase FromJson(string json)
        {
            var file = JsonConvert.DeserializeObject<AircraftFile>(json) ?? throw new FormatException("aircraft.json vide.");
            if (file.Schema != SupportedSchema)
                throw new FormatException($"aircraft.json : schéma {file.Schema} non pris en charge (attendu {SupportedSchema}).");
            return new AircraftDatabase(file);
        }

        /// <summary>A6 : OVNI et contenu événementiel jamais référencés.</summary>
        public static bool IsExcluded(string jsonKey, bool isEventContent) => isEventContent || jsonKey == "UFO";

        /// <summary>Renvoie (clé, fiche) ou (null, null) si l'appareil n'est pas référencé.</summary>
        public KeyValuePair<string, AircraftRecord> Find(string jsonKey, string unitName)
        {
            if (!string.IsNullOrEmpty(jsonKey) && _file.Aircraft.TryGetValue(jsonKey, out var byKey))
                return new KeyValuePair<string, AircraftRecord>(jsonKey, byKey);
            if (!string.IsNullOrWhiteSpace(unitName) && _aliases.TryGetValue(unitName.Trim(), out var key) && _file.Aircraft.TryGetValue(key, out var byAlias))
                return new KeyValuePair<string, AircraftRecord>(key, byAlias);
            return new KeyValuePair<string, AircraftRecord>(null, null);
        }

        /// <summary>Nom français d'un emplacement, ou null (le nom anglais du jeu est alors affiché).</summary>
        public string HardpointName(string aircraftKey, string gameName)
        {
            if (aircraftKey == null || gameName == null) return null;
            return _file.HardpointNames.TryGetValue(aircraftKey, out var names) && names != null && names.TryGetValue(gameName, out var fr)
                   && !string.IsNullOrWhiteSpace(fr) ? fr.Trim() : null;
        }
    }
}

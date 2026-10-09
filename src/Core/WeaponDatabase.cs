using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuclearOptionWiki.Core
{
    /// <summary>
    /// Base des armes chargée depuis weapons.json. Recherche (décision D18) :
    /// nom d'asset WeaponInfo → alias (jsonKey de l'emport) → weaponName s'il est unique → non trouvée.
    /// </summary>
    public sealed class WeaponDatabase
    {
        public const int SupportedSchema = 1;

        private readonly Dictionary<string, WeaponRecord> _byKey;
        private readonly Dictionary<string, string> _aliases;
        private readonly Dictionary<string, WeaponRecord> _byUniqueName;

        public int Count => _byKey.Count;
        public IEnumerable<KeyValuePair<string, WeaponRecord>> All => _byKey;

        private WeaponDatabase(WeaponFile file)
        {
            _byKey = new Dictionary<string, WeaponRecord>(file.Weapons ?? new Dictionary<string, WeaponRecord>(), StringComparer.Ordinal);
            _aliases = new Dictionary<string, string>(file.Aliases ?? new Dictionary<string, string>(), StringComparer.Ordinal);

            _byUniqueName = new Dictionary<string, WeaponRecord>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in _byKey.Values)
            {
                if (string.IsNullOrWhiteSpace(record.Name)) continue;
                string name = record.Name.Trim();
                if (duplicates.Contains(name)) continue;
                if (_byUniqueName.ContainsKey(name))
                {
                    _byUniqueName.Remove(name);
                    duplicates.Add(name);
                }
                else
                {
                    _byUniqueName[name] = record;
                }
            }
        }

        /// <summary>Charge le contenu JSON. Lève <see cref="FormatException"/> si le schéma n'est pas pris en charge.</summary>
        public static WeaponDatabase FromJson(string json)
        {
            var file = JsonConvert.DeserializeObject<WeaponFile>(json) ?? throw new FormatException("weapons.json vide.");
            if (file.Schema != SupportedSchema)
                throw new FormatException($"weapons.json : schéma {file.Schema} non pris en charge (attendu {SupportedSchema}).");
            return new WeaponDatabase(file);
        }

        /// <summary>Cherche une arme. Renvoie null si elle n'est pas référencée.</summary>
        public WeaponRecord Find(string infoName, string mountJsonKey, string weaponName)
        {
            if (!string.IsNullOrEmpty(infoName) && _byKey.TryGetValue(infoName, out var byInfo))
                return byInfo;
            if (!string.IsNullOrEmpty(mountJsonKey) && _aliases.TryGetValue(mountJsonKey, out var key) && _byKey.TryGetValue(key, out var byAlias))
                return byAlias;
            if (!string.IsNullOrWhiteSpace(weaponName) && _byUniqueName.TryGetValue(weaponName.Trim(), out var byName))
                return byName;
            return null;
        }
    }
}

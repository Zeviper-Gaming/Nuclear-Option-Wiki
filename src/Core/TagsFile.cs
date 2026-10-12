using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuclearOptionWiki.Core
{
    /// <summary>
    /// Tags saisis en jeu (plan avions A24) : BepInEx/config/NuclearOptionWiki_tags.json, { "jsonKey": ["tag", …] }.
    /// Prioritaires sur la colonne Tags du classeur ; recopiés dans le classeur par mettre_a_jour.py --jeu.
    /// </summary>
    public sealed class TagsFile
    {
        public const string FileName = "NuclearOptionWiki_tags.json";

        private readonly SortedDictionary<string, List<string>> _tags = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);

        public int Count => _tags.Count;

        /// <summary>Saisie du champ F1 : mots-clés séparés par « ; », espaces et vides retirés.</summary>
        public static List<string> Parse(string input)
        {
            var res = new List<string>();
            foreach (var part in (input ?? "").Split(';'))
            {
                string t = part.Trim();
                if (t.Length > 0 && !res.Contains(t)) res.Add(t);
            }
            return res;
        }

        public static string Join(IEnumerable<string> tags) => tags == null ? "" : string.Join("; ", tags);

        public static TagsFile FromJson(string json)
        {
            var f = new TagsFile();
            if (string.IsNullOrWhiteSpace(json)) return f;
            var d = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            if (d == null) return f;
            foreach (var kv in d)
                if (!string.IsNullOrEmpty(kv.Key) && kv.Value != null && kv.Value.Count > 0) f._tags[kv.Key] = kv.Value;
            return f;
        }

        public string ToJson() => JsonConvert.SerializeObject(_tags, Formatting.Indented);

        /// <summary>Tags saisis en jeu pour cet appareil, ou null (les tags du classeur s'appliquent).</summary>
        public List<string> Get(string jsonKey) =>
            jsonKey != null && _tags.TryGetValue(jsonKey, out var t) ? new List<string>(t) : null;

        /// <summary>Enregistre la saisie ; une saisie vide supprime l'entrée (retour aux tags du classeur).</summary>
        public void Set(string jsonKey, string input)
        {
            if (string.IsNullOrEmpty(jsonKey)) return;
            var tags = Parse(input);
            if (tags.Count == 0) _tags.Remove(jsonKey);
            else _tags[jsonKey] = tags;
        }
    }
}

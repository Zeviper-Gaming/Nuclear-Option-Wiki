using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace NuclearOptionWiki.Diagnostics
{
    /// <summary>
    /// Exporte la liste des armes chargées en jeu (emports + armes), pour établir la correspondance Excel ↔ jeu.
    /// Le fichier cumule ce qui a été vu pendant la session : rouvrir le hangar avec d'autres avions complète la liste.
    /// </summary>
    internal static class WeaponDump
    {
        public const string FileName = "NuclearOptionWiki_armes.csv";
        private const string Header = "info_name;weapon_name;short_name;mount_jsonkey;mount_name;missile;bomb;gun;energy;cargo;troops;dans_encyclopedie;" +
                                      "autodirecteur;guidage_laser;vitesse_initiale;vitesse_max;penetration;explosif;cout_par_coup;masse_par_coup;portee_max;cadence_s";

        private static Type _missileType;
        private static System.Reflection.MethodInfo _getPierce, _getYield;
        private static bool _missileResolved;

        private static readonly SortedDictionary<string, string> Rows = new SortedDictionary<string, string>(StringComparer.Ordinal);

        public static void Export()
        {
            try
            {
                var inEncyclopedia = new HashSet<WeaponMount>();
                try
                {
                    var encyclopedia = Encyclopedia.i;
                    if (encyclopedia != null && encyclopedia.weaponMounts != null)
                        inEncyclopedia.UnionWith(encyclopedia.weaponMounts.Where(m => m != null));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"[Wiki] Encyclopédie illisible pendant l'export : {e.Message}");
                }

                var infosWithMount = new HashSet<WeaponInfo>();
                foreach (var mount in Resources.FindObjectsOfTypeAll<WeaponMount>())
                {
                    if (mount == null) continue;
                    var info = mount.info;
                    if (info != null) infosWithMount.Add(info);
                    Add(info, mount, inEncyclopedia.Contains(mount));
                }
                foreach (var info in Resources.FindObjectsOfTypeAll<WeaponInfo>())
                {
                    if (info != null && !infosWithMount.Contains(info))
                        Add(info, null, false);
                }

                var path = Path.Combine(Paths.ConfigPath, FileName);
                var sb = new StringBuilder();
                sb.Append(Header).Append('\n');
                foreach (var row in Rows.Values) sb.Append(row).Append('\n');
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                Plugin.Log.LogInfo($"[Wiki] Liste des armes exportée ({Rows.Count} lignes) : {path}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Wiki] Échec de l'export des armes : {e}");
            }
        }

        private static void Add(WeaponInfo info, WeaponMount mount, bool inEncyclopedia)
        {
            string infoName = info != null ? info.name : "";
            string mountKey = mount != null ? mount.jsonKey : "";
            string key = infoName + "\u0001" + mountKey + "\u0001" + (mount != null ? mount.name : "");
            Rows[key] = string.Join(";", new[]
            {
                Clean(infoName),
                Clean(info?.weaponName),
                Clean(info?.shortName),
                Clean(mountKey),
                Clean(mount?.mountName),
                Flag(info != null && info.missile),
                Flag(info != null && info.bomb),
                Flag(info != null && info.gun),
                Flag(info != null && info.energy),
                Flag(info != null && info.cargo),
                Flag(info != null && info.troops),
                Flag(inEncyclopedia),
                Stats(info),
            });
        }

        /// <summary>Stats de l'arme, pour départager les variantes de même nom (ex. « 57mm Cannon (variante n) »).</summary>
        private static string Stats(WeaponInfo info)
        {
            if (info == null) return ";;;;;;;;;";
            string seeker = "", pierce = Num(info.pierceDamage), blast = Num(info.blastDamage), maxSpeed = "";
            try
            {
                var prefab = info.weaponPrefab;
                if (prefab != null)
                {
                    var s = prefab.GetComponent<MissileSeeker>();
                    if (s != null) seeker = s.GetSeekerType();
                    ResolveMissile();
                    var missile = _missileType != null ? prefab.GetComponent(_missileType) : null;
                    if (missile != null)
                    {
                        if (_getPierce != null) pierce = Num(Convert.ToSingle(_getPierce.Invoke(missile, null)));
                        if (_getYield != null) blast = Num(Convert.ToSingle(_getYield.Invoke(missile, null)));
                    }
                }
                maxSpeed = Num(info.GetMaxSpeed());
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"[Wiki] Stats incomplètes pour {info.name} : {e.GetBaseException().Message}");
            }
            return string.Join(";", new[]
            {
                Clean(seeker), Flag(info.laserGuided), Num(info.muzzleVelocity), maxSpeed, pierce, blast,
                Num(info.costPerRound), Num(info.massPerRound), Num(info.targetRequirements.maxRange), Num(info.fireInterval),
            });
        }

        private static void ResolveMissile()
        {
            if (_missileResolved) return;
            _missileResolved = true;
            _missileType = HarmonyLib.AccessTools.TypeByName("Missile");
            if (_missileType == null) return;
            _getPierce = HarmonyLib.AccessTools.Method(_missileType, "GetPierce");
            _getYield = HarmonyLib.AccessTools.Method(_missileType, "GetYield");
        }

        private static string Num(float v) =>
            float.IsNaN(v) || float.IsInfinity(v) ? "" : v.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

        private static string Clean(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace(';', ',').Replace('\n', ' ').Replace('\r', ' ').Trim();

        private static string Flag(bool b) => b ? "1" : "0";
    }
}

using System;
using BepInEx.Configuration;
using UnityEngine;

namespace NuclearOptionWiki
{
    public enum WikiTheme
    {
        MfdPhosphore,
        Annonciateur,
        HangarNO
    }

    public enum RangeScale
    {
        Logarithmique,
        Rang
    }

    /// <summary>Entrées de configuration, visibles et modifiables dans le menu F1 (Configuration Manager).</summary>
    public class WikiConfig
    {
        public static readonly KeyboardShortcut DefaultToggleKey = new KeyboardShortcut(KeyCode.F6);

        public ConfigEntry<KeyboardShortcut> ToggleKey { get; }
        public ConfigEntry<bool> ActiveOnOpen { get; }
        public ConfigEntry<WikiTheme> Theme { get; }
        public ConfigEntry<RangeScale> RangeNote { get; }
        public ConfigEntry<float> Scale { get; }
        public ConfigEntry<float> BackgroundOpacity { get; }
        public ConfigEntry<bool> ExportWeapons { get; }
        public ConfigEntry<bool> ExportAircraft { get; }
        public ConfigEntry<string> AircraftTags { get; }
        public ConfigEntry<bool> DemoSheet { get; }
        public ConfigEntry<bool> VerboseLog { get; }

        /// <param name="exportButton">Fabrique de l'attribut du Configuration Manager qui dessine un bouton d'export
        /// (« armes » ou « appareils ») ; null = cases à cocher simples (tests hors jeu).</param>
        /// <param name="tagsEditor">Attribut du Configuration Manager qui dessine l'éditeur de tags (A24) ; null hors jeu.</param>
        public WikiConfig(ConfigFile config, Func<string, object> exportButton = null, Func<object> tagsEditor = null)
        {
            ToggleKey = config.Bind("Général", "Touche bascule", DefaultToggleKey,
                "Touche qui affiche ou masque la fiche wiki dans l'écran de loadout (hangar). " +
                "Clique sur le bouton puis appuie sur la touche voulue ; Ctrl, Shift et Alt sont acceptés. " +
                "Échap est refusée (elle ferme le hangar).");
            // Les noms de section et de clé ne doivent contenir aucun de ces caractères : = \n \t \ " ' [ ]
            // (BepInEx lève une exception sinon ; vérifié par tests/Core.Tests et tests/Config.Smoke).
            ActiveOnOpen = config.Bind("Général", "Affichage automatique au hangar", false,
                "Affiche la fiche dès l'ouverture du hangar, sans appuyer sur la touche.");

            Theme = config.Bind("Affichage", "Thème", WikiTheme.MfdPhosphore,
                "Style de la fiche : MfdPhosphore (voyants MFD verts), Annonciateur (tuiles allumées), HangarNO (bleu du hangar).");
            RangeNote = config.Bind("Affichage", "Note de portée", RangeScale.Logarithmique,
                "Calcul de la note de portée /10 : Logarithmique (100 m = 1, 1 000 km = 10) ou Rang (comme la note de vitesse).");
            Scale = config.Bind("Affichage", "Échelle", 1.0f,
                new ConfigDescription("Taille de la fiche (1 = 600 × 840 px en 1080p).", new AcceptableValueRange<float>(0.75f, 1.5f)));
            BackgroundOpacity = config.Bind("Affichage", "Opacité du fond", 0.9f,
                new ConfigDescription("Opacité du fond de la fiche.", new AcceptableValueRange<float>(0.5f, 1f)));

            // Boutons d'export (menu F1) : la valeur booléenne n'est pas utilisée, le Configuration Manager dessine un bouton à sa place.
            ExportWeapons = config.Bind("Diagnostic", "Exporter la liste des armes", false,
                new ConfigDescription("Bouton : écrit tout de suite la liste des armes chargées dans " +
                                      "BepInEx/config/NuclearOptionWiki_armes_v<version>.csv. À utiliser dans le hangar.",
                    null, Tags(exportButton, "armes")));
            ExportAircraft = config.Bind("Diagnostic", "Exporter la liste des appareils", false,
                new ConfigDescription("Bouton : écrit tout de suite les appareils chargés et leurs emports dans " +
                                      "BepInEx/config/NuclearOptionWiki_avions_v<version>.csv et NuclearOptionWiki_emports_v<version>.csv. " +
                                      "À utiliser dans le hangar, tous les mods d'avions chargés.",
                    null, Tags(exportButton, "appareils")));
            // A24 : la valeur n'est pas utilisée ; le Configuration Manager dessine le champ des tags de l'appareil affiché.
            object tagsTag = tagsEditor?.Invoke();
            AircraftTags = config.Bind("Tags", "Tags (appareil sélectionné)", "",
                new ConfigDescription("Mots-clés de l'appareil sélectionné dans le hangar (pas celui survolé), séparés par « ; ». « Enregistrer » met la fiche à jour ; " +
                                      "champ vide = retour aux tags du classeur. Enregistrés dans BepInEx/config/NuclearOptionWiki_tags.json.",
                    null, tagsTag == null ? new object[0] : new[] { tagsTag }));
            DemoSheet = config.Bind("Diagnostic", "Fiche de démonstration", false,
                "Affiche la fiche du CI-22 (ou de l'AGM-48 sans données d'appareils) dès l'ouverture du hangar (contrôle visuel).");
            VerboseLog = config.Bind("Diagnostic", "Journal détaillé", false,
                "Écrit plus de détails dans le journal BepInEx.");

            // Échap ferme le hangar : refusée et remplacée par la touche par défaut.
            ToggleKey.SettingChanged += (_, __) => RejectForbiddenKey();
            RejectForbiddenKey();
        }

        private static object[] Tags(Func<string, object> factory, string kind)
        {
            object tag = factory?.Invoke(kind);
            return tag == null ? new object[0] : new[] { tag };
        }

        private void RejectForbiddenKey()
        {
            if (ToggleKey.Value.MainKey == KeyCode.Escape)
            {
                Plugin.Log?.LogWarning("[Wiki] Échap ne peut pas servir de touche bascule (elle ferme le hangar) : retour à F6.");
                ToggleKey.Value = DefaultToggleKey;
            }
        }
    }
}

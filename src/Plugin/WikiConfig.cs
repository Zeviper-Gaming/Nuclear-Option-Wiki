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
        public ConfigEntry<bool> DemoSheet { get; }
        public ConfigEntry<bool> VerboseLog { get; }

        public WikiConfig(ConfigFile config)
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

            ExportWeapons = config.Bind("Diagnostic", "Exporter la liste des armes", false,
                "À l'ouverture du hangar, écrit la liste des armes chargées dans BepInEx/config/NuclearOptionWiki_armes.csv.");
            DemoSheet = config.Bind("Diagnostic", "Fiche de démonstration", false,
                "Affiche la fiche de l'AGM-48 dès l'ouverture du hangar (contrôle visuel).");
            VerboseLog = config.Bind("Diagnostic", "Journal détaillé", false,
                "Écrit plus de détails dans le journal BepInEx.");

            // Échap ferme le hangar : refusée et remplacée par la touche par défaut.
            ToggleKey.SettingChanged += (_, __) => RejectForbiddenKey();
            RejectForbiddenKey();
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

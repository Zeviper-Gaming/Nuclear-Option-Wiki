using System;

namespace NuclearOptionWiki.Core
{
    /// <summary>Échelle de couleur des notes, commune aux trois thèmes (plan, section 7.2).</summary>
    public static class NoteScale
    {
        /// <summary>Couleurs des notes 0 à 5 (rouge → vert).</summary>
        public static readonly string[] NoteHex = { "#FF3B3B", "#FF6A2B", "#FFA31F", "#FFD523", "#A9FF2F", "#3DFF57" };

        /// <summary>Couleur « N/A ».</summary>
        public const string NotAvailableHex = "#5D6670";

        /// <summary>Arrondi au demi supérieur, identique au script Python (pas d'arrondi bancaire).</summary>
        public static double RoundHalfUp(double value, int decimals = 0) =>
            Math.Round(value, decimals, MidpointRounding.AwayFromZero);

        /// <summary>Indice de couleur (0..5) d'une note /5, ou null si N/A. Inversé : 0 devient 5 (signatures).</summary>
        public static int? ColorIndex5(int? note, bool inverted = false)
        {
            if (note == null) return null;
            int n = Clamp(note.Value, 0, 5);
            return inverted ? 5 - n : n;
        }

        /// <summary>Indice de couleur (0..5) d'une note /10 : arrondi(note / 2).</summary>
        public static int? ColorIndex10(double? note)
        {
            if (note == null) return null;
            return Clamp((int)RoundHalfUp(note.Value / 2.0), 0, 5);
        }

        /// <summary>Nombre de segments allumés d'une barre à 10 niveaux : arrondi(note) borné à [1 ; 10], 0 si N/A.</summary>
        public static int LitSegments(double? note)
        {
            if (note == null) return 0;
            return Clamp((int)RoundHalfUp(note.Value), 1, 10);
        }

        public static string Hex(int? colorIndex) => colorIndex == null ? NotAvailableHex : NoteHex[colorIndex.Value];

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}

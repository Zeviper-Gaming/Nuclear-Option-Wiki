using System;
using System.Globalization;

namespace NuclearOptionWiki.Core
{
    /// <summary>
    /// Formatage français des valeurs de la fiche. Séparateur de milliers : espace simple
    /// (les polices du jeu n'ont pas forcément l'espace fine insécable).
    /// </summary>
    public static class Format
    {
        public const string NotAvailable = "N/A";
        public const string Unknown = "—";

        private static readonly NumberFormatInfo Fr = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = " ",
            NumberGroupSizes = new[] { 3 },
            NegativeSign = "-",
        };

        /// <summary>Nombre avec au plus <paramref name="maxDecimals"/> décimales, zéros finaux retirés : 1 550 ; 0,09 ; 7,5.</summary>
        public static string Number(double value, int maxDecimals = 0)
        {
            double rounded = NoteScale.RoundHalfUp(value, maxDecimals);
            string pattern = maxDecimals > 0 ? "#,0." + new string('#', maxDecimals) : "#,0";
            return rounded.ToString(pattern, Fr);
        }

        /// <summary>Nombre avec exactement une décimale : 5,5 ; 6,0.</summary>
        public static string OneDecimal(double value) => NoteScale.RoundHalfUp(value, 1).ToString("0.0", Fr);

        public static string Distance(double? meters)
        {
            if (meters == null) return NotAvailable;
            double m = meters.Value;
            if (m < 1000) return Number(m) + " m";
            double km = m / 1000.0;
            return km < 10 ? Number(km, 1) + " km" : Number(km) + " km";
        }

        public static string Kmh(double kmh) => Number(kmh) + " km/h";
        public static string Kg(double kg) => Number(kg) + " kg";
        public static string Speed(double? ms) => ms == null ? NotAvailable : Number(ms.Value) + " m/s";
        public static string Cost(double? millions) => millions == null ? NotAvailable : Number(millions.Value, 2) + " M$";
        public static string Kilograms(double? kg) => kg == null ? NotAvailable : Number(kg.Value, 1) + " kg";
        public static string Plain(double? v) => v == null ? NotAvailable : Number(v.Value);
        public static string KilogramsPerMillion(double? v) => v == null ? NotAvailable : Number(v.Value) + " kg/M$";
        public static string SquareMeters(double? m2) => m2 == null ? Unknown : Number(m2.Value, 3) + " m²";
        public static string YesNo(bool? b) => b == null ? NotAvailable : b.Value ? "OUI" : "NON";

        /// <summary>Intensité IR lue en jeu (sans unité).</summary>
        public static string Infrared(double? intensity) => intensity == null ? Unknown : "IR " + Number(intensity.Value, 2);

        /// <summary>Agilité lue en jeu : « 35 °/s · 20 G », ou une seule des deux valeurs.</summary>
        public static string Agility(double? turnRateDegS, double? gLimit)
        {
            bool hasTurn = turnRateDegS != null && turnRateDegS.Value > 0;
            bool hasG = gLimit != null && gLimit.Value > 0;
            if (hasTurn && hasG) return Number(turnRateDegS.Value) + " °/s · " + Number(gLimit.Value) + " G";
            if (hasTurn) return Number(turnRateDegS.Value) + " °/s";
            if (hasG) return Number(gLimit.Value) + " G";
            return Unknown;
        }

        public static string OrNotAvailable(string s) => string.IsNullOrWhiteSpace(s) ? NotAvailable : s.Trim();
    }
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuclearOptionWiki.Core
{
    /// <summary>Bornes d'une échelle S/M/H en tiers log (plan A7 : encombrement en m² ; A9 : charge en kg).</summary>
    public sealed class LoadThresholds
    {
        [JsonProperty("min")] public double Min;
        [JsonProperty("max")] public double Max;
        [JsonProperty("s_max")] public double SMax;
        [JsonProperty("m_max")] public double MMax;
    }

    /// <summary>Classes S / M / H et notes IR, calculées comme le font les scripts Python.</summary>
    public static class LoadClass
    {
        /// <summary>« S », « M » ou « H » ; null si la valeur ou les seuils manquent.</summary>
        public static string Classify(double? value, LoadThresholds t)
        {
            if (value == null || value.Value <= 0 || t == null) return null;
            if (value.Value <= t.SMax) return "S";
            if (value.Value <= t.MMax) return "M";
            return "H";
        }

        /// <summary>Tiers égaux en échelle log entre la plus petite et la plus grande valeur (> 0).</summary>
        public static LoadThresholds FromValues(IEnumerable<double?> values)
        {
            double min = double.MaxValue, max = double.MinValue;
            foreach (var v in values)
            {
                if (v == null || v.Value <= 0) continue;
                min = Math.Min(min, v.Value);
                max = Math.Max(max, v.Value);
            }
            if (min == double.MaxValue) return null;
            double lmin = Math.Log10(min), lmax = Math.Log10(max);
            return new LoadThresholds
            {
                Min = min,
                Max = max,
                SMax = Math.Pow(10, lmin + (lmax - lmin) / 3),
                MMax = Math.Pow(10, lmin + 2 * (lmax - lmin) / 3),
            };
        }

        /// <summary>A12 : note RCS /5 = clamp(arrondi(log10(rcs) + 4), 0, 5) (10⁻⁴ m² → 0 ; 10 m² → 5).</summary>
        public static int? RcsNote(double? rcsM2)
        {
            if (rcsM2 == null || rcsM2.Value <= 0) return null;
            double n = NoteScale.RoundHalfUp(Math.Log10(rcsM2.Value) + 4);
            return (int)Math.Max(0, Math.Min(5, n));
        }

        /// <summary>
        /// A11 (amendé v3.3) : même échelle linéaire pour l'IR à 30 % et à plein gaz, de 0 à l'IR plein gaz le plus fort
        /// de tous les appareils : clamp(arrondi(5 × IR / IR max), 0, 5).
        /// </summary>
        public static int? IrNote(double? ir, double? globalMax)
        {
            if (ir == null || globalMax == null || globalMax.Value <= 0) return null;
            double n = NoteScale.RoundHalfUp(5 * ir.Value / globalMax.Value);
            return (int)Math.Max(0, Math.Min(5, n));
        }

        /// <summary>A11 : IR à ≈ 30 % de gaz = IR ralenti + 0,3 × (IR plein gaz sans postcombustion − IR ralenti).</summary>
        public static double? Ir30(double? irMin, double? irMaxDry)
        {
            if (irMin == null || irMaxDry == null) return null;
            return irMin.Value + 0.3 * (irMaxDry.Value - irMin.Value);
        }
    }
}

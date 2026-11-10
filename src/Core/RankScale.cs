using System;
using System.Collections.Generic;

namespace NuclearOptionWiki.Core
{
    /// <summary>
    /// Note /10 au rang centile d'une valeur parmi celles des données (wiki d'armes D24, wiki avions A14) :
    /// 1 + 9 × (nb ≤ v − 1) / (n − 1), une décimale, ex æquo en haut du groupe.
    /// Sert aux éléments absents des données (mods ajoutés) : la valeur lue en jeu est classée parmi celles du JSON,
    /// comme si elle faisait partie de la liste, d'où une note cohérente avec celles calculées par les scripts.
    /// </summary>
    public sealed class RankScale
    {
        private readonly double[] _sorted;
        private readonly bool _positiveOnly;

        public int Count => _sorted.Length;

        /// <param name="positiveOnly">Vrai : seules les valeurs > 0 sont classées, et 0 donne la note 0 (plan v1.1, Q3).</param>
        public RankScale(IEnumerable<double?> values, bool positiveOnly)
        {
            _positiveOnly = positiveOnly;
            var list = new List<double>();
            foreach (var v in values)
                if (v != null && !double.IsNaN(v.Value) && (!positiveOnly || v.Value > 0)) list.Add(v.Value);
            list.Sort();
            _sorted = list.ToArray();
        }

        /// <summary>Note d'une valeur qui fait déjà partie de la liste (calcul identique aux scripts Python).</summary>
        public double? NoteOfMember(double? v) => Note(v, member: true);

        /// <summary>Note d'une valeur nouvelle (lue en jeu) : elle est insérée dans la liste avant le classement.</summary>
        public double? NoteOfNew(double? v) => Note(v, member: false);

        private double? Note(double? v, bool member)
        {
            if (v == null || double.IsNaN(v.Value)) return null;
            if (_positiveOnly && v.Value <= 0) return 0;
            int le = 0;
            foreach (var x in _sorted)
            {
                if (x <= v.Value + 1e-9) le++;
                else break;
            }
            int n = _sorted.Length;
            if (!member)
            {
                le++;
                n++;
            }
            if (n <= 0) return null;
            if (n == 1) return 10;
            return NoteScale.RoundHalfUp(1 + 9.0 * Math.Max(le - 1, 0) / (n - 1), 1);
        }
    }
}

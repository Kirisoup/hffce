using System;
using System.Collections.Generic;
// using System.Linq;

public static class LinqExtensions
{
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> src, Func<TSource, TKey> sel)
        {
                if (src == null) throw new ArgumentNullException(nameof(src));
                if (sel == null) throw new ArgumentNullException(nameof(sel));

                using var enm = src.GetEnumerator();

                if (!enm.MoveNext()) throw new InvalidOperationException("Sequence contains no elements");

                var min = enm.Current;

                if (!enm.MoveNext()) return min;

                var minval = sel(min);

                while (enm.MoveNext())
                {
                        var candi = enm.Current;
                        var candival = sel(candi);

                        if (Comparer<TKey>.Default.Compare(candival, minval) >= 0) continue;

                        min = candi;
                        minval = candival;
                }

                return min;
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> src, Func<TSource, TKey> sel)
        {
                if (src == null) throw new ArgumentNullException(nameof(src));
                if (sel == null) throw new ArgumentNullException(nameof(sel));

                using var enm = src.GetEnumerator();

                if (!enm.MoveNext()) throw new InvalidOperationException("Sequence contains no elements");

                var min = enm.Current;

                if (!enm.MoveNext()) return min;

                var minval = sel(min);

                while (enm.MoveNext())
                {
                        var candi = enm.Current;
                        var candival = sel(candi);

                        if (Comparer<TKey>.Default.Compare(candival, minval) <= 0) continue;

                        min = candi;
                        minval = candival;
                }

                return min;
        }
}
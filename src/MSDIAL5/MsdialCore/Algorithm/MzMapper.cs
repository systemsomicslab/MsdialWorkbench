using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class MzMapper
    {
        /// <summary>
        /// Maps m/z values by grouping molecule references based on m/z tolerance.
        /// </summary>
        /// <param name="references">A collection of molecule references to process.</param>
        /// <param name="mzTolerance">The m/z tolerance for grouping peaks.</param>
        /// <returns>A list of tuples where each tuple contains an array of molecule references and a sorted array of grouped m/z values.</returns>
        public List<(MoleculeMsReference[], double[])> MapMzByReferenceGroups(IEnumerable<MoleculeMsReference> references, double mzTolerance) {
            var dict = new Dictionary<HashSet<MoleculeMsReference>, List<double>>(new SetComparer<MoleculeMsReference>());

            var peaks = EnumeratePeaks(references, mzTolerance);
            foreach (var (referencesSet, mz) in peaks) {
                if (!dict.TryGetValue(referencesSet, out var mzList)) {
                    mzList = dict[referencesSet] = [];
                }
                mzList.Add(mz);
            }

            return dict.Select(kvp => (kvp.Key.ToArray(), kvp.Value.ToArray())).ToList();
        }

        /// <summary>
        /// Enumerates peaks from a collection of molecule references, grouping them by m/z tolerance.
        /// </summary>
        /// <param name="references">A collection of molecule references to process.</param>
        /// <param name="mzTolerance">The m/z tolerance for grouping peaks.</param>
        /// <returns>An enumerable of tuples where each tuple contains a set of molecule references and the average m/z value of the group.</returns>
        private static IEnumerable<(HashSet<MoleculeMsReference>, double)> EnumeratePeaks(IEnumerable<MoleculeMsReference> references, double mzTolerance) {
            var buffer = new List<(MoleculeMsReference reference, double mz)>();
            foreach ((MoleculeMsReference reference, double mz) pair in references.SelectMany(r => r.Spectrum, (r, p) => (r, p.Mass)).OrderBy(pair => pair.Mass)) {
                if (buffer.Count == 0) {
                    buffer.Add(pair);
                    continue;
                }

                if (Math.Abs(buffer.First().mz - pair.mz) < mzTolerance) {
                    buffer.Add(pair);
                }
                else {
                    yield return ([.. buffer.Select(p => p.reference)], (buffer.First().mz + buffer.Last().mz) / 2d);
                    buffer.Clear();
                    buffer.Add(pair);
                }
            }
            if (buffer.Count >= 1) {
                yield return ([.. buffer.Select(p => p.reference)], (buffer.First().mz + buffer.Last().mz) / 2d);
            }
        }

        class SetComparer<T> : IEqualityComparer<HashSet<T>>
        {
            public bool Equals(HashSet<T> x, HashSet<T> y)
            {
                if (x == null || y == null) return false;
                return x.SetEquals(y);
            }

            public int GetHashCode(HashSet<T> set)
            {
                if (set == null) return 0;
                int hash = 0;
                foreach (var item in set)
                {
                    hash ^= item?.GetHashCode() ?? 0;
                }
                return hash;
            }
        }
    }
}

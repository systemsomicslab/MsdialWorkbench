using CompMs.Common.Components;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class MzMapper
    {
        /// <summary>
        /// Maps the list of distinct m/z values from molecule reference spectra.
        /// </summary>
        /// <param name="references">A collection of molecule reference to process.</param>
        /// <returns>A dictionary where each key is a molecule reference and the value is a sorted array of distinct m/z values.</returns>
        public Dictionary<MoleculeMsReference, double[]> MapMzValues(IEnumerable<MoleculeMsReference> references) {
            var result = new Dictionary<MoleculeMsReference, double[]>();
            foreach (var scan in references) {
                var mzList = scan.Spectrum?.Select(peak => peak.Mass).Distinct().OrderBy(mz => mz).ToArray() ?? [];
                result[scan] = mzList;
            }
            return result;
        }
    }
}

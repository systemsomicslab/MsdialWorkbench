using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class MzMapper
    {
        /// <summary>
        /// Retrieves and groups the list of m/z values from in silico spectra for each MoleculeMsReference.
        /// </summary>
        /// <param name="groups">Groups by lipid name (MoleculeMsReference)</param>
        /// <returns>For each MoleculeMsReference, a list of m/z values</returns>
        public Dictionary<MoleculeMsReference, IEnumerable<double>> MapMzToGroups(
            ILookup<MoleculeMsReference, AlignmentChromPeakFeature> groups)
        {
            var result = new Dictionary<MoleculeMsReference, IEnumerable<double>>();
            foreach (var group in groups)
            {
                var mzList = group.Key?.Spectrum?.Select(peak => peak.Mass).Distinct().ToList() ?? new List<double>();
                result[group.Key] = mzList;
            }
            return result;
        }
    }
}

using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class Ms2Quantifier
    {
        /// <summary>
        /// Returns a list of abundances for each quant mass in each sample.
        /// </summary>
        /// <param name="mzList">List of m/z values to quantify.</param>
        /// <param name="ms2DataList">MS2 spectral data for each sample.</param>
        /// <param name="features">AlignmentChromPeakFeature for each sample (for sample name).</param>
        /// <returns>For each m/z, a list of abundances for each sample.</returns>
        public IEnumerable<Ms2QuantResult> Quantify(IEnumerable<double> mzList, IEnumerable<MSDecResult> ms2DataList, IEnumerable<AlignmentChromPeakFeature> features)
        {
            var results = new List<Ms2QuantResult>();
            var ms2DataArray = ms2DataList.ToArray();
            var featureArray = features.ToArray();
            if (ms2DataArray.Length != featureArray.Length) throw new ArgumentException("ms2DataList and features must have the same length.");
            foreach (var mz in mzList)
            {
                var abundances = new List<SampleAbundance>();
                for (int i = 0; i < ms2DataArray.Length; i++)
                {
                    var ms2Data = ms2DataArray[i];
                    var feature = featureArray[i];
                    var sampleName = feature.FileName;
                    var peak = ms2Data.Spectrum
                        .OrderBy(p => Math.Abs(p.Mass - mz))
                        .FirstOrDefault();
                    double abundance = peak != null ? peak.Intensity : 0.0;
                    abundances.Add(new SampleAbundance
                    {
                        SampleName = sampleName,
                        Abundance = abundance
                    });
                }
                results.Add(new Ms2QuantResult
                {
                    QuantMass = mz,
                    Abundances = abundances
                });
            }
            return results;
        }
    }
}

using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
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
        /// <param name="files">Array of IFileBean for each sample (for sample name).</param>
        /// <param name="tolerance">Tolerance for matching m/z values.</param>
        /// <returns>For each m/z, a list of abundances for each sample.</returns>
        public List<Ms2QuantResult> Quantify(IEnumerable<double> mzList, IEnumerable<IMSScanProperty?> ms2DataList, IEnumerable<IFileBean> files, double tolerance)
        {
            var results = new List<Ms2QuantResult>();
            var ms2DataArray = ms2DataList as IMSScanProperty?[] ?? ms2DataList.ToArray();
            var fileArray = files as IFileBean[] ?? files.ToArray();
            if (ms2DataArray.Length != fileArray.Length) throw new ArgumentException("ms2DataList and features must have the same length.");
            foreach (var mz in mzList)
            {
                var abundances = new List<SampleAbundance>();
                for (int i = 0; i < ms2DataArray.Length; i++)
                {
                    var ms2Data = ms2DataArray[i];
                    var file = fileArray[i];
                    var sampleName = file.FileName;
                    var abundance = ms2Data?.Spectrum
                        .Where(p => Math.Abs(p.Mass - mz) < tolerance)
                        .DefaultIfEmpty()
                        .Sum(p => p?.Intensity) ?? 0d;
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

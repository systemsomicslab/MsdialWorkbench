using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm {
    public class Annotation {

        // mspDB must be sorted by precursor mz
        // textDB must be sorted by precursor mz
        public void MainProcess(List<RawSpectrum> spectrumList,
            List<ChromatogramPeakFeature> chromPeakFeatures, List<MSDecResult> msdecResults, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            MsdialLcmsParameter param, Action<int> reportAction) {

            Console.WriteLine("Annotation started");
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                // count of chrompeakfeatures and msdecresults should be same
                var chromPeak = chromPeakFeatures[i];
                var msdecResult = msdecResults[i];
                if (chromPeak.PeakCharacter.IsotopeWeightNumber == 0) {
                    LcMsMsMatchMethod(chromPeak, msdecResult, spectrumList, mspDB, textDB, param);
                }
                Console.WriteLine("Done {0}/{1}", i, chromPeakFeatures.Count);
            }
        }

        public void LcMsMsMatchMethod(ChromatogramPeakFeature chromPeak, MSDecResult msdecResult, 
            List<RawSpectrum> spectrumList, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, MsdialLcmsParameter param) {

            if (mspDB.IsEmptyOrNull() && textDB.IsEmptyOrNull()) return;

            var isotopes = DataAccess.GetIsotopicPeaks(spectrumList, chromPeak.MS1RawSpectrumIdTop, (float)chromPeak.Mass, param.CentroidMs1Tolerance);
            var normMSScanProp = DataAccess.GetNormalizedMSScanProperty(chromPeak, msdecResult, param);

            var mz = chromPeak.Mass;
            var ms1Tol = param.MspSearchParam.Ms1Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }

            if (!mspDB.IsEmptyOrNull()) {
                var startID = SearchCollection.LowerBound(mspDB,
                    new MoleculeMsReference() { PrecursorMz = chromPeak.Mass - ms1Tol },
                    (a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));
                var candidates = new List<MsScanMatchResult>();
                for (int i = startID; i < mspDB.Count; i++) {
                    var refSpec = mspDB[i];
                    if (refSpec.PrecursorMz > mz + ms1Tol) break;
                    if (refSpec.PrecursorMz < mz - ms1Tol) continue;

                    MsScanMatchResult result = null;
                    if (param.TargetOmics == Common.Enum.TargetOmics.Metablomics) {
                        result = MsScanMatching.CompareMS2ScanProperties(msdecResult, refSpec, param.MspSearchParam, isotopes, refSpec.IsotopicPeaks);
                    }
                    else if (param.TargetOmics == Common.Enum.TargetOmics.Lipidomics) {
                        result = MsScanMatching.CompareMS2LipidomicsScanProperties(msdecResult, refSpec, param.MspSearchParam, isotopes, refSpec.IsotopicPeaks);
                    }
                    if (result.IsSpectrumMatch || result.IsPrecursorMzMatch) {
                        result.LibraryIDWhenOrdered = i;
                        candidates.Add(result);
                    }
                }

                foreach (var (result, index) in candidates.OrEmptyIfNull().OrderByDescending(n => n.TotalScore).WithIndex()) {
                    if (index == 0) {
                        chromPeak.MspBasedMatchResult = result;
                        chromPeak.MspID = result.LibraryID;
                        chromPeak.MspIDWhenOrdered = result.LibraryIDWhenOrdered;
                        DataAccess.SetMoleculeMsProperty(chromPeak, mspDB[result.LibraryIDWhenOrdered], result);
                    }
                    chromPeak.MspIDs.Add(result.LibraryID);
                }
            }

            if (!textDB.IsEmptyOrNull()) {
                var startID = SearchCollection.LowerBound(textDB,
                    new MoleculeMsReference() { PrecursorMz = chromPeak.Mass - ms1Tol },
                    (a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));
                var candidates = new List<MsScanMatchResult>();
                for (int i = startID; i < mspDB.Count; i++) {
                    var refSpec = mspDB[i];
                    if (refSpec.PrecursorMz > mz + ms1Tol) break;
                    if (refSpec.PrecursorMz < mz - ms1Tol) continue;

                    var result = MsScanMatching.CompareMS2ScanProperties(msdecResult, refSpec, param.MspSearchParam, isotopes, refSpec.IsotopicPeaks);
                    if (result.IsPrecursorMzMatch) {
                        result.LibraryIDWhenOrdered = i;
                        candidates.Add(result);
                    }
                }

                foreach (var (result, index) in candidates.OrEmptyIfNull().OrderByDescending(n => n.TotalScore).WithIndex()) {
                    if (index == 0) {
                        chromPeak.TextDbBasedMatchResult = result;
                        chromPeak.TextDbID = result.LibraryID;
                        chromPeak.TextDbIDWhenOrdered = result.LibraryIDWhenOrdered;
                        DataAccess.SetMoleculeMsProperty(chromPeak, textDB[result.LibraryIDWhenOrdered], result, true);
                    }
                    chromPeak.TextDbIDs.Add(result.LibraryID);
                }
            }
        }
    }
}

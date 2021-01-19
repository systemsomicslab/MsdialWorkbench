using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;

namespace CompMs.MsdialGcMsApi.Algorithm
{
    public class GcmsDataAccessor : DataAccessor
    {
        IComparer<IMSScanProperty> Comparer { get; }

        public GcmsDataAccessor(AlignmentIndexType indexType) {
            switch (indexType) {
                case AlignmentIndexType.RI:
                    Comparer = ChromXsComparer.RIComparer;
                    break;
                case AlignmentIndexType.RT:
                    Comparer = ChromXsComparer.RTComparer;
                    break;
                default:
                    Comparer = ChromXsComparer.RIComparer;
                    break;
            }

        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);
            msdecResults.Sort(Comparer);
            return msdecResults.Cast<IMSScanProperty>().ToList();
        }

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, List<RawSpectrum> spectrum) {
            throw new NotImplementedException();
        }
    }
}

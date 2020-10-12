using System;
using System.Collections.Generic;

using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;

namespace CompMs.MsdialLcImMsApi.Algorithm
{
    public class LcimmsDataAccessor : DataAccessor
    {
        static readonly IComparer<IMSScanProperty> Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer, ChromXsComparer.DriftComparer);

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(Comparer);
            return new List<IMSScanProperty>(chromatogram);
        }
    }
}

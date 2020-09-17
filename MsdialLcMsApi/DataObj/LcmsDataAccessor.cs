using System;
using System.Collections.Generic;

using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;

namespace CompMs.MsdialLcMsApi.DataObj
{
    class LcmsDataAccessor : DataAccessor
    {
        static readonly IComparer<IMSScanProperty> Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(Comparer);
            return new List<IMSScanProperty>(chromatogram);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;

namespace CompMs.MsdialDimsCore.DataObj
{
    public class DimsDataAccessor : DataAccessor
    {
        readonly IComparer<IMSScanProperty> Comparer = ChromXsComparer.MzComparer;
        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(Comparer);
            return chromatogram.Cast<IMSScanProperty>().ToList();
        }
    }
}

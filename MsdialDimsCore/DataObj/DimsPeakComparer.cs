using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore.DataObj
{
    public class DimsPeakComparer : PeakComparer
    {
        private double _mztol;

        public DimsPeakComparer(double mztol) {
            _mztol = mztol;
        }

        public override int Compare(IMSScanProperty x, IMSScanProperty y) {
            return x.PrecursorMz.CompareTo(y.PrecursorMz);
        }

        public override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol;
        }

        public override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2));
        }

        public override ChromXs GetCenter(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return new ChromXs(chromFeatures.Select(n => n.ChromXsTop).Average(p => p.Mz.Value), ChromXType.Mz, ChromXUnit.Mz);
        }

        public override double GetAveragePeakWidth(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return chromFeatures.Max(n => n.PeakWidth(ChromXType.Mz));
        }
    }

    public class DimsAlignmentProcessFactory : AlignmentProcessFactory {
        private MsdialDimsParameter Param;
        private List<AnalysisFileBean> Files;
        private Dictionary<int, RawPeakElement[]> peakElementsMemo = new Dictionary<int, RawPeakElement[]>();

        public DimsAlignmentProcessFactory(List<AnalysisFileBean> files, MsdialDimsParameter param) {
            this.Files = files;
            this.Param = param;
            this.IonMode = param.IonMode;
            this.SmoothingMethod = param.SmoothingMethod;
            this.SmoothingLevel = param.SmoothingLevel;
            this.MzTol = param.Ms1AlignmentTolerance;
            this.IsForceInsert = param.IsForceInsertForGapFilling;
        }

        public override List<ChromatogramPeak> PeaklistOnChromCenter(ChromXs center, double peakWidth, List<RawSpectrum> spectrumList, int fileID) {
            RawPeakElement[] peakElements;
            if (!peakElementsMemo.ContainsKey(fileID))
                peakElementsMemo[fileID] = DataAccess.AccumulateMS1Spectrum(spectrumList); // TODO: remove cache (too much memory use)
            peakElements = peakElementsMemo[fileID];
            var peaklist = DataAccess.ConvertRawPeakElementToChromatogramPeakList(peakElements, center.Mz.Value - peakWidth * 2.0, center.Mz.Value + peakWidth * 2.0);
            return peaklist;
        }
    }
}

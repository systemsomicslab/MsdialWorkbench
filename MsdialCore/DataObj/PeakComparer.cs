using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Accord.Statistics.Models.Markov.Topology;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;

namespace CompMs.MsdialCore.DataObj
{
    public abstract class PeakComparer : IEqualityComparer<IMSScanProperty>, IComparer<IMSScanProperty>
    {
        public abstract int Compare(IMSScanProperty x, IMSScanProperty y);
        public abstract bool Equals(IMSScanProperty x, IMSScanProperty y);
        public abstract double GetSimilality(IMSScanProperty x, IMSScanProperty y);

        public virtual ChromXs GetCenter(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            var chromXs = new ChromXs(chromFeatures.Select(n => n.ChromXsTop).Average(p => p.RT.Value), ChromXType.RT, ChromXUnit.Min);
            chromXs.Mz = new MzValue(chromFeatures.Argmax(n => n.PeakHeightTop).Mass);
            return chromXs;
        }

        public virtual double GetAveragePeakWidth(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return chromFeatures.Max(n => n.PeakWidth(ChromXType.RT));
        }

        public virtual int GetHashCode(IMSScanProperty obj) {
            return obj.GetHashCode();
        }
    }

    public abstract class AlignmentProcessFactory {
        public List<MoleculeMsReference> MspDB;
        public bool IsRepresentativeQuantMassBasedOnBasePeakMz;

        public IonMode IonMode;
        public SmoothingMethod SmoothingMethod;
        public int SmoothingLevel;
        public double MzTol;
        public double RtTol;
        public double RiTol;
        public double DtTol;
        public bool IsForceInsert;

        public virtual List<ChromatogramPeak> PeaklistOnChromCenter(ChromXs center, double peakWidth, List<RawSpectrum> spectrumList, int fileID = -1) {
            return new List<ChromatogramPeak>();
        }

        public virtual int ChromStartIndex(List<ChromatogramPeak> peaks, ChromXs center, double tolerance) {
            return SearchCollection.LowerBound(peaks, new ChromatogramPeak { ChromXs = new ChromXs(center.RT.Value - tolerance) }, (a, b) => a.ChromXs.RT.Value.CompareTo(b.ChromXs.RT.Value));
        }
    }
}

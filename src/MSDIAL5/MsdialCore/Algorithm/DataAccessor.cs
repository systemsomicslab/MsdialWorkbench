using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm
{
    public abstract class DataAccessor {
        public abstract List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile);
        public abstract ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance);
    }

    public class IDComparer : IComparer<IMSScanProperty>
    {
        public static readonly IComparer<IMSScanProperty> Comparer = new IDComparer();

        IDComparer() { }
        public int Compare(IMSScanProperty x, IMSScanProperty y) {
            return x.ScanID.CompareTo(y.ScanID);
        }
    }

    public class MassComparer : IComparer<IMSProperty>
    {
        public static readonly IComparer<IMSProperty> Comparer = new MassComparer();

        MassComparer() { }
        public int Compare(IMSProperty x, IMSProperty y) {
            return x.PrecursorMz.CompareTo(y.PrecursorMz);
        }
    }

    public class ChromXsComparer
    {
        public static readonly IComparer<IMSProperty> ChromXComparer = new ChromXComparerImpl();
        public static readonly IComparer<IMSProperty> RTComparer = new RTComparerImpl();
        public static readonly IComparer<IMSProperty> RIComparer = new RIComparerImpl();
        public static readonly IComparer<IMSProperty> MzComparer = new MzComparerImpl();
        public static readonly IComparer<IMSProperty> DriftComparer = new DriftComparerImpl();

        public static IComparer<IMSProperty> GetComparer(ChromXType type) {
            switch (type) {
                case ChromXType.RT:
                    return RTComparer;
                case ChromXType.RI:
                    return RIComparer;
                case ChromXType.Mz:
                    return MzComparer;
                case ChromXType.Drift:
                    return DriftComparer;
            }
            return ChromXComparer;
        }

        ChromXsComparer() { }
        class ChromXComparerImpl : IComparer<IMSProperty>
        {
            public int Compare(IMSProperty x, IMSProperty y) {
                return x.ChromXs.Value.CompareTo(y.ChromXs.Value);
            }
        }

        class RTComparerImpl : IComparer<IMSProperty>
        {
            public int Compare(IMSProperty x, IMSProperty y) {
                return x.ChromXs.RT.Value.CompareTo(y.ChromXs.RT.Value);
            }
        }
        class RIComparerImpl : IComparer<IMSProperty>
        {
            public int Compare(IMSProperty x, IMSProperty y) {
                return x.ChromXs.RI.Value.CompareTo(y.ChromXs.RI.Value);
            }
        }
        class MzComparerImpl : IComparer<IMSProperty>
        {
            public int Compare(IMSProperty x, IMSProperty y) {
                return x.ChromXs.Mz.Value.CompareTo(y.ChromXs.Mz.Value);
            }
        }
        class DriftComparerImpl : IComparer<IMSProperty>
        {
            public int Compare(IMSProperty x, IMSProperty y) {
                return x.ChromXs.Drift.Value.CompareTo(y.ChromXs.Drift.Value);
            }
        }
    }

    public class CollisionCrossSectionComparer : IComparer<IIonProperty>
    {
        public static readonly IComparer<IIonProperty> Comparer;

        static CollisionCrossSectionComparer() {
            Comparer = new CollisionCrossSectionComparer();
        }

        CollisionCrossSectionComparer() { }

        public int Compare(IIonProperty x, IIonProperty y) {
            return x.CollisionCrossSection.CompareTo(y.CollisionCrossSection);
        }
    }

}

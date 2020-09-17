using System;
using System.Collections.Generic;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.DataObj
{
    public abstract class DataAccessor {
        public abstract List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile);
    }

    public class IDComparer : IComparer<IMSScanProperty>
    {
        public static readonly IComparer<IMSScanProperty> Comparer = new IDComparer();

        IDComparer() { }
        public int Compare(IMSScanProperty x, IMSScanProperty y) {
            return x.ScanID.CompareTo(y.ScanID);
        }
    }

    public class MassComparer : IComparer<IMSScanProperty>
    {
        public static readonly IComparer<IMSScanProperty> Comparer = new MassComparer();

        MassComparer() { }
        public int Compare(IMSScanProperty x, IMSScanProperty y) {
            return x.PrecursorMz.CompareTo(y.PrecursorMz);
        }
    }

    public class ChromXsComparer
    {
        public static readonly IComparer<IMSScanProperty> ChromXComparer = new ChromXComparerImpl();
        public static readonly IComparer<IMSScanProperty> RTComparer = new RTComparerImpl();
        public static readonly IComparer<IMSScanProperty> RIComparer = new RIComparerImpl();
        public static readonly IComparer<IMSScanProperty> MzComparer = new MzComparerImpl();
        public static readonly IComparer<IMSScanProperty> DriftComparer = new DriftComparerImpl();

        public static IComparer<IMSScanProperty> GetComparer(ChromXType type) {
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
        class ChromXComparerImpl : IComparer<IMSScanProperty>
        {
            public int Compare(IMSScanProperty x, IMSScanProperty y) {
                return x.ChromXs.Value.CompareTo(y.ChromXs.Value);
            }
        }

        class RTComparerImpl : IComparer<IMSScanProperty>
        {
            public int Compare(IMSScanProperty x, IMSScanProperty y) {
                return x.ChromXs.RT.Value.CompareTo(y.ChromXs.RT.Value);
            }
        }
        class RIComparerImpl : IComparer<IMSScanProperty>
        {
            public int Compare(IMSScanProperty x, IMSScanProperty y) {
                return x.ChromXs.RI.Value.CompareTo(y.ChromXs.RI.Value);
            }
        }
        class MzComparerImpl : IComparer<IMSScanProperty>
        {
            public int Compare(IMSScanProperty x, IMSScanProperty y) {
                return x.ChromXs.Mz.Value.CompareTo(y.ChromXs.Mz.Value);
            }
        }
        class DriftComparerImpl : IComparer<IMSScanProperty>
        {
            public int Compare(IMSScanProperty x, IMSScanProperty y) {
                return x.ChromXs.Drift.Value.CompareTo(y.ChromXs.Drift.Value);
            }
        }
    }

}

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
            var chromXs = new ChromXs(chromFeatures.Select(n => n.ChromXsTop).Average(p => p.RT.Value), ChromXType.RT, ChromXUnit.Min)
            {
                Mz = new MzValue(chromFeatures.Argmax(n => n.PeakHeightTop).Mass)
            };
            return chromXs;
        }

        public virtual double GetAveragePeakWidth(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return chromFeatures.Max(n => n.PeakWidth(ChromXType.RT));
        }

        public virtual int GetHashCode(IMSScanProperty obj) {
            return obj.GetHashCode();
        }
    }
}

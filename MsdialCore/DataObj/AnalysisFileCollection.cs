using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnalysisFileCollection
    {
        private readonly IList<AnalysisFileBean> _items;

        public AnalysisFileCollection(IEnumerable<AnalysisFileBean> items) {
            if (items is null) {
                throw new ArgumentNullException(nameof(items));
            }
            _items = (items as IList<AnalysisFileBean>) ?? new List<AnalysisFileBean>(items);
        }

        public double MinimumLowessSpan() {
            var count = _items.Count(item => item.AnalysisFileIncluded && item.AnalysisFileType == AnalysisFileType.QC);
            var minOptSize = SmootherMathematics.GetMinimumLowessSpan(count);
            return minOptSize;
        }
    }
}

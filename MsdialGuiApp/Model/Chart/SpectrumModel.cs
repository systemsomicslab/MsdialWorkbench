using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Chart
{
    public class SpectrumModel : BindableBase
    {
        public SpectrumModel(
            List<SpectrumPeak> spectrums,
            IAxisManager horizontalAxis,
            IAxisManager verticalAxis,
            string horizontalProperty,
            string verticalProperty,
            string graphTitle) {

            Spectrums = spectrums;
            HorizontalAxis = horizontalAxis;
            VerticalAxis = verticalAxis;
            HorizontalProperty = horizontalProperty;
            VerticalProperty = verticalProperty;
            GraphTitle = graphTitle;
        }

        public List<SpectrumPeak> Spectrums { get; }

        public IAxisManager HorizontalAxis { get; }
        public IAxisManager VerticalAxis { get; }

        public string HorizontalProperty { get; }

        public string VerticalProperty { get; }

        public string GraphTitle { get; }
    }
}

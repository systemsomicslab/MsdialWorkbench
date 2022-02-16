using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ChromatogramsViewModel : ViewModelBase {
        public ChromatogramsViewModel(
           ChromatogramsModel model,
           IAxisManager<double> horizontalAxis = null,
           IAxisManager<double> verticalAxis = null) {

            DisplayChromatograms = model.DisplayChromatograms;

            if (horizontalAxis is null) {
                horizontalAxis = new ContinuousAxisManager<double>(model.ChromRangeSource);
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis is null) {
                //verticalAxis = model.AbundanceRangeSource
                //    .ToReactiveAxisManager<double>(new RelativeMargin(0, 0.1), new Range(0d, 0d), LabelType.Order)
                //    .AddTo(Disposables);
                verticalAxis = new ContinuousAxisManager<double>(model.AbundanceRangeSource) { LabelType = LabelType.Order };
            }
            VerticalAxis = verticalAxis;

            GraphTitle = model.GraphTitle;
            HorizontalTitle = model.HorizontalTitle;
            VerticalTitle = model.VerticalTitle;
            HorizontalProperty = model.HorizontalProperty;
            VerticalProperty = model.VerticalProperty;
        }

        public List<DisplayChromatogram> DisplayChromatograms { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public string GraphTitle { get; }

        public string HorizontalTitle { get; }

        public string VerticalTitle { get; }

        public string HorizontalProperty { get; }

        public string VerticalProperty { get; }
    }
}

using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model
{
    public class AnalysisPeakPlotModel : ValidatableBase
    {
        public AnalysisPeakPlotModel(
            IList<ChromatogramPeakFeatureVM> spots,
            IAxisManager horizontalAxis,
            IAxisManager verticalAxis) {

            Spots = spots;
            HorizontalAxis = horizontalAxis;
            VerticalAxis = verticalAxis;
        }

        public IList<ChromatogramPeakFeatureVM> Spots {
            get => spots;
            set => SetProperty(ref spots, value);
        }
        private IList<ChromatogramPeakFeatureVM> spots;

        public IAxisManager HorizontalAxis {
            get => horizontalAxis;
            set => SetProperty(ref horizontalAxis, value);
        }
        private IAxisManager horizontalAxis;

        public IAxisManager VerticalAxis {
            get => verticalAxis;
            set => SetProperty(ref verticalAxis, value);
        }
        private IAxisManager verticalAxis;

        public ChromatogramPeakFeatureVM Target {
            get => target;
            set => SetProperty(ref target, value);
        }
        private ChromatogramPeakFeatureVM target;
    }
}

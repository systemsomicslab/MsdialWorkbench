using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model
{
    public class AlignmentPeakPlotModel : ValidatableBase
    {
        public AlignmentPeakPlotModel(
            IList<AlignmentSpotPropertyModel> spots,
            IAxisManager horizontalAxis,
            IAxisManager verticalAxis) {

            Spots = spots;
            HorizontalAxis = horizontalAxis;
            VerticalAxis = verticalAxis;
        }

        public IList<AlignmentSpotPropertyModel> Spots {
            get => spots;
            set => SetProperty(ref spots, value);
        }
        private IList<AlignmentSpotPropertyModel> spots;

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

        public AlignmentSpotPropertyModel Target {
            get => target;
            set => SetProperty(ref target, value);
        }
        private AlignmentSpotPropertyModel target;
    }
}

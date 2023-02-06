using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public sealed class ChromatogramsViewModel : ViewModelBase {
        public ChromatogramsViewModel(
           ChromatogramsModel model,
           IAxisManager<double> horizontalAxis = null,
           IAxisManager<double> verticalAxis = null) {

            DisplayChromatograms = model.DisplayChromatograms;

            HorizontalAxis = horizontalAxis ?? model.ChromAxis;
            VerticalAxis = verticalAxis ?? model.AbundanceAxis;

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

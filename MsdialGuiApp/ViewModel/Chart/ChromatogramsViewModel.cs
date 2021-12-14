using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Chart {
    class ChromatogramsViewModel : ViewModelBase {
        public ChromatogramsViewModel(
           ChromatogramsModel model,
           IAxisManager<double> horizontalAxis = null,
           IAxisManager<double> verticalAxis = null) {

            Chromatograms = model.DisplayChromatograms;

            if (horizontalAxis is null) {
                horizontalAxis = new ContinuousAxisManager<double>(model.ChromRangeSource);
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis is null) {
                verticalAxis = new ContinuousAxisManager<double>(model.AbundanceRangeSource);
            }
            VerticalAxis = verticalAxis;

            GraphTitle = model.GraphTitle;
            HorizontalTitle = model.HorizontalTitle;
            VerticalTitle = model.VerticalTitle;
            HorizontalProperty = model.HorizontalProperty;
            VerticalProperty = model.VerticalProperty;
        }

        public List<DisplayChromatogram> Chromatograms { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public string GraphTitle { get; }

        public string HorizontalTitle { get; }

        public string VerticalTitle { get; }

        public string HorizontalProperty { get; }

        public string VerticalProperty { get; }
    }
}

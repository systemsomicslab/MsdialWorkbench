using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class ChromatogramsViewModel : ViewModelBase {
        private readonly ChromatogramsModel _model;

        public ChromatogramsViewModel(ChromatogramsModel model) {
            _model = model;

            DisplayChromatograms = model.DisplayChromatograms;

            HorizontalAxis = model.ChromAxis;
            VerticalAxis = model.AbundanceAxis;

            HorizontalTitle = model.HorizontalTitle;
            VerticalTitle = model.VerticalTitle;

            GraphTitle = model.GraphTitle;
            HorizontalProperty = model.HorizontalProperty;
            VerticalProperty = model.VerticalProperty;
        }

        public List<DisplayChromatogram> DisplayChromatograms { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public AxisItemSelector<double> HorizontalSelector => _model.ChromAxisItemSelector;
        public AxisItemSelector<double> VerticalSelector => _model.AbundanceAxisItemSelector;

        public string GraphTitle { get; }

        public string HorizontalTitle { get; }

        public string VerticalTitle { get; }

        public string HorizontalProperty { get; }

        public string VerticalProperty { get; }
    }
}

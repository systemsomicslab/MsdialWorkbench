using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class ChromatogramsViewModel : ViewModelBase {
        private readonly ChromatogramsModel _model;

        public ChromatogramsViewModel(ChromatogramsModel model) {
            _model = model;

            DisplayChromatograms = model.DisplayChromatograms;

            GraphTitle = model.GraphTitle;
            HorizontalProperty = model.HorizontalProperty;
            VerticalProperty = model.VerticalProperty;
        }

        public List<DisplayChromatogram> DisplayChromatograms { get; }

        public AxisItemSelector<double> HorizontalSelector => _model.ChromAxisItemSelector;
        public AxisItemSelector<double> VerticalSelector => _model.AbundanceAxisItemSelector;

        public string GraphTitle { get; }

        public string HorizontalProperty { get; }

        public string VerticalProperty { get; }
    }
}

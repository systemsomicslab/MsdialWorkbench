using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class RawDecSpectrumsViewModel : ViewModelBase
    {
        public RawDecSpectrumsViewModel(
            RawDecSpectrumsModel model,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> upperVerticalAxis = null,
            IAxisManager<double> lowerVerticalAxis = null) {

            this.model = model;
            RawRefSpectrumViewModels = new MsSpectrumViewModel<ChromatogramPeakFeatureModel>(
                model.RawRefSpectrumModels,
                horizontalAxis,
                upperVerticalAxis,
                lowerVerticalAxis);

            DecRefSpectrumViewModels = new MsSpectrumViewModel<ChromatogramPeakFeatureModel>(
                model.DecRefSpectrumModels,
                horizontalAxis,
                upperVerticalAxis,
                lowerVerticalAxis);
        }

        private readonly RawDecSpectrumsModel model;

        public MsSpectrumViewModel<ChromatogramPeakFeatureModel> RawRefSpectrumViewModels { get; }

        public MsSpectrumViewModel<ChromatogramPeakFeatureModel> DecRefSpectrumViewModels { get; }

    }
}

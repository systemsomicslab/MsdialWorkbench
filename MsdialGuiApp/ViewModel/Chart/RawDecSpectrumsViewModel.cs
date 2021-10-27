using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
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
            IAxisManager<double> lowerVerticalAxis = null,
            IObservable<IBrushMapper<SpectrumComment>> upperSpectrumBrushSource = null,
            IObservable<IBrushMapper<SpectrumComment>> lowerSpectrumBrushSource = null) {

            this.model = model;
            RawRefSpectrumViewModels = new MsSpectrumViewModel(
                model.RawRefSpectrumModels,
                horizontalAxis,
                upperVerticalAxis,
                lowerVerticalAxis,
                upperSpectrumBrushSource,
                lowerSpectrumBrushSource);

            DecRefSpectrumViewModels = new MsSpectrumViewModel(
                model.DecRefSpectrumModels,
                horizontalAxis,
                upperVerticalAxis,
                lowerVerticalAxis,
                upperSpectrumBrushSource,
                lowerSpectrumBrushSource);
        }

        private readonly RawDecSpectrumsModel model;

        public MsSpectrumViewModel RawRefSpectrumViewModels { get; }

        public MsSpectrumViewModel DecRefSpectrumViewModels { get; }

    }
}

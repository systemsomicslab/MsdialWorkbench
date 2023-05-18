using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class RawPurifiedSpectrumsViewModel : ViewModelBase
    {
        private readonly RawPurifiedSpectrumsModel _model;

        public RawPurifiedSpectrumsViewModel(RawPurifiedSpectrumsModel model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            RawSpectrumViewModel = new SingleSpectrumViewModel(model.RawSpectrumModel).AddTo(Disposables);
            DecSpectrumViewModel = new SingleSpectrumViewModel(model.DecSpectrumModel).AddTo(Disposables);
            _model = model;
        }

        public SingleSpectrumViewModel RawSpectrumViewModel { get; }
        public SingleSpectrumViewModel DecSpectrumViewModel { get; }
        public IAxisManager<double> HorizontalAxis => _model.HorizontalAxis;
    }
}

using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Algorithm.Scoring;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(MsSpectrumModel rawRefSpectrumModels, MsSpectrumModel decRefSpectrumModels, MultiMsmsRawSpectrumLoader loader) {
            RawRefSpectrumModels = rawRefSpectrumModels ?? throw new ArgumentNullException(nameof(rawRefSpectrumModels));
            DecRefSpectrumModels = decRefSpectrumModels ?? throw new ArgumentNullException(nameof(decRefSpectrumModels));
            RawLoader = loader;
        }

        public RawDecSpectrumsModel(SingleSpectrumModel rawSpectrumModel, SingleSpectrumModel decSpectrumModel, SingleSpectrumModel referenceSpectrumModel, IObservable<Ms2ScanMatching?> ms2ScanMatching, MultiMsmsRawSpectrumLoader? loader = null) {
            if (rawSpectrumModel is null) {
                throw new ArgumentNullException(nameof(rawSpectrumModel));
            }

            if (decSpectrumModel is null) {
                throw new ArgumentNullException(nameof(decSpectrumModel));
            }

            if (referenceSpectrumModel is null) {
                throw new ArgumentNullException(nameof(referenceSpectrumModel));
            }

            if (ms2ScanMatching is null) {
                throw new ArgumentNullException(nameof(ms2ScanMatching));
            }

            RawRefSpectrumModels = new MsSpectrumModel(rawSpectrumModel, referenceSpectrumModel, ms2ScanMatching)
            {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative abundance",
            }.AddTo(Disposables);

            IsRawSpectrumOverlayVisible = new ReactivePropertySlim<bool>(true).AddTo(Disposables);


            var decRawOverlaySpectrumModel = rawSpectrumModel.Clone().AddTo(Disposables);
            decRawOverlaySpectrumModel.IsVisible.Value = false;
            decRawOverlaySpectrumModel.IsAnnotationVisible.Value = false;
            decRawOverlaySpectrumModel.LineThickness.Value = 1d;
            decRawOverlaySpectrumModel.StrokeDashArray.Value = new DoubleCollection { 3d, 3d };
            decRawOverlaySpectrumModel.Brush = new ConstantBrushMapper<object>(Brushes.DarkGray);

            DecRefSpectrumModels = new MsSpectrumModel(decSpectrumModel, referenceSpectrumModel, ms2ScanMatching)
            {
                GraphTitle =  "Deconvolution vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative abundacne",
            }.AddTo(Disposables);
            DecRefSpectrumModels.UpperSpectraModel.Insert(0, decRawOverlaySpectrumModel);

            IsRawSpectrumOverlayVisible
                .Subscribe(isVisible => {
                    decRawOverlaySpectrumModel.IsVisible.Value = isVisible;
                })
                .AddTo(Disposables);

            RawLoader = loader;
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public ReactivePropertySlim<bool> IsRawSpectrumOverlayVisible { get; }
        public MultiMsmsRawSpectrumLoader? RawLoader { get; }
    }
}

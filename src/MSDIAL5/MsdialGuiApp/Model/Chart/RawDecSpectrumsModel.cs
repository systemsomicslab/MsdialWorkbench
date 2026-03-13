using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.MsResult;
using CompMs.Common.Algorithm.Scoring;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(SingleSpectrumModel rawSpectrumModel, SingleSpectrumModel? q1DecSpectrumModel, SingleSpectrumModel decSpectrumModel, SingleSpectrumModel referenceSpectrumModel, IObservable<Ms2ScanMatching?> ms2ScanMatching, IMultiMsmsSpectrumLoader<ChromatogramPeakFeatureModel>?[]? loaders) {
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

            IsRawSpectrumOverlayVisible = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            SingleSpectrumModel? q1RawOverlaySpectrumModel = null;
            if (q1DecSpectrumModel is not null) {
                q1RawOverlaySpectrumModel = rawSpectrumModel.Clone().AddTo(Disposables);
                q1RawOverlaySpectrumModel.IsVisible.Value = false;
                q1RawOverlaySpectrumModel.IsAnnotationVisible.Value = false;
                q1RawOverlaySpectrumModel.LineThickness.Value = 1d;
                q1RawOverlaySpectrumModel.StrokeDashArray.Value = new DoubleCollection { 3d, 3d };
                q1RawOverlaySpectrumModel.Brush = new ConstantBrushMapper<object>(Brushes.DarkGray);

                Q1DecRefSpectrumModels = new MsSpectrumModel(q1DecSpectrumModel, referenceSpectrumModel, ms2ScanMatching)
                {
                    GraphTitle = "Q1 Deconvolution vs. Reference",
                    HorizontalTitle = "m/z",
                    VerticalTitle = "Relative abundance",
                }.AddTo(Disposables);
                Q1DecRefSpectrumModels.UpperSpectraModel.Insert(0, q1RawOverlaySpectrumModel);
            }

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
                    if (q1RawOverlaySpectrumModel is not null) {
                        q1RawOverlaySpectrumModel.IsVisible.Value = isVisible;
                    }
                })
                .AddTo(Disposables);

            RawLoader = loaders?.ElementAtOrDefault(0);
            Q1DecLoader = loaders?.ElementAtOrDefault(1);
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel? Q1DecRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public ReactivePropertySlim<bool> IsRawSpectrumOverlayVisible { get; }
        public IMultiMsmsSpectrumLoader<ChromatogramPeakFeatureModel>? RawLoader { get; }
        public IMultiMsmsSpectrumLoader<ChromatogramPeakFeatureModel>? Q1DecLoader { get; }

        public ProductIonIntensityMapModel? ProductIonIntensityMapModel { get; set; }
    }
}

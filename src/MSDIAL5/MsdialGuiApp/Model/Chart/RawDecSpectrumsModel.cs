using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Algorithm.Scoring;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(SingleSpectrumModel rawSpectrumModel, SingleSpectrumModel decSpectrumModel, SingleSpectrumModel referenceSpectrumModel, IObservable<Ms2ScanMatching?> ms2ScanMatching, MultiMsmsRawSpectrumLoader? loader = null, IObservable<ChromatogramPeakFeatureModel?>? target = null) {
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
            }.AddTo(Disposables);
            DecRefSpectrumModels.UpperSpectraModel.Insert(0, decRawOverlaySpectrumModel);

            IsRawSpectrumOverlayVisible
                .Subscribe(isVisible => {
                    decRawOverlaySpectrumModel.IsVisible.Value = isVisible;
                })
                .AddTo(Disposables);

            RawLoader = loader;
            _target = target;
            _referenceSpectrum = referenceSpectrumModel.MsSpectrum;
        }

        private readonly IObservable<ChromatogramPeakFeatureModel?>? _target;
        private readonly IObservable<MsSpectrum?> _referenceSpectrum;

        /// <summary>Whether the MS2 scans of the selected peak can be listed: the raw spectra and the selected peak are both known.</summary>
        public bool CanShowMs2SpectrumList => RawLoader is not null && _target is not null;

        /// <summary>
        /// The MS2 spectra of every MS2 scan of the selected peak, the representative scan first.
        /// It follows the selected peak, and the vertical axis type of <paramref name="spectrumModel"/> when given, until it is disposed.
        /// </summary>
        public Ms2SpectrumListModel CreateMs2SpectrumListModel(MsSpectrumModel? spectrumModel = null) {
            if (RawLoader is not { } loader || _target is not { } target) {
                throw new InvalidOperationException("The MS2 scans of the peak are not available.");
            }
            return Ms2SpectrumListModel.Create(
                "MS2 spectrum list of the peak",
                "MS2 scans",
                target,
                DescribePeak,
                loader.LoadMs2SpectrumListAsync,
                _referenceSpectrum,
                spectrumModel?.UpperVerticalAxisSelector.GetAxisItemAsObservable().Select(item => (string?)item?.Label));
        }

        private static string DescribePeak(ChromatogramPeakFeatureModel peak) {
            var header = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Peak ID {0}, m/z {1:F4}", peak.MasterPeakID, peak.Mass);
            return string.IsNullOrWhiteSpace(peak.Name) ? header : $"{header}, {peak.Name}";
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public ReactivePropertySlim<bool> IsRawSpectrumOverlayVisible { get; }
        public MultiMsmsRawSpectrumLoader? RawLoader { get; }
    }
}

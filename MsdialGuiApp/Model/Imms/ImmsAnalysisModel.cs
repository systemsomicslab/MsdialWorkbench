using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using NSSplash;
using NSSplash.impl;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsAnalysisModel : ViewModelBase {
        public ImmsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            IMatchResultRefer refer,
            ParameterBase parameter,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> textDBAnnotator) {

            this.provider = provider;
            this.parameter = parameter as MsdialImmsParameter;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            FileName = analysisFile.AnalysisFileName;
            peakAreaFile = analysisFile.PeakAreaBeanInformationFilePath;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(peakAreaFile);
            Ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>(
                peaks.Select(peak => new ChromatogramPeakFeatureModel(peak, parameter.TargetOmics != CompMs.Common.Enum.TargetOmics.Metabolomics))
            );
            AmplitudeOrderMin = Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0;
            AmplitudeOrderMax = Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0;

            Ms2SpectrumModel = new Chart.RawDecSpectrumsModel(
                new MsRawSpectrumLoader(provider, parameter),
                new MsDecSpectrumLoader(analysisFile.DeconvolutionFilePath, Ms1Peaks),
                new MsRefSpectrumLoader(refer),
                peak => peak.Mass,
                peak => peak.Intensity)
            {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Aubndance",
                HorizontaProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            };

            EicModel = new Chart.EicModel(
                new EicLoader(provider, parameter, ChromXType.Drift, ChromXUnit.Msec, this.parameter.DriftTimeBegin, this.parameter.DriftTimeEnd)
                )
            {
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "Abundance",
            };
            PlotModel = new Chart.AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass)
            {
                HorizontalTitle = EicModel.HorizontalTitle,
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            };

            Target = PlotModel
                .ObserveProperty(m => m.Target)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            Target
                .Where(t => t != null)
                .Subscribe(t => PlotModel.GraphTitle = $"Spot ID: {t.InnerModel.MasterPeakID} Scan: {t.InnerModel.MS1RawSpectrumIdTop} Mass m/z: {t.InnerModel.Mass:N5}");
            Target
                .Where(t => t == null)
                .Subscribe(_ => PlotModel.GraphTitle = string.Empty);
            Target.Subscribe(async t => await OnTargetChangedAsync(t));

            SurveyScanModel = new Chart.SurveyScanModel(
                Target.SelectMany(t =>
                    Observable.DeferAsync(async token => {
                        var result = await LoadMs1SpectrumAsync(t, token);
                        return Observable.Return(result);
                    })),
                spec => spec.Mass,
                spec => spec.Intensity
            ).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);
        }

        private readonly MsdialImmsParameter parameter;
        private readonly IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator, textDBAnnotator;
        private readonly string peakAreaFile;
        private readonly IDataProvider provider;

        public ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks {
            get => ms1Peaks;
            set => SetProperty(ref ms1Peaks, value);
        }
        private ObservableCollection<ChromatogramPeakFeatureModel> ms1Peaks;

        public Chart.AnalysisPeakPlotModel PlotModel { get; }

        public Chart.EicModel EicModel { get; }

        public Chart.RawDecSpectrumsModel Ms2SpectrumModel { get; }

        public Chart.SurveyScanModel SurveyScanModel { get; }

        public double Ms1Tolerance => parameter.CentroidMs1Tolerance;

        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }
        private string rawSplashKey = string.Empty;

        public ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel;

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusDt {
            get => focusDt;
            set => SetProperty(ref focusDt, value);
        }
        private double focusDt;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        public double ChromMin => Ms1Peaks.Min(peak => peak.ChromXValue) ?? 0;
        public double ChromMax => Ms1Peaks.Max(peak => peak.ChromXValue) ?? 0;
        public double MassMin => Ms1Peaks.Min(peak => peak.Mass);
        public double MassMax => Ms1Peaks.Max(peak => peak.Mass);
        public double IntensityMin => Ms1Peaks.Min(peak => peak.Intensity);
        public double IntensityMax => Ms1Peaks.Max(peak => peak.Intensity);

        private CancellationTokenSource cts;
        async Task OnTargetChangedAsync(ChromatogramPeakFeatureModel target) {
            cts?.Cancel();
            var localCts = cts = new CancellationTokenSource();

            try {
                await OnTargetChangedAsync(target, localCts.Token).ContinueWith(
                    t => {
                        localCts.Dispose();
                        if (cts == localCts)
                            cts = null;
                    }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {

            }
        }

        async Task OnTargetChangedAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target != null) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusDt = target.ChromXValue ?? 0;
                FocusMz = target.Mass;
            }

            await Task.WhenAll(
                EicModel.LoadEicAsync(target, token),
                Ms2SpectrumModel.LoadSpectrumAsync(target, token)
            ).ConfigureAwait(false);
        }

        async Task<List<SpectrumPeakWrapper>> LoadMs1SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms1Spectrum = new List<SpectrumPeakWrapper>();

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS1RawSpectrumIdTop < 0) {
                        return;
                    }
                    var spectra = DataAccess.GetCentroidMassSpectra(provider.LoadMs1Spectrums()[target.MS1RawSpectrumIdTop], parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                    token.ThrowIfCancellationRequested();
                    ms1Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                }, token);
            }
            return ms1Spectrum;
        }
    }
}
